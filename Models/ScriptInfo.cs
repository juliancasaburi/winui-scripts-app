using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace winui_scripts_app.Models
{
    public class ScriptInfo : INotifyPropertyChanged
    {
        private bool _isExecuting;
        private DateTime? _lastExecuted;

        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        
        public DateTime? LastExecuted
        {
            get => _lastExecuted;
            set
            {
                if (SetProperty(ref _lastExecuted, value))
                {
                    OnPropertyChanged(nameof(LastExecutedDisplay));
                }
            }
        }
        
        public long FileSize { get; set; }
        
        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                if (SetProperty(ref _isExecuting, value))
                {
                    OnPropertyChanged(nameof(IsNotExecuting));
                }
            }
        }

        public string LastExecutedDisplay
        {
            get
            {
                if (LastExecuted == null)
                    return "Never executed";
                var dt = LastExecuted.Value;
                var now = DateTime.Now;
                if (dt.Date == now.Date)
                    return $"Today at {dt:HH:mm}";
                if (dt.Date == now.Date.AddDays(-1))
                    return $"Yesterday at {dt:HH:mm}";
                return $"on {dt:MMM dd, yyyy} at {dt:HH:mm}";
            }
        }
        
        public string FileSizeDisplay => FileSize < 1024 ? $"{FileSize} bytes" : $"{FileSize / 1024.0:F1} KB";
        public bool IsNotExecuting => !IsExecuting;

        public string Folder { get; set; } = string.Empty;
        
        // Enhanced folder display properties
        public string DisplayFolder => string.IsNullOrEmpty(Folder) ? "Root" : Folder;
        public bool IsInSubfolder => !string.IsNullOrEmpty(Folder);
        public Visibility ShowFolderBadge => IsInSubfolder ? Visibility.Visible : Visibility.Collapsed;
        
        // Group key for folder grouping
        public string GroupKey => string.IsNullOrEmpty(Folder) ? "?? Root Folder" : $"?? {Folder}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}