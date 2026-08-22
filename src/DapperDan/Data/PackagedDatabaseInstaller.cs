using System.Globalization;
using Microsoft.Data.Sqlite;

namespace CodeCrafty.DapperDan.Data;

public sealed class PackagedDatabaseInstaller(
    IDatabasePathProvider databasePathProvider,
    IPackagedDatabaseSource packagedDatabaseSource)
{
    public async Task InstallAsync(CancellationToken cancellationToken = default)
    {
        var databasePath = databasePathProvider.GetDatabasePath();
        if (File.Exists(databasePath))
        {
            await ValidateAsync(databasePath, cancellationToken);
            return;
        }

        var databaseDirectory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException("The writable database path has no directory.");
        Directory.CreateDirectory(databaseDirectory);

        var installingPath = databasePath + $".{Guid.NewGuid():N}.installing";
        try
        {
            await using (var source = await packagedDatabaseSource.OpenReadAsync(cancellationToken))
            await using (var destination = new FileStream(
                installingPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.Asynchronous))
            {
                await source.CopyToAsync(destination, cancellationToken);
                await destination.FlushAsync(cancellationToken);
            }

            await ValidateAsync(installingPath, cancellationToken);

            try
            {
                File.Move(installingPath, databasePath);
            }
            catch (IOException) when (File.Exists(databasePath))
            {
                // Another initializer won the first-install race. Its database is
                // validated below before the canary is allowed to continue.
            }

            await ValidateAsync(databasePath, cancellationToken);
        }
        finally
        {
            File.Delete(installingPath);
        }
    }

    private static async Task ValidateAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            Pooling = false
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var applicationId = await ReadIntPragmaAsync(
            connection,
            "PRAGMA application_id;",
            cancellationToken);
        if (applicationId != DapperDanDatabaseMetadata.ApplicationId)
        {
            throw new InvalidDataException(
                $"The packaged database application_id is {applicationId}, expected {DapperDanDatabaseMetadata.ApplicationId}.");
        }

        var schemaVersion = await ReadIntPragmaAsync(
            connection,
            "PRAGMA user_version;",
            cancellationToken);
        if (schemaVersion != DapperDanDatabaseMetadata.SchemaVersion)
        {
            throw new InvalidDataException(
                $"The packaged database schema is v{schemaVersion}, expected v{DapperDanDatabaseMetadata.SchemaVersion}.");
        }

        await using var integrityCommand = connection.CreateCommand();
        integrityCommand.CommandText = "PRAGMA quick_check;";
        var integrityResult = Convert.ToString(
            await integrityCommand.ExecuteScalarAsync(cancellationToken),
            CultureInfo.InvariantCulture);
        if (!string.Equals(integrityResult, "ok", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"SQLite quick_check failed: {integrityResult ?? "no result"}.");
        }
    }

    private static async Task<int> ReadIntPragmaAsync(
        SqliteConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }
}
