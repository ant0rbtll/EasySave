using EasySave.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.GUI.Models
{
    public class BackupJob : ModelBase
    {
        public int Id { get; set; }

        private string name = String.Empty;

		public string Name
		{
			get { return name; }
			set { name = value; OnNotifyPropertyChanged(nameof(Name)); }
		}

		private string source = String.Empty;

		public string Source
		{
			get { return source; }
			set { source = value; OnNotifyPropertyChanged(nameof(Source)); }
		}

		private string destination = String.Empty;

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

		private DateTime? latestExecutionDate = null;

		public DateTime? LastExecutionDate
		{
			get { return latestExecutionDate; }
			set { latestExecutionDate = value; OnNotifyPropertyChanged(nameof(LastExecutionDate)); } 
		}

		private bool isActive = false;

		public bool IsActive
		{
			get { return isActive; }
			set { isActive = value; OnNotifyPropertyChanged(nameof(IsActive)); }
		}
	}
}
