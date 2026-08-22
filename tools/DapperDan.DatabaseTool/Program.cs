using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using CodeCrafty.DapperDan.Data;
using CodeCrafty.DapperDan.Data.Entities;

if (args is not ["--seed", var requestedOutput])
{
    Console.Error.WriteLine("Usage: DapperDan.DatabaseTool --seed <output.db3>");
    return 2;
}

SQLitePCL.Batteries_V2.Init();

var outputPath = Path.GetFullPath(requestedOutput);
if (!string.Equals(Path.GetExtension(outputPath), ".db3", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("The seed output must use the .db3 extension.");
    return 2;
}

var outputDirectory = Path.GetDirectoryName(outputPath)
    ?? throw new InvalidOperationException("The seed output path has no directory.");
Directory.CreateDirectory(outputDirectory);

var artifactName = Path.GetFileName(outputPath);
var generationId = Guid.NewGuid().ToString("N");
var buildingPath = Path.Combine(
    outputDirectory,
    $".{artifactName}.{generationId}.building");
var backupPath = Path.Combine(
    outputDirectory,
    $".{artifactName}.{generationId}.backup");
var replacementCompleted = false;
try
{
    var buildingConnectionString = new SqliteConnectionStringBuilder
    {
        DataSource = buildingPath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Private,
        ForeignKeys = true,
        Pooling = false
    }.ToString();
    var options = new DbContextOptionsBuilder<DapperDanDbContext>()
        .UseSqlite(buildingConnectionString)
        .Options;

    await using (var context = new DapperDanDbContext(options))
    {
        // SQLite stores the original CREATE statements in sqlite_schema. Use
        // one canonical newline so the reviewed seed does not depend on the
        // generator host's Windows/Unix line ending.
        var createScript = NormalizeLineEndings(
            context.Database.GenerateCreateScript());
        await context.Database.ExecuteSqlRawAsync(createScript);
        context.Keiki.Add(new Keiki
        {
            Id = 1,
            Name = "Kai",
            FavoriteBreak = "Keiki Cove",
            CreatedUtc = DateTimeOffset.Parse(
                "2026-01-01T08:00:00+00:00",
                CultureInfo.InvariantCulture),
            Memories =
            {
                new KeikiMemory
                {
                    Id = 1,
                    Note = "First clean paddle-out.",
                    RememberedUtc = DateTimeOffset.Parse(
                        "2026-01-01T08:05:00+00:00",
                        CultureInfo.InvariantCulture)
                },
                new KeikiMemory
                {
                    Id = 2,
                    Note = "Remembered to mālama the beach.",
                    RememberedUtc = DateTimeOffset.Parse(
                        "2026-01-01T08:10:00+00:00",
                        CultureInfo.InvariantCulture)
                }
            }
        });
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync(
            $"PRAGMA application_id = {DapperDanDatabaseMetadata.ApplicationId};");
        await context.Database.ExecuteSqlRawAsync(
            $"PRAGMA user_version = {DapperDanDatabaseMetadata.SchemaVersion};");
    }

    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = buildingPath,
        Mode = SqliteOpenMode.ReadWrite,
        Cache = SqliteCacheMode.Private,
        ForeignKeys = true,
        Pooling = false
    }.ToString();
    await using (var connection = new SqliteConnection(connectionString))
    {
        await connection.OpenAsync();

        await using (var journalMode = connection.CreateCommand())
        {
            journalMode.CommandText = "PRAGMA journal_mode=DELETE;";
            await journalMode.ExecuteNonQueryAsync();
        }

        await using (var vacuum = connection.CreateCommand())
        {
            vacuum.CommandText = "VACUUM;";
            await vacuum.ExecuteNonQueryAsync();
        }

        await using var integrity = connection.CreateCommand();
        integrity.CommandText = "PRAGMA integrity_check;";
        var result = Convert.ToString(
            await integrity.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture);
        if (!string.Equals(result, "ok", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Generated seed failed integrity_check: {result}.");
        }
    }

    using (var stagedDatabase = new FileStream(
        buildingPath,
        FileMode.Open,
        FileAccess.ReadWrite,
        FileShare.None))
    {
        stagedDatabase.Flush(flushToDisk: true);
    }

    if (File.Exists(outputPath))
    {
        File.Replace(
            buildingPath,
            outputPath,
            backupPath,
            ignoreMetadataErrors: true);
    }
    else
    {
        File.Move(buildingPath, outputPath);
    }

    replacementCompleted = true;
    DeleteDatabaseFiles(backupPath);
    Console.WriteLine($"Generated {outputPath}");
    return 0;
}
finally
{
    DeleteDatabaseFiles(buildingPath);
    if (replacementCompleted)
    {
        DeleteDatabaseFiles(backupPath);
    }
}

static void DeleteDatabaseFiles(string databasePath)
{
    File.Delete(databasePath);
    File.Delete(databasePath + "-wal");
    File.Delete(databasePath + "-shm");
}

static string NormalizeLineEndings(string value) =>
    value
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n');
