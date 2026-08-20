namespace CodeCrafty.DapperDan.Data.Entities;

public sealed class Keiki
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FavoriteBreak { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<KeikiMemory> Memories { get; set; } = new List<KeikiMemory>();
}
