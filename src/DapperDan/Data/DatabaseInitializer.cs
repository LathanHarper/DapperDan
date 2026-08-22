namespace CodeCrafty.DapperDan.Data;

public sealed class DatabaseInitializer(PackagedDatabaseInstaller databaseInstaller)
{
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private bool _isInitialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializationGate.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await databaseInstaller.InstallAsync(cancellationToken);
            _isInitialized = true;
        }
        finally
        {
            _initializationGate.Release();
        }
    }
}
