using Microsoft.EntityFrameworkCore;

using CodeCrafty.DapperDan.Data.Entities;

namespace CodeCrafty.DapperDan.Data;

public sealed class DapperDanDbContext(DbContextOptions<DapperDanDbContext> options) : DbContext(options)
{
    public DbSet<Keiki> Keiki => Set<Keiki>();

    public DbSet<KeikiMemory> KeikiMemories => Set<KeikiMemory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Keiki>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(80).IsRequired();
            entity.Property(item => item.FavoriteBreak).HasMaxLength(120);
            entity.HasIndex(item => item.Name);
            entity.HasMany(item => item.Memories)
                .WithOne(item => item.Keiki)
                .HasForeignKey(item => item.KeikiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KeikiMemory>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Note).HasMaxLength(280).IsRequired();
            entity.HasIndex(item => new { item.KeikiId, item.RememberedUtc });
        });
    }
}
