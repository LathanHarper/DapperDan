using CodeCrafty.DapperDan.Diagnostics;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Flexler.Models;
using Flexler.PanelBossKit;
using Flexler.Services;
using Microsoft.Maui.Layouts;
using Prism.Commands;
using Prism.Mvvm;

namespace Flexler.ViewModels;

public sealed partial class MainPageViewModel : BindableBase, INavigationAware
{
    public const string ContainerControlsPanelName = "FlexlerContainerControlsPanel";
    public const string InspectorPanelName = "FlexlerInspectorPanel";
    public const string RecipePanelName = "FlexlerRecipePanel";

    private readonly SemaphoreSlim _editorGate = new(1, 1);
    private readonly HashSet<FlexItemViewModel> _observedItems = [];
    private readonly IFlexRecipeExporter _recipeExporter;
    private FlexAlignContent _alignContent = FlexAlignContent.Stretch;
    private FlexAlignItems _alignItems = FlexAlignItems.Stretch;
    private FlexDirection _direction = FlexDirection.Row;
    private string _exportStatus = "Ready";
    private bool _isContainerBusy;
    private bool _isContainerOpen;
    private bool _isExportBusy;
    private bool _isInspectorBusy;
    private bool _isInspectorOpen;
    private bool _isRecipeBusy;
    private bool _isRecipeOpen;
    private bool _isRemoveBusy;
    private bool _isResetBusy;
    private bool _isResetting;
    private FlexJustify _justifyContent = FlexJustify.Start;
    private string _layoutSummary = string.Empty;
    private int _nextId = 1;
    private double _previewMinimumHeight;
    private double _previewPadding = 18;
    private string _recipeError = string.Empty;
    private int _recipeRevision;
    private string _recipeXaml = string.Empty;
    private FlexItemViewModel? _selectedItem;
    private FlexWrap _wrap = FlexWrap.Wrap;

    public MainPageViewModel(
        IFlexRecipeExporter recipeExporter,
        PanelBoss activePanelBoss,
        IFavoriteRecipeStore favoriteStore,
        INavigationService? navigationService = null)
    {
        CrashJournal.Checkpoint(CrashPoint.FlexlerViewModelEnter);
        try
        {
            _recipeExporter = recipeExporter;
            ActivePanelBoss = activePanelBoss;
            _favoriteStore = favoriteStore;
            _showcaseNavigation = navigationService;

            BasisOptions =
            [
                new FlexBasisOption("Auto", FlexBasis.Auto),
                new FlexBasisOption("25%", new FlexBasis(0.25f, true)),
                new FlexBasisOption("50%", new FlexBasis(0.50f, true)),
                new FlexBasisOption("75%", new FlexBasis(0.75f, true)),
                new FlexBasisOption("100%", new FlexBasis(1f, true)),
                new FlexBasisOption("64", new FlexBasis(64)),
                new FlexBasisOption("128", new FlexBasis(128))
            ];

            AddItemCommand = new DelegateCommand(AddItem);
            CloseContainerControlsCommand = new AsyncDelegateCommand(CloseContainerControlsAsync);
            CloseInspectorCommand = new AsyncDelegateCommand(CloseInspectorAsync);
            CloseRecipeCommand = new AsyncDelegateCommand(CloseRecipeAsync);
            EditItemCommand = new AsyncDelegateCommand<FlexItemViewModel>(
                OpenInspectorAsync,
                item => item is not null && Items.Contains(item));
            ExportCommand = new AsyncDelegateCommand(ExportAsync);
            PreviewRecipeCommand = new AsyncDelegateCommand(PreviewRecipeAsync);
            RemoveSelectedCommand = new AsyncDelegateCommand(RemoveSelectedAsync, () => SelectedItem is not null);
            ShapeSelectedCommand = new AsyncDelegateCommand(
                () => SelectedItem is { } item ? OpenInspectorAsync(item) : Task.CompletedTask,
                () => SelectedItem is not null);
            ResetCommand = new AsyncDelegateCommand(ResetAsync);
            ToggleContainerControlsCommand = new AsyncDelegateCommand(ToggleContainerControlsAsync);
            OpenFavoritesCommand = new AsyncDelegateCommand(OpenFavoritesAsync);
            SaveFavoriteCommand = new AsyncDelegateCommand(SaveFavoriteAsync,
                () => !_favoriteOperationRunning && _favoritesLoaded && !string.IsNullOrWhiteSpace(FavoriteName));
            LoadFavoriteCommand = new AsyncDelegateCommand(LoadFavoriteAsync, CanUseSelectedFavorite);
            DeleteFavoriteCommand = new AsyncDelegateCommand(DeleteFavoriteAsync, CanUseSelectedFavorite);

            Items.CollectionChanged += OnItemsChanged;
            ResetState();
            CrashJournal.Checkpoint(CrashPoint.FlexlerViewModelReady);
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(CrashSource.GuardedSeam,
                CrashPoint.FlexlerViewModelEnter, exception, terminating: false);
            throw;
        }
    }

    public PanelBoss ActivePanelBoss { get; }

    public IReadOnlyList<FlexAlignContent> AlignContentOptions { get; } =
        Enum.GetValues<FlexAlignContent>();

    public IReadOnlyList<FlexAlignItems> AlignItemsOptions { get; } =
        Enum.GetValues<FlexAlignItems>();

    public IReadOnlyList<FlexAlignSelf> AlignSelfOptions { get; } =
        Enum.GetValues<FlexAlignSelf>();

    public IReadOnlyList<FlexBasisOption> BasisOptions { get; }

    public IReadOnlyList<FlexDirection> DirectionOptions { get; } =
        Enum.GetValues<FlexDirection>();

    public IReadOnlyList<float> FlexFactorOptions { get; } =
        [0f, 0.25f, 0.5f, 1f, 2f, 3f, 4f];

    public IReadOnlyList<FlexJustify> JustifyOptions { get; } =
        Enum.GetValues<FlexJustify>();

    public IReadOnlyList<int> OrderOptions { get; } =
        Enumerable.Range(-5, 16).ToArray();

    public IReadOnlyList<FlexWrap> WrapOptions { get; } =
        Enum.GetValues<FlexWrap>();

    public ObservableCollection<FlexItemViewModel> Items { get; } = [];

    public DelegateCommand AddItemCommand { get; }

    public AsyncDelegateCommand CloseContainerControlsCommand { get; }

    public AsyncDelegateCommand CloseInspectorCommand { get; }

    public AsyncDelegateCommand CloseRecipeCommand { get; }

    public AsyncDelegateCommand<FlexItemViewModel> EditItemCommand { get; }

    public AsyncDelegateCommand ExportCommand { get; }

    public AsyncDelegateCommand PreviewRecipeCommand { get; }

    public AsyncDelegateCommand RemoveSelectedCommand { get; }

    public AsyncDelegateCommand ShapeSelectedCommand { get; }

    public AsyncDelegateCommand ResetCommand { get; }

    public AsyncDelegateCommand ToggleContainerControlsCommand { get; }

    public FlexAlignContent AlignContent
    {
        get => _alignContent;
        set
        {
            if (SetProperty(ref _alignContent, value))
            {
                RefreshRecipe();
            }
        }
    }

    public FlexAlignItems AlignItems
    {
        get => _alignItems;
        set
        {
            if (SetProperty(ref _alignItems, value))
            {
                RefreshRecipe();
            }
        }
    }

    public FlexDirection Direction
    {
        get => _direction;
        set
        {
            if (SetProperty(ref _direction, value))
            {
                RefreshRecipe();
            }
        }
    }

    public string ExportStatus
    {
        get => _exportStatus;
        private set => SetProperty(ref _exportStatus, value);
    }

    public bool IsInspectorOpen
    {
        get => _isInspectorOpen;
        private set => SetProperty(ref _isInspectorOpen, value);
    }

    public bool IsContainerOpen
    {
        get => _isContainerOpen;
        private set => SetProperty(ref _isContainerOpen, value);
    }

    public bool IsRecipeOpen
    {
        get => _isRecipeOpen;
        private set => SetProperty(ref _isRecipeOpen, value);
    }

    // RichButton sets these through TwoWay bindings; the owning operation clears them.
    public bool IsContainerBusy
    {
        get => _isContainerBusy;
        set => SetProperty(ref _isContainerBusy, value);
    }

    public bool IsExportBusy
    {
        get => _isExportBusy;
        set => SetProperty(ref _isExportBusy, value);
    }

    public bool IsInspectorBusy
    {
        get => _isInspectorBusy;
        set => SetProperty(ref _isInspectorBusy, value);
    }

    public bool IsRecipeBusy
    {
        get => _isRecipeBusy;
        set => SetProperty(ref _isRecipeBusy, value);
    }

    public bool IsRemoveBusy
    {
        get => _isRemoveBusy;
        set => SetProperty(ref _isRemoveBusy, value);
    }

    public bool IsResetBusy
    {
        get => _isResetBusy;
        set => SetProperty(ref _isResetBusy, value);
    }

    public FlexJustify JustifyContent
    {
        get => _justifyContent;
        set
        {
            if (SetProperty(ref _justifyContent, value))
            {
                RefreshRecipe();
            }
        }
    }

    public string LayoutSummary
    {
        get => _layoutSummary;
        private set => SetProperty(ref _layoutSummary, value);
    }

    public double PreviewMinimumHeight
    {
        get => _previewMinimumHeight;
        set
        {
            if (SetProperty(ref _previewMinimumHeight, value))
            {
                RefreshRecipe();
            }
        }
    }

    public double PreviewPadding
    {
        get => _previewPadding;
        set
        {
            if (SetProperty(ref _previewPadding, value))
            {
                RefreshRecipe();
            }
        }
    }

    public string RecipeError
    {
        get => _recipeError;
        private set => SetProperty(ref _recipeError, value);
    }

    public string RecipeXaml
    {
        get => _recipeXaml;
        private set => SetProperty(ref _recipeXaml, value);
    }

    public FlexItemViewModel? SelectedItem
    {
        get => _selectedItem;
        private set
        {
            if (ReferenceEquals(_selectedItem, value))
            {
                return;
            }

            if (_selectedItem is not null)
            {
                _selectedItem.IsSelected = false;
            }

            if (SetProperty(ref _selectedItem, value) && value is not null)
            {
                value.IsSelected = true;
            }

            ShapeSelectedCommand.RaiseCanExecuteChanged();
            RemoveSelectedCommand.RaiseCanExecuteChanged();
        }
    }

    public FlexWrap Wrap
    {
        get => _wrap;
        set
        {
            if (SetProperty(ref _wrap, value))
            {
                RefreshRecipe();
            }
        }
    }

    public Task OpenInspectorAsync(FlexItemViewModel item) =>
        RunEditorOperationAsync(busy => IsInspectorBusy = busy, async () =>
        {
            if (item is null || !Items.Contains(item))
            {
                return;
            }

            await CloseContainerCoreAsync();
            await CloseRecipeCoreAsync();
            SelectedItem = item;
            await ActivePanelBoss.InputPanels_ActivatePanelByName(InspectorPanelName);
            IsInspectorOpen = true;
        });

    public void Select(FlexItemViewModel item)
    {
        if (Items.Contains(item))
        {
            SelectedItem = item;
        }
    }

    public void OnNavigatedFrom(INavigationParameters parameters)
    {
    }

    public void OnNavigatedTo(INavigationParameters parameters)
    {
    }

    private void AddItem()
    {
        var text = $"Item {_nextId}";
        Items.Add(new FlexItemViewModel(_nextId++, text, BasisOptions[0]));
    }

    private Task CloseContainerControlsAsync() =>
        RunEditorOperationAsync(busy => IsContainerBusy = busy, CloseContainerCoreAsync);

    private async Task CloseContainerCoreAsync()
    {
        if (IsContainerOpen)
        {
            await ActivePanelBoss.HeaderPanels_DeActivatePanelByName(ContainerControlsPanelName);
            IsContainerOpen = false;
        }
    }

    private Task CloseInspectorAsync() =>
        RunEditorOperationAsync(busy => IsInspectorBusy = busy, CloseInspectorCoreAsync);

    private async Task CloseInspectorCoreAsync()
    {
        if (IsInspectorOpen)
        {
            await ActivePanelBoss.InputPanels_DeActivatePanelByName(InspectorPanelName);
            IsInspectorOpen = false;
        }
    }

    private Task CloseRecipeAsync() =>
        RunEditorOperationAsync(busy => IsRecipeBusy = busy, CloseRecipeCoreAsync);

    private async Task CloseRecipeCoreAsync()
    {
        if (IsRecipeOpen)
        {
            await ActivePanelBoss.InputPanels_DeActivatePanelByName(RecipePanelName);
            IsRecipeOpen = false;
        }
    }

    private async Task ExportAsync()
    {
        IsExportBusy = true;
        var itemCount = Items.Count;
        var revision = _recipeRevision;
        try
        {
            await _recipeExporter.CopyAsync(this);
            ExportStatus = revision == _recipeRevision
                ? $"Copied {itemCount} items at {DateTime.Now:T}"
                : "Recipe changed while copying. Copy again for the latest edits.";
        }
        catch (ArgumentException)
        {
            ExportStatus = "Could not export: check item text and layout values.";
        }
        catch (Exception)
        {
            ExportStatus = "Clipboard unavailable. Your recipe is safe; try copying again.";
        }
        finally
        {
            IsExportBusy = false;
        }
    }

    private Task PreviewRecipeAsync() =>
        RunEditorOperationAsync(busy => IsRecipeBusy = busy, async () =>
        {
            IsFavoritesView = false;
            await CloseContainerCoreAsync();
            await CloseInspectorCoreAsync();
            RefreshRecipe(markChanged: false);
            await ActivePanelBoss.InputPanels_ActivatePanelByName(RecipePanelName);
            IsRecipeOpen = true;
        });

    private Task RemoveSelectedAsync() =>
        RunEditorOperationAsync(busy => IsRemoveBusy = busy, async () =>
        {
            if (SelectedItem is { } item)
            {
                await CloseInspectorCoreAsync();
                Items.Remove(item);
            }
        });

    private Task ResetAsync() =>
        RunEditorOperationAsync(busy => IsResetBusy = busy, async () =>
        {
            await ActivePanelBoss.RestoreDefaultPanelChromeAsync();
            ResetState();
        });

    private void ResetState()
    {
        _isResetting = true;
        try
        {
            Direction = FlexDirection.Row;
            Wrap = FlexWrap.Wrap;
            JustifyContent = FlexJustify.Start;
            AlignItems = FlexAlignItems.Stretch;
            AlignContent = FlexAlignContent.Stretch;
            PreviewPadding = 18;
            PreviewMinimumHeight = 0;
            Items.Clear();
            SelectedItem = null;
            IsContainerOpen = false;
            IsInspectorOpen = false;
            IsRecipeOpen = false;
            _nextId = 1;

            foreach (var text in new[] { "One", "Two is longer", "Three", "Four", "Five" })
            {
                Items.Add(new FlexItemViewModel(_nextId++, text, BasisOptions[0]));
            }
        }
        finally
        {
            _isResetting = false;
        }

        RefreshRecipe();
        ExportStatus = "Defaults restored: Grow 0 · Shrink 1 · Basis Auto";
    }

    private Task ToggleContainerControlsAsync() =>
        RunEditorOperationAsync(busy => IsContainerBusy = busy, async () =>
        {
            await CloseInspectorCoreAsync();
            await CloseRecipeCoreAsync();
            await ActivePanelBoss.HeaderPanels_TogglePanelByName(ContainerControlsPanelName);
            IsContainerOpen = !IsContainerOpen;
        });

    private async Task RunEditorOperationAsync(Action<bool> setBusy, Func<Task> operation)
    {
        setBusy(true);
        await _editorGate.WaitAsync();
        try
        {
            setBusy(true);
            await operation();
        }
        finally
        {
            setBusy(false);
            _editorGate.Release();
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Reset notifications do not contain removed items. Keep an explicit set
        // so cleared/replaced items no longer retain this page or refresh its recipe.
        foreach (var item in _observedItems.Where(item => !Items.Contains(item)).ToArray())
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            _observedItems.Remove(item);
        }

        foreach (var item in Items)
        {
            if (_observedItems.Add(item))
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        if (SelectedItem is { } selected && !Items.Contains(selected))
        {
            SelectedItem = null;
        }

        EditItemCommand.RaiseCanExecuteChanged();
        RefreshRecipe();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(FlexItemViewModel.IsSelected)
            and not nameof(FlexItemViewModel.BasisOption))
        {
            RefreshRecipe();
        }
    }

    private void RefreshRecipe(bool markChanged = true)
    {
        if (_isResetting)
        {
            return;
        }

        LayoutSummary = $"{Items.Count} {(Items.Count == 1 ? "item" : "items")} · {Direction} · {Wrap} · Justify {JustifyContent} · Align {AlignItems}";
        if (markChanged)
        {
            _recipeRevision++;
            ExportStatus = "Recipe updated · ready to copy";
        }

        try
        {
            RecipeXaml = _recipeExporter.Build(this);
            RecipeError = string.Empty;
        }
        catch (ArgumentException)
        {
            RecipeXaml = string.Empty;
            RecipeError = "Could not build XAML. Check item text and layout values.";
        }
        catch (Exception)
        {
            RecipeXaml = string.Empty;
            RecipeError = "XAML preview unavailable. Your layout is safe; try opening the preview again.";
        }
    }
}
