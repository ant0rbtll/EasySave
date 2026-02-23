using EasySave.Core;

namespace EasySave.GUI.Models
{
	public class BackupJob : ModelBase
	{
		public int Id { get; set; }

		private string name = string.Empty;

		public string Name
		{
			get { return name; }
			set { name = value; OnNotifyPropertyChanged(nameof(Name)); }
		}

		private string source = string.Empty;

		public string Source
		{
			get { return source; }
			set { source = value; OnNotifyPropertyChanged(nameof(Source)); }
		}

		private string destination = string.Empty;

		public string Destination
		{
			get { return destination; }
			set { destination = value; OnNotifyPropertyChanged(nameof(Destination)); }
		}

		private BackupType type;

		public BackupType Type
		{
			get { return type; }
			set { type = value; OnNotifyPropertyChanged(nameof(Type)); }
		}

		private DateTime? lastExecutionDate;

		public DateTime? LastExecutionDate
		{
			get { return lastExecutionDate; }
			set { lastExecutionDate = value; OnNotifyPropertyChanged(nameof(LastExecutionDate)); }
		}

		private string lastExecutionDisplay = string.Empty;

		public string LastExecutionDisplay
		{
			get { return lastExecutionDisplay; }
			set { lastExecutionDisplay = value; OnNotifyPropertyChanged(nameof(LastExecutionDisplay)); }
		}

		private bool isActive;

		public bool IsActive
		{
			get { return isActive; }
			set { isActive = value; OnNotifyPropertyChanged(nameof(IsActive)); }
		}

		private BackupJobStatus status = BackupJobStatus.Inactive;

		public BackupJobStatus Status
		{
			get { return status; }
			set { status = value; OnNotifyPropertyChanged(nameof(Status)); }
		}

		private string statusDisplay = string.Empty;

		public string StatusDisplay
		{
			get { return statusDisplay; }
			set { statusDisplay = value; OnNotifyPropertyChanged(nameof(StatusDisplay)); }
		}

		private string statusTag = BackupJobStatus.Inactive.ToString();

		public string StatusTag
		{
			get { return statusTag; }
			set { statusTag = value; OnNotifyPropertyChanged(nameof(StatusTag)); }
		}

		private bool isSelected;

		public bool IsSelected
		{
			get { return isSelected; }
			set { isSelected = value; OnNotifyPropertyChanged(nameof(IsSelected)); }
		}
	}
}
