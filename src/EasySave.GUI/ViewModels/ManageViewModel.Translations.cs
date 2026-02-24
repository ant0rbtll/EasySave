using CommunityToolkit.Mvvm.ComponentModel;
using EasySave.Localization;

namespace EasySave.GUI.ViewModels;

public partial class ManageViewModel
{
    // Localized text properties
    [ObservableProperty]
    private string titleText = string.Empty;

    [ObservableProperty]
    private string subtitleText = string.Empty;

    [ObservableProperty]
    private string searchWatermark = string.Empty;

    [ObservableProperty]
    private string actionsHeader = string.Empty;

    [ObservableProperty]
    private string runningLabel = string.Empty;

    [ObservableProperty]
    private string confirmRunTitle = string.Empty;

    [ObservableProperty]
    private string confirmRunMessage = string.Empty;

    [ObservableProperty]
    private string confirmDeleteTitle = string.Empty;

    [ObservableProperty]
    private string confirmDeleteMessage = string.Empty;

    [ObservableProperty]
    private string btnConfirmText = string.Empty;

    [ObservableProperty]
    private string btnCancelText = string.Empty;

    [ObservableProperty]
    private string btnDeleteText = string.Empty;

    [ObservableProperty]
    private string tooltipRun = string.Empty;

    [ObservableProperty]
    private string tooltipModify = string.Empty;

    [ObservableProperty]
    private string tooltipDelete = string.Empty;

    [ObservableProperty]
    private string playText = string.Empty;

    [ObservableProperty]
    private string pauseText = string.Empty;

    [ObservableProperty]
    private string stopText = string.Empty;

    [ObservableProperty]
    private string tooltipPlay = string.Empty;

    [ObservableProperty]
    private string tooltipPause = string.Empty;

    [ObservableProperty]
    private string tooltipStop = string.Empty;

    [ObservableProperty]
    private string runSelectedText = string.Empty;

    [ObservableProperty]
    private string selectAllTooltip = string.Empty;

    [ObservableProperty]
    private string confirmRunSelectedMessage = string.Empty;

    [ObservableProperty]
    private string deleteSelectedText = string.Empty;

    [ObservableProperty]
    private string confirmDeleteSelectedMessage = string.Empty;

    [ObservableProperty]
    private string emptyTitleText = string.Empty;

    [ObservableProperty]
    private string emptySubtitleText = string.Empty;

    // Status filter texts
    private string filterByStatusLabel = string.Empty;
    private string filterAllStatusLabel = string.Empty;
    private string filterStatusActive = string.Empty;
    private string filterStatusPaused = string.Empty;
    private string filterStatusBlocked = string.Empty;
    private string filterStatusInactive = string.Empty;
    private string filterStatusDone = string.Empty;
    private string filterStatusError = string.Empty;
    private string filterStatusWaiting = string.Empty;
    private string filterStatusDefault = string.Empty;

    private string _idLabel = "ID";
    private string _statusLabel = string.Empty;
    private string _nameLabel = string.Empty;
    private string _sourceLabel = string.Empty;
    private string _destinationLabel = string.Empty;
    private string _typeLabel = string.Empty;
    private string _lastRunLabel = string.Empty;

    public string IdHeader => _idLabel + GetSortIndicator("Id");
    public string StatusHeader => _statusLabel + GetSortIndicator("Status");
    public string NameHeader => _nameLabel + GetSortIndicator("Name");
    public string SourceHeader => _sourceLabel + GetSortIndicator("Source");
    public string DestinationHeader => _destinationLabel + GetSortIndicator("Destination");
    public string TypeHeader => _typeLabel + GetSortIndicator("Type");
    public string LastRunHeader => _lastRunLabel + GetSortIndicator("LastRun");

    public string GetSortIndicator(string column)
    {
        if (SortColumn != column) return "";
        return SortAscending ? " \u25b2" : " \u25bc";
    }

    public void RefreshTranslations()
    {
        TitleText = _localizationService.TranslateText(LocalizationKey.gui_manage_title);
        SubtitleText = _localizationService.TranslateText(LocalizationKey.gui_manage_subtitle);
        SearchWatermark = _localizationService.TranslateText(LocalizationKey.gui_manage_search);
        ActionsHeader = _localizationService.TranslateText(LocalizationKey.gui_manage_actions);
        RunningLabel = _localizationService.TranslateText(LocalizationKey.gui_manage_running);
        ConfirmRunTitle = _localizationService.TranslateText(LocalizationKey.gui_manage_confirm_run_title);
        ConfirmRunMessage = _localizationService.TranslateText(LocalizationKey.gui_manage_confirm_run_message);
        ConfirmDeleteTitle = _localizationService.TranslateText(LocalizationKey.gui_manage_confirm_delete_title);
        ConfirmDeleteMessage = _localizationService.TranslateText(LocalizationKey.gui_manage_confirm_delete_message);
        BtnConfirmText = _localizationService.TranslateText(LocalizationKey.gui_manage_btn_confirm);
        BtnCancelText = _localizationService.TranslateText(LocalizationKey.gui_manage_btn_cancel);
        BtnDeleteText = _localizationService.TranslateText(LocalizationKey.gui_manage_btn_delete);
        TooltipRun = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_run);
        TooltipModify = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_modify);
        TooltipDelete = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_delete);
        PlayText = _localizationService.TranslateText(LocalizationKey.gui_manage_play);
        PauseText = _localizationService.TranslateText(LocalizationKey.gui_manage_pause);
        StopText = _localizationService.TranslateText(LocalizationKey.gui_manage_stop);
        TooltipPlay = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_play);
        TooltipPause = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_pause);
        TooltipStop = _localizationService.TranslateText(LocalizationKey.gui_manage_tooltip_stop);
        RunSelectedText = _localizationService.TranslateText(LocalizationKey.gui_manage_run_selected);
        DeleteSelectedText = _localizationService.TranslateText(LocalizationKey.gui_manage_delete_selected);
        SelectAllTooltip = _localizationService.TranslateText(LocalizationKey.gui_manage_select_all);
        EmptyTitleText = _localizationService.TranslateText(LocalizationKey.gui_manage_empty_title);
        EmptySubtitleText = _localizationService.TranslateText(LocalizationKey.gui_manage_empty_subtitle);
        filterByStatusLabel = _localizationService.TranslateText(LocalizationKey.gui_manage_filter_by_status);
        filterAllStatusLabel = _localizationService.TranslateText(LocalizationKey.gui_manage_filter_all_status);
        filterStatusActive = _localizationService.TranslateText(LocalizationKey.backupjob_active);
        filterStatusPaused = _localizationService.TranslateText(LocalizationKey.backupjob_paused);
        filterStatusBlocked = _localizationService.TranslateText(LocalizationKey.backupjob_blocked);
        filterStatusInactive = _localizationService.TranslateText(LocalizationKey.backupjob_inactive);
        filterStatusDone = _localizationService.TranslateText(LocalizationKey.backupjob_done);
        filterStatusError = _localizationService.TranslateText(LocalizationKey.backupjob_error);
        filterStatusWaiting = _localizationService.TranslateText(LocalizationKey.backupjob_waiting);
        filterStatusDefault = _localizationService.TranslateText(LocalizationKey.backupjob_status);

        _idLabel = _localizationService.TranslateText(LocalizationKey.backupjob_id);
        _statusLabel = _localizationService.TranslateText(LocalizationKey.backupjob_status);
        _nameLabel = _localizationService.TranslateText(LocalizationKey.backupjob_name);
        _sourceLabel = _localizationService.TranslateText(LocalizationKey.backupjob_source);
        _destinationLabel = _localizationService.TranslateText(LocalizationKey.backupjob_destination);
        _typeLabel = _localizationService.TranslateText(LocalizationKey.backupjob_type);
        _lastRunLabel = _localizationService.TranslateText(LocalizationKey.backupjob_last_executed);
        OnPropertyChanged(nameof(IdHeader));
        OnPropertyChanged(nameof(StatusHeader));
        OnPropertyChanged(nameof(NameHeader));
        OnPropertyChanged(nameof(SourceHeader));
        OnPropertyChanged(nameof(DestinationHeader));
        OnPropertyChanged(nameof(TypeHeader));
        OnPropertyChanged(nameof(LastRunHeader));
        OnPropertyChanged(nameof(StatusFilters));
        OnPropertyChanged(nameof(TypeFilters));
        InitializeFilters();
        ApplyJobs(_displayService.FetchJobs());
    }

    public string FilterAllStatusLabel => filterAllStatusLabel;
    public string FilterStatusActive => filterStatusActive;
    public string FilterStatusPaused => filterStatusPaused;
    public string FilterStatusBlocked => filterStatusBlocked;
    public string FilterStatusInactive => filterStatusInactive;
    public string FilterStatusDone => filterStatusDone;
    public string FilterStatusError => filterStatusError;
    public string FilterStatusWaiting => filterStatusWaiting;
    public string FilterStatusDefault => filterStatusDefault;
}
