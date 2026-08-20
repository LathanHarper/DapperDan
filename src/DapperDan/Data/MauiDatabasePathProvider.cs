namespace CodeCrafty.DapperDan.Data;

public sealed class MauiDatabasePathProvider : IDatabasePathProvider
{
    private const string DatabaseFileName = "dapper-dan.db3";

    public string GetDatabasePath()
        => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
}
