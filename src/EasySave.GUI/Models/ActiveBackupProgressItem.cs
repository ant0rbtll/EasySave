namespace EasySave.GUI.Models;

public class ActiveBackupProgressItem : ModelBase
{
    public int Id { get; set; }

    private string name = string.Empty;
    public string Name
    {
        get => name;
        set
        {
            if (string.Equals(name, value, StringComparison.Ordinal))
                return;

            name = value;
            OnNotifyPropertyChanged(nameof(Name));
        }
    }

    private int progressPercent;
    public int ProgressPercent
    {
        get => progressPercent;
        set
        {
            if (progressPercent == value)
                return;

            progressPercent = value;
            OnNotifyPropertyChanged(nameof(ProgressPercent));
        }
    }

    private string progressDisplay = "0%";
    public string ProgressDisplay
    {
        get => progressDisplay;
        set
        {
            if (string.Equals(progressDisplay, value, StringComparison.Ordinal))
                return;

            progressDisplay = value;
            OnNotifyPropertyChanged(nameof(ProgressDisplay));
        }
    }

    private string filesDisplay = string.Empty;
    public string FilesDisplay
    {
        get => filesDisplay;
        set
        {
            if (string.Equals(filesDisplay, value, StringComparison.Ordinal))
                return;

            filesDisplay = value;
            OnNotifyPropertyChanged(nameof(FilesDisplay));
        }
    }

    private string sizeDisplay = string.Empty;
    public string SizeDisplay
    {
        get => sizeDisplay;
        set
        {
            if (string.Equals(sizeDisplay, value, StringComparison.Ordinal))
                return;

            sizeDisplay = value;
            OnNotifyPropertyChanged(nameof(SizeDisplay));
        }
    }

    private string currentSourcePath = string.Empty;
    public string CurrentSourcePath
    {
        get => currentSourcePath;
        set
        {
            if (string.Equals(currentSourcePath, value, StringComparison.Ordinal))
                return;

            currentSourcePath = value;
            OnNotifyPropertyChanged(nameof(CurrentSourcePath));
        }
    }

    private string currentDestinationPath = string.Empty;
    public string CurrentDestinationPath
    {
        get => currentDestinationPath;
        set
        {
            if (string.Equals(currentDestinationPath, value, StringComparison.Ordinal))
                return;

            currentDestinationPath = value;
            OnNotifyPropertyChanged(nameof(CurrentDestinationPath));
        }
    }

    private string updatedAtDisplay = string.Empty;
    public string UpdatedAtDisplay
    {
        get => updatedAtDisplay;
        set
        {
            if (string.Equals(updatedAtDisplay, value, StringComparison.Ordinal))
                return;

            updatedAtDisplay = value;
            OnNotifyPropertyChanged(nameof(UpdatedAtDisplay));
        }
    }

    private string runtimeStatusText = string.Empty;
    public string RuntimeStatusText
    {
        get => runtimeStatusText;
        set
        {
            if (string.Equals(runtimeStatusText, value, StringComparison.Ordinal))
                return;

            runtimeStatusText = value;
            OnNotifyPropertyChanged(nameof(RuntimeStatusText));
        }
    }

    private string runtimeStatusBackground = "#224A90E2";
    public string RuntimeStatusBackground
    {
        get => runtimeStatusBackground;
        set
        {
            if (string.Equals(runtimeStatusBackground, value, StringComparison.Ordinal))
                return;

            runtimeStatusBackground = value;
            OnNotifyPropertyChanged(nameof(RuntimeStatusBackground));
        }
    }

    private string runtimeStatusForeground = "#CFE8FF";
    public string RuntimeStatusForeground
    {
        get => runtimeStatusForeground;
        set
        {
            if (string.Equals(runtimeStatusForeground, value, StringComparison.Ordinal))
                return;

            runtimeStatusForeground = value;
            OnNotifyPropertyChanged(nameof(RuntimeStatusForeground));
        }
    }

    private bool canPlay;
    public bool CanPlay
    {
        get => canPlay;
        set
        {
            if (canPlay == value)
                return;

            canPlay = value;
            OnNotifyPropertyChanged(nameof(CanPlay));
        }
    }

    private bool canPause = true;
    public bool CanPause
    {
        get => canPause;
        set
        {
            if (canPause == value)
                return;

            canPause = value;
            OnNotifyPropertyChanged(nameof(CanPause));
        }
    }

    private bool canStop = true;
    public bool CanStop
    {
        get => canStop;
        set
        {
            if (canStop == value)
                return;

            canStop = value;
            OnNotifyPropertyChanged(nameof(CanStop));
        }
    }
}
