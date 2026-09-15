using Flexler.Models;

namespace Flexler.Services;

public interface IFavoriteRecipeStore
{
    Task<IReadOnlyList<FavoriteRecipe>> LoadAsync();
    Task AddAsync(FavoriteRecipe favorite);
    Task RemoveAsync(Guid id);
}
