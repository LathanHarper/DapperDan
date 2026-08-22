using Microsoft.EntityFrameworkCore;

using CodeCrafty.DapperDan.Data;
using CodeCrafty.DapperDan.Data.CompiledModels;

namespace CodeCrafty.DapperDan.Tests;

public sealed class KeikiPersistenceTests
{
    public KeikiPersistenceTests()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    [Fact]
    public async Task PackagedSeedUsesCompiledModelAndRoundTripsRepositoryWrites()
    {
        using var sandbox = new DatabaseSandbox();
        var installer = sandbox.CreateInstaller();
        await installer.InstallAsync();

        var options = sandbox.CreateOptions();
        await using (var context = new DapperDanDbContext(options))
        {
            Assert.Same(DapperDanDbContextModel.Instance, context.Model);
        }

        var repository = new KeikiRepository(
            new TestDbContextFactory(options),
            new DatabaseInitializer(installer));

        var seeded = await repository.LoadAsync();
        var kai = Assert.Single(seeded);
        Assert.Equal("Kai", kai.Name);
        Assert.Equal("Keiki Cove", kai.FavoriteBreak);
        Assert.Equal(2, kai.Memories.Count);

        await repository.AddAsync(
            "Malia",
            "First Light",
            "Caught the clean canary wave.");

        var saved = await repository.LoadAsync();
        Assert.Equal(["Kai", "Malia"], saved.Select(item => item.Name));

        await repository.ClearAsync();
        Assert.Empty(await repository.LoadAsync());

        await using var assertContext = new DapperDanDbContext(options);
        Assert.Empty(await assertContext.KeikiMemories.ToListAsync());
    }

    [Fact]
    public async Task ExistingWritableDatabaseIsValidatedButNeverOverwritten()
    {
        using var sandbox = new DatabaseSandbox();
        var installer = sandbox.CreateInstaller();
        await installer.InstallAsync();

        var options = sandbox.CreateOptions();
        await using (var context = new DapperDanDbContext(options))
        {
            context.Keiki.Add(new()
            {
                Name = "Noa",
                FavoriteBreak = "Second Launch"
            });
            await context.SaveChangesAsync();
        }

        await installer.InstallAsync();

        await using var assertContext = new DapperDanDbContext(options);
        Assert.Equal(2, await assertContext.Keiki.CountAsync());
        Assert.True(await assertContext.Keiki.AnyAsync(item => item.Name == "Noa"));
    }

    [Fact]
    public async Task PackagedSeedCarriesExpectedIdentityVersionAndIntegrity()
    {
        using var sandbox = new DatabaseSandbox();
        var installer = sandbox.CreateInstaller();
        await installer.InstallAsync();

        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            $"Data Source={sandbox.DatabasePath};Mode=ReadOnly;Cache=Private;Pooling=False");
        await connection.OpenAsync();

        Assert.Equal(
            DapperDanDatabaseMetadata.ApplicationId,
            await ReadIntAsync(connection, "PRAGMA application_id;"));
        Assert.Equal(
            DapperDanDatabaseMetadata.SchemaVersion,
            await ReadIntAsync(connection, "PRAGMA user_version;"));
        Assert.Equal("ok", await ReadStringAsync(connection, "PRAGMA integrity_check;"));

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = "SELECT sql FROM sqlite_schema WHERE sql IS NOT NULL;";
        await using var schemaReader = await schemaCommand.ExecuteReaderAsync();
        while (await schemaReader.ReadAsync())
        {
            // Canonical seed schema text uses LF on every generation platform.
            Assert.DoesNotContain('\r', schemaReader.GetString(0));
        }
    }

    [Fact]
    public async Task InvalidPackagedDatabaseLeavesNoPartialWritableFile()
    {
        using var sandbox = new DatabaseSandbox();
        var invalidSeedPath = Path.Combine(sandbox.DirectoryPath, "invalid-seed.db3");
        await File.WriteAllTextAsync(invalidSeedPath, "not a SQLite database");

        var installer = sandbox.CreateInstaller(invalidSeedPath);
        await Assert.ThrowsAnyAsync<Exception>(() => installer.InstallAsync());

        Assert.False(File.Exists(sandbox.DatabasePath));
        Assert.Empty(Directory.EnumerateFiles(
            sandbox.DirectoryPath,
            DapperDanDatabaseMetadata.WritableDatabaseFileName + ".*.installing"));
    }

    [Fact]
    public async Task ConcurrentFirstInstallersPublishOneValidatedDatabaseAndCleanStagingFiles()
    {
        using var sandbox = new DatabaseSandbox();
        const int installerCount = 8;
        var source = new CoordinatedPackagedDatabaseSource(
            sandbox.SeedPath,
            installerCount);
        var installers = Enumerable
            .Range(0, installerCount)
            .Select(_ => sandbox.CreateInstaller(source))
            .ToArray();

        await Task.WhenAll(installers.Select(installer => installer.InstallAsync()));

        Assert.True(File.Exists(sandbox.DatabasePath));
        Assert.Empty(Directory.EnumerateFiles(
            sandbox.DirectoryPath,
            DapperDanDatabaseMetadata.WritableDatabaseFileName + ".*.installing"));

        await using var context = new DapperDanDbContext(sandbox.CreateOptions());
        var kai = await context.Keiki
            .Include(item => item.Memories)
            .SingleAsync();
        Assert.Equal("Kai", kai.Name);
        Assert.Equal(2, kai.Memories.Count);
    }

    private static async Task<int> ReadIntAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        string commandText)
        => Convert.ToInt32(await ReadScalarAsync(connection, commandText));

    private static async Task<string> ReadStringAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        string commandText)
        => Convert.ToString(await ReadScalarAsync(connection, commandText)) ?? string.Empty;

    private static async Task<object?> ReadScalarAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        return await command.ExecuteScalarAsync();
    }

    private sealed class DatabaseSandbox : IDisposable
    {
        private readonly string _directory = Directory
            .CreateTempSubdirectory("DapperDan.Tests.")
            .FullName;

        public string DirectoryPath => _directory;

        public string DatabasePath => Path.Combine(
            _directory,
            DapperDanDatabaseMetadata.WritableDatabaseFileName);

        public string SeedPath => GetSeedPath();

        public PackagedDatabaseInstaller CreateInstaller(string? seedPath = null)
            => new(
                new TestDatabasePathProvider(DatabasePath),
                new TestPackagedDatabaseSource(seedPath ?? GetSeedPath()));

        public PackagedDatabaseInstaller CreateInstaller(IPackagedDatabaseSource source)
            => new(
                new TestDatabasePathProvider(DatabasePath),
                source);

        public DbContextOptions<DapperDanDbContext> CreateOptions()
            => new DbContextOptionsBuilder<DapperDanDbContext>()
                .UseSqlite(
                    $"Data Source={DatabasePath};Mode=ReadWrite;Cache=Private;Foreign Keys=True;Pooling=False")
                .UseModel(DapperDanDbContextModel.Instance)
                .Options;

        public void Dispose()
            => Directory.Delete(_directory, recursive: true);

        private static string GetSeedPath()
            => Path.Combine(
                AppContext.BaseDirectory,
                DapperDanDatabaseMetadata.SeedAssetName);
    }

    private sealed class TestDatabasePathProvider(string databasePath)
        : IDatabasePathProvider
    {
        public string GetDatabasePath() => databasePath;
    }

    private sealed class TestPackagedDatabaseSource(string seedPath)
        : IPackagedDatabaseSource
    {
        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(File.OpenRead(seedPath));
        }
    }

    private sealed class CoordinatedPackagedDatabaseSource(
        string seedPath,
        int expectedReaders)
        : IPackagedDatabaseSource
    {
        private readonly TaskCompletionSource<bool> _allReadersReady = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _readerCount;

        public async Task<Stream> OpenReadAsync(
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _readerCount) == expectedReaders)
            {
                _allReadersReady.TrySetResult(true);
            }

            await _allReadersReady.Task.WaitAsync(cancellationToken);
            return File.OpenRead(seedPath);
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<DapperDanDbContext> options)
        : IDbContextFactory<DapperDanDbContext>
    {
        public DapperDanDbContext CreateDbContext()
            => new(options);

        public Task<DapperDanDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }
    }
}
