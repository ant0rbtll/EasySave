using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySave.Application;
using EasySave.Application.Services;
using EasySave.Backup;
using EasySave.Localization;
using EasySave.Log;

namespace EasySave.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    public SidebarViewModel Sidebar { get; }

    [ObservableProperty]
    private ViewModelBase currentPage;

    [ObservableProperty]
    private bool useGlobalScroll;

    public ViewModelBase? ScrollablePage => UseGlobalScroll ? CurrentPage : null;

    public ViewModelBase? FixedPage => UseGlobalScroll ? null : CurrentPage;

    [ObservableProperty]
    private string tooltipMinimize = string.Empty;

    [ObservableProperty]
    private string tooltipMaximize = string.Empty;

    [ObservableProperty]
    private string tooltipClose = string.Empty;

    [ObservableProperty]
    private bool isLogServerWarningVisible;

    [ObservableProperty]
    private string logServerWarningMessage = string.Empty;

    [ObservableProperty]
    private bool isCloseDialogOpen;

    [ObservableProperty]
    private string closeDialogTitle = string.Empty;

    [ObservableProperty]
    private string closeDialogMessage = string.Empty;

    [ObservableProperty]
    private string closeDialogConfirmText = string.Empty;

    [ObservableProperty]
    private string closeDialogCancelText = string.Empty;

    private readonly ILocalizationService _localizationService;
    private readonly ILogServerStatusNotifier _logServerStatusNotifier;
    private readonly BackupApplicationService _backupApplicationService;
    private readonly IBackupExecutionController _backupExecutionController;
    private readonly HomeViewModel _homeViewModel;
    private readonly CreateViewModel _createViewModel;
    private readonly ManageViewModel _manageViewModel;
    private readonly ProgressViewModel _progressViewModel;
    private readonly LogViewModel _logViewModel;
    private readonly ConfigViewModel _configViewModel;

    private string _tooltipMaximizeText = string.Empty;
    private string _tooltipRestoreText = string.Empty;

    public event EventHandler? CloseConfirmed;

    public MainWindowViewModel(
        CreateViewModel createViewModel,
        ManageViewModel manageViewModel,
        ProgressViewModel progressViewModel,
        LogViewModel logViewModel,
        ConfigViewModel configViewModel,
        SidebarViewModel sidebarViewModel,
        HomeViewModel homeViewModel,
        ILocalizationService localizationService,
        ILogServerStatusNotifier logServerStatusNotifier,
        BackupApplicationService backupApplicationService,
        IBackupExecutionController backupExecutionController
        )
    {
        _createViewModel = createViewModel;
        _manageViewModel = manageViewModel;
        _progressViewModel = progressViewModel;
        _logViewModel = logViewModel;
        _logViewModel.SetOnLanguageChanged(OnLanguageChanged);
        _configViewModel = configViewModel;
        _configViewModel.SetOnLanguageChanged(OnLanguageChanged);
        _homeViewModel = homeViewModel;
        _localizationService = localizationService;
        _logServerStatusNotifier = logServerStatusNotifier;
        _logServerStatusNotifier.ServerStatusChanged += OnLogServerStatusChanged;

        if (!_logServerStatusNotifier.IsServerReachable)
        {
            LogServerWarningMessage = _localizationService.TranslateText(
                LocalizationKey.gui_log_server_unreachable);
            IsLogServerWarningVisible = true;
        }
        _backupApplicationService = backupApplicationService;
        _backupExecutionController = backupExecutionController;

        _createViewModel.OnJobCreated = () => _manageViewModel.LoadJobsCommand.Execute(null);

        Sidebar = sidebarViewModel;
        Sidebar.SetNavigateAction(Navigate);
        CurrentPage = _homeViewModel;

        RefreshTranslations();
    }

    public void RefreshTranslations()
    {
        TooltipMinimize = _localizationService.TranslateText(LocalizationKey.gui_window_minimize);
        _tooltipMaximizeText = _localizationService.TranslateText(LocalizationKey.gui_window_maximize);
        _tooltipRestoreText = _localizationService.TranslateText(LocalizationKey.gui_window_restore);
        TooltipClose = _localizationService.TranslateText(LocalizationKey.gui_window_close);
        TooltipMaximize = _tooltipMaximizeText;

        if (IsLogServerWarningVisible && !_logServerStatusNotifier.IsServerReachable)
        {
            LogServerWarningMessage = _localizationService.TranslateText(
                LocalizationKey.gui_log_server_unreachable);
        }
        CloseDialogTitle = _localizationService.TranslateText(LocalizationKey.gui_close_confirm_title);
        CloseDialogMessage = _localizationService.TranslateText(LocalizationKey.gui_close_confirm_message);
        CloseDialogConfirmText = _localizationService.TranslateText(LocalizationKey.gui_close_confirm_confirm);
        CloseDialogCancelText = _localizationService.TranslateText(LocalizationKey.gui_close_confirm_cancel);
    }

    public void UpdateMaximizeTooltip(bool isMaximized)
    {
        TooltipMaximize = isMaximized ? _tooltipRestoreText : _tooltipMaximizeText;
    }

    private void OnLanguageChanged()
    {
        RefreshTranslations();
        Sidebar.RefreshTranslations();
        _configViewModel.RefreshTranslations();
        _logViewModel.RefreshTranslations();
        _homeViewModel.RefreshTranslations();
        _manageViewModel.RefreshTranslations();
        _progressViewModel.RefreshTranslations();
        _createViewModel.RefreshTranslations();
    }
    public void Navigate(string page)
    {
        if (string.Equals(page, "log", StringComparison.Ordinal))
        {
            _logViewModel.LoadDatesCommand.Execute(null);
        }

        CurrentPage = page switch
        {
            "creation" => _createViewModel,
            "manage" => _manageViewModel,
            "progress" => _progressViewModel,
            "log" => _logViewModel,
            "conf" => _configViewModel,
            _ => CurrentPage
        };
    }

    partial void OnCurrentPageChanged(ViewModelBase value)
    {
        // Keep global scroll only on long form pages.
        // Other pages keep their own centered layout without a scroll container.
        var shouldUseGlobalScroll = value is CreateViewModel or ConfigViewModel;
        if (UseGlobalScroll != shouldUseGlobalScroll)
        {
            UseGlobalScroll = shouldUseGlobalScroll;
            return;
        }

        NotifyPageTargetsChanged();
    }

    partial void OnUseGlobalScrollChanged(bool value)
    {
        NotifyPageTargetsChanged();
    }

    private void NotifyPageTargetsChanged()
    {
        OnPropertyChanged(nameof(ScrollablePage));
        OnPropertyChanged(nameof(FixedPage));
    }

    public void Dispose()
    {
        _logServerStatusNotifier.ServerStatusChanged -= OnLogServerStatusChanged;
    }

    private void OnLogServerStatusChanged(bool isReachable)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (isReachable)
            {
                IsLogServerWarningVisible = false;
            }
            else
            {
                LogServerWarningMessage = _localizationService.TranslateText(
                    LocalizationKey.gui_log_server_unreachable);
                IsLogServerWarningVisible = true;
            }
        });
    }

    public bool HasRunningOrPausedBackups()
    {
        try
        {
            return _backupApplicationService
                .GetAllJobsRuntimeStates()
                .Values
                .Any(state => state.IsActive);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void StopAllBackups()
    {
        _backupExecutionController.RequestStopAll();
    }

    public void OpenCloseConfirmationDialog()
    {
        IsCloseDialogOpen = true;
    }

    [RelayCommand]
    private void CancelCloseDialog()
    {
        IsCloseDialogOpen = false;
    }

    [RelayCommand]
    private void ConfirmCloseDialog()
    {
        StopAllBackups();
        IsCloseDialogOpen = false;
        CloseConfirmed?.Invoke(this, EventArgs.Empty);
    }
}
