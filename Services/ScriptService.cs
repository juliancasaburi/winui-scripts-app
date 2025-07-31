using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using winui_scripts_app.Models;

namespace winui_scripts_app.Services
{
    public class ScriptService 
    {
        private readonly ISettingsService _settingsService;
        private readonly IExecutionHistoryService _historyService;
        private FileSystemWatcher? _watcher;

        public event EventHandler<ScriptInfo>? ScriptAdded;
        public event EventHandler<string>? ScriptRemoved;
        public event EventHandler<string>? FolderDeleted; // New event for folder deletion

        private static readonly string[] SupportedExtensions = new[] { ".vbs", ".bat", ".cmd", ".ps1" };

        public ScriptService(IExecutionHistoryService historyService, ISettingsService settingsService)
        {
            _historyService = historyService;
            _settingsService = settingsService;
            
            // Subscribe to settings changes
            _settingsService.ScriptsFolderChanged += OnScriptsFolderChanged;
            
            // Ensure the scripts folder exists
            EnsureScriptsFolderExists();
        }

        private void EnsureScriptsFolderExists()
        {
            if (!Directory.Exists(_settingsService.ScriptsFolder))
            {
                Directory.CreateDirectory(_settingsService.ScriptsFolder);
            }
        }

        private void OnScriptsFolderChanged(object? sender, string newFolderPath)
        {
            // Stop watching the old folder
            StopWatching();
            
            // Ensure new folder exists
            EnsureScriptsFolderExists();
            
            // Start watching the new folder
            StartWatching();
        }

        public async Task<List<ScriptInfo>> GetScriptsAsync()
        {
            var scripts = new List<ScriptInfo>();
            var scriptsFolder = _settingsService.ScriptsFolder;
            
            if (!Directory.Exists(scriptsFolder))
                return scripts;

            var files = Directory.GetFiles(scriptsFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var lastExecution = await _historyService.GetLastExecutionAsync(file);
                var relativeFolder = Path.GetDirectoryName(Path.GetRelativePath(scriptsFolder, file)) ?? string.Empty;
                scripts.Add(new ScriptInfo
                {
                    Name = Path.GetFileName(file),
                    FullPath = file,
                    CreatedDate = fileInfo.CreationTime,
                    ModifiedDate = fileInfo.LastWriteTime,
                    FileSize = fileInfo.Length,
                    LastExecuted = lastExecution,
                    IsExecuting = false,
                    Folder = relativeFolder
                });
            }
            return scripts.OrderBy(s => s.Name).ToList();
        }

        public async Task<bool> ExecuteScriptAsync(string scriptPath)
        {
            try
            {
                if (!File.Exists(scriptPath))
                    return false;
                var ext = Path.GetExtension(scriptPath).ToLowerInvariant();
                ProcessStartInfo startInfo;
                switch (ext)
                {
                    case ".vbs":
                        startInfo = new ProcessStartInfo
                        {
                            FileName = "cscript.exe",
                            Arguments = $"\"{scriptPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? _settingsService.ScriptsFolder
                        };
                        break;
                    case ".bat":
                    case ".cmd":
                        startInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c \"{scriptPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? _settingsService.ScriptsFolder
                        };
                        break;
                    case ".ps1":
                        startInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? _settingsService.ScriptsFolder
                        };
                        break;
                    default:
                        return false;
                }
                using var process = Process.Start(startInfo);
                if (process == null)
                    return false;
                await process.WaitForExitAsync();
                await _historyService.SaveExecutionAsync(scriptPath, DateTime.Now);
                return process.ExitCode == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteScriptAsync(string scriptPath)
        {
            try
            {
                if (File.Exists(scriptPath))
                {
                    File.Delete(scriptPath);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void StartWatching()
        {
            if (_watcher != null)
                return;
                
            var scriptsFolder = _settingsService.ScriptsFolder;
            if (!Directory.Exists(scriptsFolder))
                return;
                
            _watcher = new FileSystemWatcher(scriptsFolder)
            {
                Filter = "*.*",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            _watcher.Created += OnScriptFileCreated;
            _watcher.Deleted += OnScriptFileDeleted;
            _watcher.Renamed += OnScriptFileRenamed;
            _watcher.EnableRaisingEvents = true;
        }

        public void StopWatching()
        {
            _watcher?.Dispose();
            _watcher = null;
        }

        private async void OnScriptFileCreated(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(100);
            if (!SupportedExtensions.Contains(Path.GetExtension(e.FullPath), StringComparer.OrdinalIgnoreCase))
                return;
            var fileInfo = new FileInfo(e.FullPath);
            var scriptsFolder = _settingsService.ScriptsFolder;
            var relativeFolder = Path.GetDirectoryName(Path.GetRelativePath(scriptsFolder, e.FullPath)) ?? string.Empty;
            var scriptInfo = new ScriptInfo
            {
                Name = Path.GetFileName(e.FullPath),
                FullPath = e.FullPath,
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length,
                LastExecuted = null,
                IsExecuting = false,
                Folder = relativeFolder
            };
            ScriptAdded?.Invoke(this, scriptInfo);
        }

        private void OnScriptFileDeleted(object sender, FileSystemEventArgs e)
        {
            // Check if this is a directory deletion
            if (Directory.Exists(e.FullPath) == false && File.Exists(e.FullPath) == false)
            {
                // This could be either a file or directory that was deleted
                // We need to check if it's a directory path by seeing if any scripts were in this path
                var relativePath = Path.GetRelativePath(_settingsService.ScriptsFolder, e.FullPath);
                
                // If the path doesn't have an extension, it's likely a directory
                if (string.IsNullOrEmpty(Path.GetExtension(e.FullPath)))
                {
                    // This is a directory deletion - notify about the folder
                    FolderDeleted?.Invoke(this, e.FullPath);
                    return;
                }
            }

            // Handle file deletion as before
            if (!SupportedExtensions.Contains(Path.GetExtension(e.FullPath), StringComparer.OrdinalIgnoreCase))
                return;
            ScriptRemoved?.Invoke(this, e.FullPath);
        }

        private async void OnScriptFileRenamed(object sender, RenamedEventArgs e)
        {
            if (!SupportedExtensions.Contains(Path.GetExtension(e.FullPath), StringComparer.OrdinalIgnoreCase))
                return;
            ScriptRemoved?.Invoke(this, e.OldFullPath);
            await Task.Delay(100);
            var fileInfo = new FileInfo(e.FullPath);
            var scriptsFolder = _settingsService.ScriptsFolder;
            var relativeFolder = Path.GetDirectoryName(Path.GetRelativePath(scriptsFolder, e.FullPath)) ?? string.Empty;
            var scriptInfo = new ScriptInfo
            {
                Name = Path.GetFileName(e.FullPath),
                FullPath = e.FullPath,
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length,
                LastExecuted = await _historyService.GetLastExecutionAsync(e.FullPath),
                IsExecuting = false,
                Folder = relativeFolder
            };
            ScriptAdded?.Invoke(this, scriptInfo);
        }
    }
}