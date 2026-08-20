using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using CodeCrafty.DapperDan.Data;
using CodeCrafty.DapperDan.Data.Entities;

namespace CodeCrafty.DapperDan.Tests;

public sealed class KeikiPersistenceTests
{
    [Fact]
    public async Task ParentAndMemoriesRoundTripAndCascade()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DapperDanDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var arrangeContext = new DapperDanDbContext(options))
        {
            await arrangeContext.Database.EnsureCreatedAsync();
            arrangeContext.Keiki.Add(new Keiki
            {
                Name = "Kai",
                FavoriteBreak = "Keiki Cove",
                Memories =
                {
                    new KeikiMemory { Note = "First paddle-out" },
                    new KeikiMemory { Note = "Shared the wave" }
                }
            });

            await arrangeContext.SaveChangesAsync();
        }

        await using (var readContext = new DapperDanDbContext(options))
        {
            var saved = await readContext.Keiki
                .Include(item => item.Memories)
                .SingleAsync();

            Assert.Equal("Kai", saved.Name);
            Assert.Equal(2, saved.Memories.Count);
            readContext.Keiki.Remove(saved);
            await readContext.SaveChangesAsync();
        }

        await using var assertContext = new DapperDanDbContext(options);
        Assert.Empty(await assertContext.KeikiMemories.ToListAsync());
    }
}
