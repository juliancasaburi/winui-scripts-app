using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using winui_scripts_app.Helpers;
using winui_scripts_app.Models;
using winui_scripts_app.Services;

namespace winui_scripts_app.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IScriptService _scriptService;
        private readonly DispatcherQueue _dispatcherQueue;
        private string _statusMessage = "Ready";
        private ScriptInfo? _selectedScript;

        public ObservableCollection<ScriptInfo> Scripts { get; } = new();

        // Grouped collection for folder organization
        public ObservableCollection<ScriptGroup> GroupedScripts { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand RunScriptCommand { get; }
        public ICommand DeleteScriptCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ScriptInfo? SelectedScript
        {
            get => _selectedScript;
            set => SetProperty(ref _selectedScript, value);
        }

        public int ScriptCount => Scripts.Count;
        public int FolderCount => Scripts.GroupBy(s => s.DisplayFolder).Count();

        public MainViewModel(IScriptService scriptService)
        {
            _scriptService = scriptService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            RefreshCommand = new RelayCommand(async () => await RefreshScriptsAsync());
            RunScriptCommand = new RelayCommand<ScriptInfo>(async script => await RunScriptAsync(script));
            DeleteScriptCommand = new RelayCommand<ScriptInfo>(async script => await DeleteScriptAsync(script));
            OpenFolderCommand = new RelayCommand(OpenScriptsFolder);

            // Subscribe to file system events
            _scriptService.ScriptAdded += OnScriptAdded;
            _scriptService.ScriptRemoved += OnScriptRemoved;
            
            // Start watching for file changes
            _scriptService.StartWatching();
            
            // Load initial scripts
            Task.Run(async () => await RefreshScriptsAsync());
        }

        private async Task RefreshScriptsAsync()
        {
            try
            {
                StatusMessage = "Loading scripts...";
                var scripts = await _scriptService.GetScriptsAsync();

                _dispatcherQueue.TryEnqueue(() =>
                {
                    Scripts.Clear();
                    GroupedScripts.Clear();
                    
                    foreach (var script in scripts)
                    {
                        Scripts.Add(script);
                    }
                    
                    // Update grouped collection
                    UpdateGroupedScripts();
                    
                    OnPropertyChanged(nameof(ScriptCount));
                    OnPropertyChanged(nameof(FolderCount));
                    
                    var folderText = FolderCount == 1 ? "folder" : "folders";
                    StatusMessage = $"Loaded {Scripts.Count} scripts in {FolderCount} {folderText}";
                });
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    StatusMessage = $"Error loading scripts: {ex.Message}";
                });
            }
        }

        private void UpdateGroupedScripts()
        {
            GroupedScripts.Clear();
            var groups = ScriptGroup.GroupScripts(Scripts);
            
            foreach (var group in groups)
            {
                GroupedScripts.Add(group);
            }
        }

        private async Task RunScriptAsync(ScriptInfo? script)
        {
            if (script == null) return;

            try
            {
                script.IsExecuting = true;
                OnPropertyChanged(nameof(script.IsExecuting));
                OnPropertyChanged(nameof(script.IsNotExecuting));
                
                StatusMessage = $"Executing {script.Name}...";

                var success = await _scriptService.ExecuteScriptAsync(script.FullPath);
                
                // Update the last executed time
                script.LastExecuted = DateTime.Now;
                OnPropertyChanged(nameof(script.LastExecutedDisplay));

                StatusMessage = success 
                    ? $"Script {script.Name} executed successfully" 
                    : $"Script {script.Name} execution failed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error executing {script.Name}: {ex.Message}";
            }
            finally
            {
                script.IsExecuting = false;
                OnPropertyChanged(nameof(script.IsExecuting));
                OnPropertyChanged(nameof(script.IsNotExecuting));
            }
        }

        private async Task DeleteScriptAsync(ScriptInfo? script)
        {
            if (script == null) return;

            try
            {
                StatusMessage = $"Deleting {script.Name}...";
                
                if (await _scriptService.DeleteScriptAsync(script.FullPath))
                {
                    Scripts.Remove(script);
                    UpdateGroupedScripts();
                    OnPropertyChanged(nameof(ScriptCount));
                    OnPropertyChanged(nameof(FolderCount));
                    StatusMessage = $"Script {script.Name} deleted successfully";
                }
                else
                {
                    StatusMessage = $"Failed to delete {script.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting {script.Name}: {ex.Message}";
            }
        }

        private void OpenScriptsFolder()
        {
            try
            {
                var scriptsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Scripts");
                
                if (!Directory.Exists(scriptsFolder))
                {
                    Directory.CreateDirectory(scriptsFolder);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = scriptsFolder,
                    UseShellExecute = true
                });

                StatusMessage = "Opened scripts folder";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening scripts folder: {ex.Message}";
            }
        }

        private void OnScriptAdded(object? sender, ScriptInfo script)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // Check if script already exists (to avoid duplicates)
                if (!Scripts.Any(s => s.FullPath == script.FullPath))
                {
                    Scripts.Add(script);
                    UpdateGroupedScripts();
                    OnPropertyChanged(nameof(ScriptCount));
                    OnPropertyChanged(nameof(FolderCount));
                    
                    var folderInfo = script.IsInSubfolder ? $" in {script.Folder}" : "";
                    StatusMessage = $"Script {script.Name} added{folderInfo}";
                }
            });
        }

        private void OnScriptRemoved(object? sender, string scriptPath)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                var scriptToRemove = Scripts.FirstOrDefault(s => s.FullPath == scriptPath);
                if (scriptToRemove != null)
                {
                    Scripts.Remove(scriptToRemove);
                    UpdateGroupedScripts();
                    OnPropertyChanged(nameof(ScriptCount));
                    OnPropertyChanged(nameof(FolderCount));
                    
                    var folderInfo = scriptToRemove.IsInSubfolder ? $" from {scriptToRemove.Folder}" : "";
                    StatusMessage = $"Script {scriptToRemove.Name} removed{folderInfo}";
                }
            });
        }

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