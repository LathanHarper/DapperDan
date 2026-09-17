using Flexler.ViewModels;

namespace Flexler.Services;

public interface IFlexRecipeExporter
{
    string Build(MainPageViewModel recipe);

    Task CopyAsync(MainPageViewModel recipe);
}
