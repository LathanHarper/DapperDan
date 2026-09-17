using System.Collections.ObjectModel;
using System.Globalization;
using Flexler.Models;
using Flexler.Services;
using Microsoft.Maui.Converters;
using Microsoft.Maui.Layouts;
using Prism.Commands;

namespace Flexler.ViewModels;

public sealed partial class MainPageViewModel
{
    private readonly IFavoriteRecipeStore _favoriteStore;
    private bool _favoriteOperationRunning;
    private bool _favoritesLoaded;
    private string _favoriteName = string.Empty;
    private string _favoritesStatus = "Save a layout to keep it on this device.";
    private bool _isFavoriteBusy;
    private bool _isFavoritesView;
    private Guid? _pendingDeleteId;
    private FavoriteRecipe? _selectedFavorite;

    public ObservableCollection<FavoriteRecipe> Favorites { get; } = [];
    public AsyncDelegateCommand OpenFavoritesCommand { get; }
    public AsyncDelegateCommand SaveFavoriteCommand { get; }
    public AsyncDelegateCommand LoadFavoriteCommand { get; }
    public AsyncDelegateCommand DeleteFavoriteCommand { get; }

    public string FavoriteName
    {
        get => _favoriteName;
        set { if (SetProperty(ref _favoriteName, value)) SaveFavoriteCommand.RaiseCanExecuteChanged(); }
    }

    public string FavoritesStatus
    {
        get => _favoritesStatus;
        private set => SetProperty(ref _favoritesStatus, value);
    }

    public bool IsFavoriteBusy
    {
        get => _isFavoriteBusy;
        set => SetProperty(ref _isFavoriteBusy, value);
    }

    public bool IsFavoritesView
    {
        get => _isFavoritesView;
        private set
        {
            if (!SetProperty(ref _isFavoritesView, value)) return;
            RaisePropertyChanged(nameof(IsXamlView));
            RaisePropertyChanged(nameof(RecipePanelTitle));
        }
    }

    public bool IsXamlView => !IsFavoritesView;
    public string RecipePanelTitle => IsFavoritesView ? "FAVORITES" : "XAML RECIPE";
    public string DeleteFavoriteLabel => _pendingDeleteId == SelectedFavorite?.Id && _pendingDeleteId is not null
        ? "CONFIRM REMOVE" : "REMOVE";
    public string FavoriteSummary => SelectedFavorite is { } favorite
        ? $"{favorite.Recipe.Items.Count} items · {favorite.Recipe.Direction} · {favorite.Recipe.Wrap}"
        : "No favorite selected";

    public FavoriteRecipe? SelectedFavorite
    {
        get => _selectedFavorite;
        set
        {
            if (!SetProperty(ref _selectedFavorite, value)) return;
            ClearDeleteConfirmation();
            RaisePropertyChanged(nameof(FavoriteSummary));
            RaiseFavoriteCommands();
        }
    }

    private bool CanUseSelectedFavorite() => !_favoriteOperationRunning && _favoritesLoaded
        && SelectedFavorite is { } favorite && Favorites.Contains(favorite);

    private Task OpenFavoritesAsync() => RunEditorOperationAsync(busy => IsRecipeBusy = busy, async () =>
    {
        await CloseContainerCoreAsync();
        await CloseInspectorCoreAsync();
        IsFavoritesView = true;
        ClearDeleteConfirmation();
        await ActivePanelBoss.InputPanels_ActivatePanelByName(RecipePanelName);
        IsRecipeOpen = true;
        await RunFavoriteOperationAsync(async () =>
        {
            _favoritesLoaded = false;
            var saved = await _favoriteStore.LoadAsync();
            var selection = SelectedFavorite?.Id;
            Favorites.Clear();
            foreach (var favorite in saved) Favorites.Add(favorite);
            SelectedFavorite = Favorites.FirstOrDefault(f => f.Id == selection) ?? Favorites.FirstOrDefault();
            _favoritesLoaded = true;
            FavoritesStatus = Favorites.Count == 0
                ? "No favorites yet. Name your current layout and save it."
                : $"{Favorites.Count} saved on this device. Load replaces the current layout.";
        });
    });

    private Task SaveFavoriteAsync() => RunFavoriteOperationAsync(async () =>
    {
        ClearDeleteConfirmation();
        var name = FavoriteName.Trim();
        if (name.Length is 0 or > 80) throw new ArgumentException("Use a favorite name between 1 and 80 characters.");
        _recipeExporter.Build(this); // Validate the exact layout before saving a snapshot.
        var revision = _recipeRevision;
        var favorite = new FavoriteRecipe(Guid.NewGuid(), name, DateTimeOffset.UtcNow, CaptureRecipe());
        await _favoriteStore.AddAsync(favorite);
        Favorites.Insert(0, favorite);
        SelectedFavorite = favorite;
        // Keep any new name typed while the write was in flight.
        if (FavoriteName.Trim() == name) FavoriteName = string.Empty;
        FavoritesStatus = revision == _recipeRevision
            ? $"Saved “{name}” on this device."
            : $"Saved “{name}”. Your current layout has newer edits.";
    });

    private Task LoadFavoriteAsync() => RunFavoriteOperationAsync(async () =>
    {
        var favorite = SelectedFavorite;
        if (favorite is null || !Favorites.Contains(favorite)) return;
        favorite.Validate();
        ClearDeleteConfirmation();
        await RunEditorOperationAsync(busy => IsRecipeBusy = busy, async () =>
        {
            await ActivePanelBoss.RestoreDefaultPanelChromeAsync();
            ApplyRecipe(favorite.Recipe);
            IsContainerOpen = false;
            IsInspectorOpen = false;
            IsRecipeOpen = false;
            ExportStatus = $"Loaded “{favorite.Name}” · ready to shape or copy";
            FavoritesStatus = $"Loaded “{favorite.Name}”. The saved favorite stays unchanged as you edit.";
        });
    });

    private Task DeleteFavoriteAsync() => RunFavoriteOperationAsync(async () =>
    {
        var favorite = SelectedFavorite;
        if (favorite is null || !Favorites.Contains(favorite)) return;
        if (_pendingDeleteId != favorite.Id)
        {
            _pendingDeleteId = favorite.Id;
            RaisePropertyChanged(nameof(DeleteFavoriteLabel));
            FavoritesStatus = $"Remove “{favorite.Name}”? Tap CONFIRM REMOVE. Your current layout stays.";
            return;
        }
        await _favoriteStore.RemoveAsync(favorite.Id);
        Favorites.Remove(favorite);
        SelectedFavorite = Favorites.FirstOrDefault();
        ClearDeleteConfirmation();
        FavoritesStatus = $"Removed “{favorite.Name}” from favorites. Your current layout stays.";
    });

    private async Task RunFavoriteOperationAsync(Func<Task> operation)
    {
        // RichButton writes IsFavoriteBusy before executing; use a separate guard.
        if (_favoriteOperationRunning) return;
        _favoriteOperationRunning = true;
        IsFavoriteBusy = true;
        RaiseFavoriteCommands();
        try { await operation(); }
        catch (ArgumentException)
        {
            FavoritesStatus = "Could not save. Check the layout and use a unique name (1–80 characters).";
        }
        catch (Exception)
        {
            FavoritesStatus = "Favorites unavailable. Your layout is safe. Check device storage, then reopen Favorites to retry.";
        }
        finally
        {
            _favoriteOperationRunning = false;
            IsFavoriteBusy = false;
            RaiseFavoriteCommands();
        }
    }

    private void RaiseFavoriteCommands()
    {
        SaveFavoriteCommand.RaiseCanExecuteChanged();
        LoadFavoriteCommand.RaiseCanExecuteChanged();
        DeleteFavoriteCommand.RaiseCanExecuteChanged();
    }

    private void ClearDeleteConfirmation()
    {
        _pendingDeleteId = null;
        RaisePropertyChanged(nameof(DeleteFavoriteLabel));
    }

    private FlexRecipeSnapshot CaptureRecipe() => new(Direction, Wrap, JustifyContent, AlignItems, AlignContent,
        PreviewPadding, PreviewMinimumHeight, Items.Select(item => new FlexItemSnapshot(
            item.Text, item.AlignSelf, item.Grow, item.Shrink, item.Order,
            item.Basis == FlexBasis.Auto ? null : item.Basis.Length,
            new FlexBasisTypeConverter().ConvertToInvariantString(item.Basis)?.EndsWith('%') == true)).ToArray());

    private void ApplyRecipe(FlexRecipeSnapshot recipe)
    {
        // Build fresh VMs before touching the current canvas; edits must not mutate stored snapshots.
        var items = recipe.Items.Select((item, index) =>
        {
            var basis = item.BasisLength is { } length ? new FlexBasis(length, item.BasisIsRelative) : FlexBasis.Auto;
            var option = BasisOptions.FirstOrDefault(option => option.Value == basis)
                ?? new FlexBasisOption(item.BasisIsRelative
                    ? (basis.Length * 100).ToString(CultureInfo.InvariantCulture) + "%"
                    : basis.Length.ToString(CultureInfo.InvariantCulture), basis);
            return new FlexItemViewModel(index + 1, item.Text, option)
            { AlignSelf = item.AlignSelf, Grow = item.Grow, Shrink = item.Shrink, Order = item.Order };
        }).ToArray();
        _isResetting = true;
        try
        {
            Direction = recipe.Direction;
            Wrap = recipe.Wrap;
            JustifyContent = recipe.JustifyContent;
            AlignItems = recipe.AlignItems;
            AlignContent = recipe.AlignContent;
            PreviewPadding = recipe.Padding;
            PreviewMinimumHeight = recipe.MinimumHeight;
            SelectedItem = null;
            Items.Clear();
            foreach (var item in items) Items.Add(item);
            _nextId = items.Length + 1;
        }
        finally { _isResetting = false; }
        RefreshRecipe();
    }
}
