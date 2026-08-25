using System.Collections.ObjectModel;
using Prism.Mvvm;
using Prism.Navigation;
using CodeCrafty.DapperDan.Data;
using CodeCrafty.DapperDan.Data.Entities;
using CodeCrafty.DapperDan.Diagnostics;
using CodeCrafty.DapperDan.Models;
using CodeCrafty.DapperDan.PanelBossKit;
using CodeCrafty.DapperDan.Speech;

namespace CodeCrafty.DapperDan.ViewModels;

public partial class DapperDanViewModel : BindableBase
{
    private const string VoiceCanaryPhrase =
        "Dapper Dan voice canary. One two three. Clear speech should sound natural from start to finish.";

    public const string BottomSheetPanelName = "DapperDanBottomSheet";
    public const string ButtonsPanelName = "DapperDanButtonsPanel";
    public const string DialogPanelName = "DapperDanDialogPanel";
    public const string HeaderPanelName = "DapperDanHeaderPanel";
    public const string InspectorPanelName = "DapperDanInspectorPanel";
    public const string LoadingPanelName = "DapperDanLoadingPanel";
    public const string MenuPanelName = "DapperDanMenuPanel";
    public const string MorePanelName = "DapperDanMorePanel";
    public const string MotionPanelName = "DapperDanMotionPanel";
    public const string PalettePanelName = "DapperDanPalettePanel";
    public const string PanelsPanelName = "DapperDanPanelsPanel";
    public const string StatusPanelName = "DapperDanStatusPanel";
    public const string TourPanelName = "DapperDanTourPanel";
    public const string WitnessPanelName = "DapperDanWitnessPanel";

    private readonly IKeikiRepository _keikiRepository;
    private readonly INavigationService _navigationService;
    private readonly IVoiceCanaryService _voiceCanaryService;
    private string _favoriteBreak = "First Light";
    private bool _hasInitialized;
    private bool _isAsyncSpecimenBusy;
    private bool _isKeikiBusy;
    private bool _isLoadingPanelBusy;
    private bool _isVoiceCanaryBusy;
    private string _keikiCountText = "No saved Keiki yet";
    private string _keikiMemory = "Remember the clean little win.";
    private string _keikiName = "Kai";
    private DapperDanPageAction _lastPrimaryAction;
    private string _statusMessage = "Dapper Dan is ready to exercise the public canary.";
    private string _voiceCanaryHeading = "No voice trial yet";
    private string _voiceCanaryReport =
        "Run A, B, and C with the same device volume and output route. Nothing leaves the device.";

    public DapperDanViewModel(
        PanelBoss activePanelBoss,
        IKeikiRepository keikiRepository,
        INavigationService navigationService,
        IVoiceCanaryService voiceCanaryService)
    {
        ActivePanelBoss = activePanelBoss;
        _keikiRepository = keikiRepository;
        _navigationService = navigationService;
        _voiceCanaryService = voiceCanaryService;

        ButtonsAction = new DapperDanPageAction(
            "Buttons",
            "RB",
            ButtonsPanelName,
            "DapperDan_Action_Buttons");
        PanelsAction = new DapperDanPageAction(
            "Panels",
            "PB",
            PanelsPanelName,
            "DapperDan_Action_Panels");
        MotionAction = new DapperDanPageAction(
            "Motion",
            "~",
            MotionPanelName,
            "DapperDan_Action_Motion");
        WitnessAction = new DapperDanPageAction(
            "Canary",
            "✓",
            WitnessPanelName,
            "DapperDan_Action_Witness");
        MoreAction = new DapperDanPageAction(
            "More",
            "...",
            MorePanelName,
            "DapperDan_Action_More");

        PageActions =
        [
            ButtonsAction,
            PanelsAction,
            MotionAction,
            WitnessAction,
            MoreAction,
        ];

        _lastPrimaryAction = ButtonsAction;
        SelectOnly(ButtonsAction);
    }

    public PanelBoss ActivePanelBoss { get; }

    public DapperDanPageAction ButtonsAction { get; }

    public string FavoriteBreak
    {
        get => _favoriteBreak;
        set => SetProperty(ref _favoriteBreak, value);
    }

    public bool IsAsyncSpecimenBusy
    {
        get => _isAsyncSpecimenBusy;
        set => SetProperty(ref _isAsyncSpecimenBusy, value);
    }

    public bool IsKeikiBusy
    {
        get => _isKeikiBusy;
        set => SetProperty(ref _isKeikiBusy, value);
    }

    public bool IsLoadingPanelBusy
    {
        get => _isLoadingPanelBusy;
        set => SetProperty(ref _isLoadingPanelBusy, value);
    }

    public bool IsVoiceCanaryBusy
    {
        get => _isVoiceCanaryBusy;
        set => SetProperty(ref _isVoiceCanaryBusy, value);
    }

    public bool IsVoiceCanarySupported => _voiceCanaryService.IsSupported;

    public ObservableCollection<Keiki> Keiki { get; } = [];

    public string KeikiCountText
    {
        get => _keikiCountText;
        private set => SetProperty(ref _keikiCountText, value);
    }

    public string KeikiMemory
    {
        get => _keikiMemory;
        set => SetProperty(ref _keikiMemory, value);
    }

    public string KeikiName
    {
        get => _keikiName;
        set => SetProperty(ref _keikiName, value);
    }

    public DapperDanPageAction MoreAction { get; }

    public DapperDanPageAction MotionAction { get; }

    public IReadOnlyList<DapperDanPageAction> PageActions { get; }

    public DapperDanPageAction PanelsAction { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public DapperDanPageAction WitnessAction { get; }

    public string VoiceCanaryHeading
    {
        get => _voiceCanaryHeading;
        private set => SetProperty(ref _voiceCanaryHeading, value);
    }

    public string VoiceCanaryReport
    {
        get => _voiceCanaryReport;
        private set => SetProperty(ref _voiceCanaryReport, value);
    }

    private async Task OpenRotationCanaryAsync()
    {
        var result = await _navigationService.NavigateAsync("RotationCanaryPage");
        if (!result.Success)
        {
            StatusMessage =
                $"Rotation canary navigation failed: {result.Exception?.Message ?? "unknown navigation error"}";
        }
    }

    private async Task OpenBillboardCanaryAsync()
    {
        var result = await _navigationService.NavigateAsync("BillboardCanaryPage");
        if (!result.Success)
        {
            StatusMessage =
                $"Billboard canary navigation failed: {result.Exception?.Message ?? "unknown navigation error"}";
        }
    }

    public async Task InitializeAsync()
    {
        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;
        CrashJournal.Checkpoint(CrashPoint.ViewModelInitializeEnter);

        try
        {
            await LoadKeikiCoreAsync();
            StatusMessage = $"Compiled EF model + packaged SQLite v{DapperDanDatabaseMetadata.SchemaVersion} ready; {Keiki.Count} Keiki loaded.";
            CrashJournal.Checkpoint(CrashPoint.ViewModelInitializeReady);
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.HandledStartupFailure,
                CrashPoint.ViewModelInitializeHandledFailure,
                exception,
                terminating: false);
            StatusMessage = $"Keiki store is not ready: {exception.Message}";
        }
    }

    private async Task AddKeikiAsync()
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(KeikiName)
                ? $"Keiki {Keiki.Count + 1}"
                : KeikiName.Trim();
            var favoriteBreak = string.IsNullOrWhiteSpace(FavoriteBreak)
                ? "Open water"
                : FavoriteBreak.Trim();
            var memory = string.IsNullOrWhiteSpace(KeikiMemory)
                ? "A small durable memory."
                : KeikiMemory.Trim();

            await _keikiRepository.AddAsync(name, favoriteBreak, memory);
            await LoadKeikiCoreAsync();
            StatusMessage = $"Saved {name} through EF Core and SQLite.";
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.HandledDataFailure,
                CrashPoint.DatabaseInitializeEnter,
                exception,
                terminating: false);
            StatusMessage = $"Save failed: {exception.Message}";
        }
        finally
        {
            IsKeikiBusy = false;
        }
    }

    private async Task ClearKeikiAsync()
    {
        try
        {
            await _keikiRepository.ClearAsync();
            await LoadKeikiCoreAsync();
            StatusMessage = "The local Keiki table is clear.";
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.HandledDataFailure,
                CrashPoint.DatabaseInitializeEnter,
                exception,
                terminating: false);
            StatusMessage = $"Clear failed: {exception.Message}";
        }
        finally
        {
            IsKeikiBusy = false;
        }
    }

    private async Task ExecutePanelDemoAsync(string action)
    {
        switch (action)
        {
            case "NonVisual":
                StatusMessage = "NonVisualControls: mounted as the hidden diagnostics lane.";
                break;
            case "Tour":
                await ActivePanelBoss.TopHeaderPagePanels_TogglePanelByName(TourPanelName);
                StatusMessage = "TopHeaderPagePanels: full-page tour toggled.";
                break;
            case "Palette":
                await ActivePanelBoss.LeftSelectorPanels_ActivatePanelByName(PalettePanelName);
                StatusMessage = "LeftSelectorPanels: palette opened.";
                break;
            case "ClosePalette":
                await ActivePanelBoss.LeftSelectorPanels_DeActivatePanelByName(PalettePanelName);
                break;
            case "Inspector":
                await ActivePanelBoss.RightSelectorPanels_ActivatePanelByName(InspectorPanelName);
                StatusMessage = "RightSelectorPanels: inspector opened.";
                break;
            case "CloseInspector":
                await ActivePanelBoss.RightSelectorPanels_DeActivatePanelByName(InspectorPanelName);
                break;
            case "Menu":
                await ActivePanelBoss.MenuPanels_TogglePanelByName(MenuPanelName);
                StatusMessage = "MenuPanels: smoky menu toggled.";
                break;
            case "CloseMenu":
                await ActivePanelBoss.MenuPanels_DeActivatePanelByName(MenuPanelName);
                break;
            case "Sheet":
                await ActivePanelBoss.BottomInputPanels_TogglePanelByName(BottomSheetPanelName);
                StatusMessage = "BottomInputPanels: viewport sheet toggled.";
                break;
            case "CloseSheet":
                await ActivePanelBoss.BottomInputPanels_DeActivatePanelByName(BottomSheetPanelName);
                break;
            case "Status":
                await ActivePanelBoss.BottomStatusPanels_TogglePanelByName(StatusPanelName);
                StatusMessage = "BottomStatusPanels: transient status toggled.";
                break;
            case "CloseStatus":
                await ActivePanelBoss.BottomStatusPanels_DeActivatePanelByName(StatusPanelName);
                break;
            case "Dialog":
                await ActivePanelBoss.FullScreenPopupPanels_TogglePanelByName(DialogPanelName);
                StatusMessage = "FullScreenPopupPanels: dialog toggled.";
                break;
            case "CloseDialog":
                await ActivePanelBoss.FullScreenPopupPanels_DeActivatePanelByName(DialogPanelName);
                break;
            case "Loading":
                await PulseOwnedLoadingPanelAsync();
                break;
            case "Restore":
                await ActivePanelBoss.RestoreDefaultPanelChromeAsync(MorePanelName, BottomSheetPanelName);
                SelectOnly(_lastPrimaryAction);
                StatusMessage = "Default header, content, and bottom action strip restored.";
                break;
        }
    }

    private async Task LoadKeikiAsync()
    {
        try
        {
            await LoadKeikiCoreAsync();
            StatusMessage = "Reloaded the Keiki rows from SQLite.";
        }
        catch (Exception exception)
        {
            CrashJournal.Capture(
                CrashSource.HandledDataFailure,
                CrashPoint.KeikiQueryEnter,
                exception,
                terminating: false);
            StatusMessage = $"Load failed: {exception.Message}";
        }
        finally
        {
            IsKeikiBusy = false;
        }
    }

    private async Task LoadKeikiCoreAsync()
    {
        var loaded = await _keikiRepository.LoadAsync();

        Keiki.Clear();
        foreach (var item in loaded)
        {
            Keiki.Add(item);
        }

        KeikiCountText = Keiki.Count == 0
            ? "No saved Keiki yet"
            : $"{Keiki.Count} saved Keiki";
    }

    private async Task PulseOwnedLoadingPanelAsync()
    {
        PanelBossFullScreenPopupPanelHandle? handle = null;

        try
        {
            handle = await ActivePanelBoss.FullScreenPopupPanels_ActivateOwnedPanelByName(LoadingPanelName);
            if (handle is null)
            {
                StatusMessage = "Loading panel is not mounted.";
                return;
            }

            var presented = await handle.WaitForPresentationAsync();
            StatusMessage = presented
                ? "Owned loading panel reached its visual presentation fence."
                : "Owned loading panel presentation fence timed out.";
            await Task.Delay(750);
        }
        finally
        {
            if (handle is not null)
            {
                await ActivePanelBoss.FullScreenPopupPanels_DeActivateOwnedPanelAsync(handle);
            }

            IsLoadingPanelBusy = false;
        }
    }

    private async Task RunAsyncSpecimenAsync()
    {
        try
        {
            StatusMessage = "Async RichButton owns busy until the work completes...";
            await Task.Delay(900);
            StatusMessage = "Async RichButton completed and released busy.";
        }
        finally
        {
            IsAsyncSpecimenBusy = false;
        }
    }

    private async Task RunVoiceCanaryAsync(string scenarioName)
    {
        if (!Enum.TryParse<VoiceCanaryScenario>(
                scenarioName,
                ignoreCase: false,
                out var scenario))
        {
            VoiceCanaryHeading = "Unknown voice trial";
            VoiceCanaryReport = $"The scenario '{scenarioName}' is not registered.";
            IsVoiceCanaryBusy = false;
            return;
        }

        var plan = VoiceCanaryPlan.For(scenario);
        IsVoiceCanaryBusy = true;
        VoiceCanaryHeading = $"Running {plan.Label}";
        VoiceCanaryReport =
            "Listen for natural timbre while Dapper Dan records the selected voice and shared audio-session state.";

        try
        {
            var result = await _voiceCanaryService.SpeakAsync(
                scenario,
                VoiceCanaryPhrase);
            VoiceCanaryHeading = $"Completed {result.Plan.Label}";
            VoiceCanaryReport = result.ToDisplayText();
            StatusMessage = $"Voice canary completed: {result.Plan.Label}.";
        }
        catch (OperationCanceledException)
        {
            VoiceCanaryHeading = $"Stopped {plan.Label}";
            VoiceCanaryReport = "Speech stopped before the trial completed.";
            StatusMessage = "Voice canary stopped.";
        }
        catch (Exception exception)
        {
            VoiceCanaryHeading = $"Failed {plan.Label}";
            VoiceCanaryReport = $"{exception.GetType().Name}: {exception.Message}";
            StatusMessage = $"Voice canary failed: {exception.Message}";
        }
        finally
        {
            IsVoiceCanaryBusy = false;
        }
    }

    private void StopVoiceCanary()
    {
        _voiceCanaryService.Stop();
        StatusMessage = "Stopping the active voice canary trial.";
    }

    private async Task SelectPageActionAsync(DapperDanPageAction action)
    {
        try
        {
            if (ReferenceEquals(action, MoreAction))
            {
                if (MoreAction.IsSelected)
                {
                    await ActivePanelBoss.RestoreDefaultPanelChromeAsync(MorePanelName);
                    SelectOnly(_lastPrimaryAction);
                    return;
                }

                await ActivePanelBoss.ContentPanels_ToggleHeaderOwningPanelByName(MorePanelName);
                SelectOnly(MoreAction);
                return;
            }

            _lastPrimaryAction = action;
            await ActivePanelBoss.RestoreDefaultPanelChromeAsync(MorePanelName);
            await ActivePanelBoss.ContentPanels_ActivatePanelByName(action.PanelName);
            SelectOnly(action);
        }
        finally
        {
            action.IsBusy = false;
        }
    }

    private void SelectOnly(DapperDanPageAction selectedAction)
    {
        foreach (var action in PageActions)
        {
            action.SetSelected(ReferenceEquals(action, selectedAction));
        }
    }
}
