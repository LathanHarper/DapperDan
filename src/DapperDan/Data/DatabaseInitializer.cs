using CodeCrafty.DapperDan.Diagnostics;

namespace CodeCrafty.DapperDan.Data;

public sealed class DatabaseInitializer(PackagedDatabaseInstaller databaseInstaller)
{
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private bool _isInitialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        CrashJournal.Checkpoint(CrashPoint.DatabaseInitializeEnter);

        if (_isInitialized)
        {
            CrashJournal.Checkpoint(CrashPoint.DatabaseInitializeReady);
            return;
        }

        await _initializationGate.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                CrashJournal.Checkpoint(CrashPoint.DatabaseInitializeReady);
                return;
            }

            await databaseInstaller.InstallAsync(cancellationToken);
            _isInitialized = true;
            CrashJournal.Checkpoint(CrashPoint.DatabaseInitializeReady);
        }
        finally
        {
            _initializationGate.Release();
        }
    }
}
