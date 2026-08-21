using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using CodeCrafty.DapperDan.Data;

namespace CodeCrafty.DapperDan.DatabaseTool;

public sealed class DapperDanDesignTimeFactory
    : IDesignTimeDbContextFactory<DapperDanDbContext>
{
    public DapperDanDbContext CreateDbContext(string[] args)
    {
        SQLitePCL.Batteries_V2.Init();

        var options = new DbContextOptionsBuilder<DapperDanDbContext>()
            .UseSqlite("Data Source=dapper-dan-model-compiler.db3")
            .Options;

        return new DapperDanDbContext(options);
    }
}
