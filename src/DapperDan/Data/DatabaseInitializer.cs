using Microsoft.EntityFrameworkCore;

using CodeCrafty.DapperDan.Data.Entities;

namespace CodeCrafty.DapperDan.Data;

public sealed class DatabaseInitializer(IDbContextFactory<DapperDanDbContext> contextFactory)
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

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await context.Database.EnsureCreatedAsync(cancellationToken);

            if (!await context.Keiki.AnyAsync(cancellationToken))
            {
                context.Keiki.Add(new Keiki
                {
                    Name = "Kai",
                    FavoriteBreak = "Keiki Cove",
                    Memories =
                    {
                        new KeikiMemory { Note = "First clean paddle-out." },
                        new KeikiMemory { Note = "Remembered to mālama the beach." }
                    }
                });

                await context.SaveChangesAsync(cancellationToken);
            }

            _isInitialized = true;
        }
        finally
        {
            _initializationGate.Release();
        }
    }
}
