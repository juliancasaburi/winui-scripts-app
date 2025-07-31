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
using Windows.Storage.Pickers;
using winui_scripts_app.Helpers;
using winui_scripts_app.Models;
using winui_scripts_app.Services;

namespace winui_scripts_app.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ScriptService _scriptService;
        private readonly ISettingsService _settingsService;
        private readonly DispatcherQueue _dispatcherQueue;
        private string _statusMessage = "Ready";
        private ScriptInfo? _selectedScript;
        private string _searchText = string.Empty;
        private string _currentScriptsFolder = string.Empty;

        // Original collections (unfiltered)
        public ObservableCollection<ScriptInfo> Scripts { get; } = new();
        
        // Grouped collection for folder organization
        public ObservableCollection<ScriptGroup> GroupedScripts { get; } = new();
        
        // Filtered collections for display
        public ObservableCollection<ScriptGroup> FilteredGroupedScripts { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand RunScriptCommand { get; }
        public ICommand DeleteScriptCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ChangeFolderCommand { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplySearch();
                }
            }
        }

        public ScriptInfo? SelectedScript
        {
            get => _selectedScript;
            set => SetProperty(ref _selectedScript, value);
        }

        public string CurrentScriptsFolder
        {
            get => _currentScriptsFolder;
            set => SetProperty(ref _currentScriptsFolder, value);
        }

        public int ScriptCount => Scripts.Count;
        public int FilteredScriptCount => FilteredGroupedScripts.SelectMany(g => g.Scripts).Count();
        public int FolderCount => Scripts.GroupBy(s => s.DisplayFolder).Count();
        public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchText);

        public MainViewModel(ScriptService scriptService, ISettingsService settingsService)
        {
            _scriptService = scriptService;
            _settingsService = settingsService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            RefreshCommand = new RelayCommand(async () => await RefreshScriptsAsync());
            RunScriptCommand = new RelayCommand<ScriptInfo>(async script => await RunScriptAsync(script));
            DeleteScriptCommand = new RelayCommand<ScriptInfo>(async script => await DeleteScriptAsync(script));
            OpenFolderCommand = new RelayCommand(OpenScriptsFolder);
            ClearSearchCommand = new RelayCommand(ClearSearch);
            ChangeFolderCommand = new RelayCommand(async () => await ChangeFolderAsync());

            // Subscribe to file system events
            _scriptService.ScriptAdded += OnScriptAdded;
            _scriptService.ScriptRemoved += OnScriptRemoved;
            _scriptService.FolderDeleted += OnFolderDeleted;
            
            // Subscribe to settings changes
            _settingsService.ScriptsFolderChanged += OnScriptsFolderChanged;
            
            // Initialize current folder display
            CurrentScriptsFolder = _settingsService.ScriptsFolder;
            
            // Start watching for file changes
            _scriptService.StartWatching();
            
            // Load initial scripts
            Task.Run(async () => await RefreshScriptsAsync());
        }

        private void ApplySearch()
        {
            FilteredGroupedScripts.Clear();
            
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // No search - show all groups
                foreach (var group in GroupedScripts)
                {
                    FilteredGroupedScripts.Add(group);
                }
            }
            else
            {
                // Apply search filter
                var searchLower = SearchText.ToLowerInvariant();
                var filteredScripts = Scripts.Where(s => 
                    s.Name.ToLowerInvariant().Contains(searchLower) ||
                    s.Folder.ToLowerInvariant().Contains(searchLower) ||
                    s.DisplayFolder.ToLowerInvariant().Contains(searchLower)
                ).ToList();

                // Create filtered groups
                var filteredGroups = ScriptGroup.GroupScripts(filteredScripts);
                foreach (var group in filteredGroups)
                {
                    FilteredGroupedScripts.Add(group);
                }
            }

            OnPropertyChanged(nameof(FilteredScriptCount));
            OnPropertyChanged(nameof(IsSearchActive));
            
            // Update status message if searching
            if (IsSearchActive)
            {
                var resultText = FilteredScriptCount == 1 ? "script" : "scripts";
                StatusMessage = $"Found {FilteredScriptCount} {resultText} matching '{SearchText}'";
            }
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
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
                    
                    // Apply current search filter
                    ApplySearch();
                    
                    OnPropertyChanged(nameof(ScriptCount));
                    OnPropertyChanged(nameof(FolderCount));
                    
                    if (!IsSearchActive)
                    {
                        var folderText = FolderCount == 1 ? "folder" : "folders";
                        StatusMessage = $"Loaded {Scripts.Count} scripts in {FolderCount} {folderText}";
                    }
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
                    ApplySearch(); // Refresh filtered collections
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
                var scriptsFolder = _settingsService.ScriptsFolder;
                
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

        private async Task ChangeFolderAsync()
        {
            try
            {
                StatusMessage = "Please select a folder in the dialog that will open...";
                
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add("*");

                // Initialize with the current window handle - this is required for WinUI 3
                var window = App.MainWindow;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
                }

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    StatusMessage = "Changing scripts folder...";
                    await _settingsService.SetScriptsFolderAsync(folder.Path);
                    
                    // Force an immediate refresh after changing the folder
                    await RefreshScriptsAsync();
                }
                else
                {
                    StatusMessage = "Folder selection cancelled.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error changing scripts folder: {ex.Message}";
            }
        }

        private async void OnScriptsFolderChanged(object? sender, string newFolderPath)
        {
            // Update the UI immediately
            CurrentScriptsFolder = newFolderPath;
            
            // Clear current scripts immediately to show the change
            _dispatcherQueue.TryEnqueue(() =>
            {
                Scripts.Clear();
                GroupedScripts.Clear();
                FilteredGroupedScripts.Clear();
                OnPropertyChanged(nameof(ScriptCount));
                OnPropertyChanged(nameof(FolderCount));
                StatusMessage = "Loading scripts from new folder...";
            });
            
            // Wait a brief moment for the ScriptService to restart watching
            await Task.Delay(100);
            
            // Then refresh scripts from new folder
            await RefreshScriptsAsync();
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
                    ApplySearch(); // Refresh filtered collections
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
                    ApplySearch(); // Refresh filtered collections
                    OnPropertyChanged(nameof(ScriptCount));
                    OnPropertyChanged(nameof(FolderCount));
                    
                    var folderInfo = scriptToRemove.IsInSubfolder ? $" from {scriptToRemove.Folder}" : "";
                    StatusMessage = $"Script {scriptToRemove.Name} removed{folderInfo}";
                }
            });
        }

        private void OnFolderDeleted(object? sender, string folderPath)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // Find all scripts that were in the deleted folder
                var scriptsToRemove = Scripts.Where(s => s.FullPath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase)).ToList();
                
                if (scriptsToRemove.Any())
                {
                    foreach (var script in scriptsToRemove)
                    {
                        Scripts.Remove(script);
                    }
                    
                    UpdateGroupedScripts();
                    ApplySearch(); // Refresh filtered collections
                    OnPropertyChanged(nameof(ScriptCount));
                    OnPropertyChanged(nameof(FolderCount));
                    
                    var folderName = Path.GetFileName(folderPath);
                    StatusMessage = $"Folder '{folderName}' deleted - removed {scriptsToRemove.Count} scripts";
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