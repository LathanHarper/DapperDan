namespace CodeCrafty.DapperDan.Data;

public sealed class MauiPackagedDatabaseSource : IPackagedDatabaseSource
{
    public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = await FileSystem.OpenAppPackageFileAsync(
            DapperDanDatabaseMetadata.SeedAssetName);
        cancellationToken.ThrowIfCancellationRequested();
        return stream;
    }
}
