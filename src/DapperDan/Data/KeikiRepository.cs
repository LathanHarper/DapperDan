using Microsoft.EntityFrameworkCore;

using CodeCrafty.DapperDan.Data.Entities;

namespace CodeCrafty.DapperDan.Data;

public sealed class KeikiRepository(
    IDbContextFactory<DapperDanDbContext> contextFactory,
    DatabaseInitializer databaseInitializer) : IKeikiRepository
{
    public async Task<Keiki> AddAsync(
        string name,
        string favoriteBreak,
        string memory,
        CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken);

        var keiki = new Keiki
        {
            Name = name.Trim(),
            FavoriteBreak = favoriteBreak.Trim()
        };

        if (!string.IsNullOrWhiteSpace(memory))
        {
            keiki.Memories.Add(new KeikiMemory { Note = memory.Trim() });
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Keiki.Add(keiki);
        await context.SaveChangesAsync(cancellationToken);
        return keiki;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Keiki.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Keiki>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await databaseInitializer.InitializeAsync(cancellationToken);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Keiki
            .AsNoTracking()
            .Include(item => item.Memories)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }
}
