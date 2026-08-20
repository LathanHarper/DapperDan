using CodeCrafty.DapperDan.Data.Entities;

namespace CodeCrafty.DapperDan.Data;

public interface IKeikiRepository
{
    Task<Keiki> AddAsync(
        string name,
        string favoriteBreak,
        string memory,
        CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Keiki>> LoadAsync(CancellationToken cancellationToken = default);
}
