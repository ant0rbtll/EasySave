namespace EasySave.GUI.Models;

public class ActiveBackupProgressItem : ModelBase
{
    public int Id { get; set; }

    private string name = string.Empty;
    public string Name
    {
        get => name;
        set { name = value; OnNotifyPropertyChanged(nameof(Name)); }
    }

    private int progressPercent;
    public int ProgressPercent
    {
        get => progressPercent;
        set { progressPercent = value; OnNotifyPropertyChanged(nameof(ProgressPercent)); }
    }

    private string progressDisplay = "0%";
    public string ProgressDisplay
    {
        get => progressDisplay;
        set { progressDisplay = value; OnNotifyPropertyChanged(nameof(ProgressDisplay)); }
    }

    private string filesDisplay = string.Empty;
    public string FilesDisplay
    {
        get => filesDisplay;
        set { filesDisplay = value; OnNotifyPropertyChanged(nameof(FilesDisplay)); }
    }

    private string sizeDisplay = string.Empty;
    public string SizeDisplay
    {
        get => sizeDisplay;
        set { sizeDisplay = value; OnNotifyPropertyChanged(nameof(SizeDisplay)); }
    }

    private string currentSourcePath = string.Empty;
    public string CurrentSourcePath
    {
        get => currentSourcePath;
        set { currentSourcePath = value; OnNotifyPropertyChanged(nameof(CurrentSourcePath)); }
    }

    private string currentDestinationPath = string.Empty;
    public string CurrentDestinationPath
    {
        get => currentDestinationPath;
        set { currentDestinationPath = value; OnNotifyPropertyChanged(nameof(CurrentDestinationPath)); }
    }

    private string updatedAtDisplay = string.Empty;
    public string UpdatedAtDisplay
    {
        get => updatedAtDisplay;
        set { updatedAtDisplay = value; OnNotifyPropertyChanged(nameof(UpdatedAtDisplay)); }
    }

    private string runtimeStatusText = string.Empty;
    public string RuntimeStatusText
    {
        get => runtimeStatusText;
        set { runtimeStatusText = value; OnNotifyPropertyChanged(nameof(RuntimeStatusText)); }
    }

    private string runtimeStatusBackground = "#224A90E2";
    public string RuntimeStatusBackground
    {
        get => runtimeStatusBackground;
        set { runtimeStatusBackground = value; OnNotifyPropertyChanged(nameof(RuntimeStatusBackground)); }
    }

    private string runtimeStatusForeground = "#CFE8FF";
    public string RuntimeStatusForeground
    {
        get => runtimeStatusForeground;
        set { runtimeStatusForeground = value; OnNotifyPropertyChanged(nameof(RuntimeStatusForeground)); }
    }

    private bool canPlay;
    public bool CanPlay
    {
        get => canPlay;
        set { canPlay = value; OnNotifyPropertyChanged(nameof(CanPlay)); }
    }

    private bool canPause = true;
    public bool CanPause
    {
        get => canPause;
        set { canPause = value; OnNotifyPropertyChanged(nameof(CanPause)); }
    }

    private bool canStop = true;
    public bool CanStop
    {
        get => canStop;
        set { canStop = value; OnNotifyPropertyChanged(nameof(CanStop)); }
    }
}
