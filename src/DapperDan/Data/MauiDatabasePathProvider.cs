namespace CodeCrafty.DapperDan.Data;

public sealed class MauiDatabasePathProvider : IDatabasePathProvider
{
    public string GetDatabasePath()
        => Path.Combine(
            FileSystem.AppDataDirectory,
            DapperDanDatabaseMetadata.WritableDatabaseFileName);
}
