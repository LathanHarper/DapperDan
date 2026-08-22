namespace CodeCrafty.DapperDan.Data;

public interface IPackagedDatabaseSource
{
    Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default);
}
