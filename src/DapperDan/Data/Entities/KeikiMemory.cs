namespace CodeCrafty.DapperDan.Data.Entities;

public sealed class KeikiMemory
{
    public int Id { get; set; }

    public int KeikiId { get; set; }

    public string Note { get; set; } = string.Empty;

    public DateTimeOffset RememberedUtc { get; set; } = DateTimeOffset.UtcNow;

    public Keiki? Keiki { get; set; }
}
