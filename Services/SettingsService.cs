using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace winui_scripts_app.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFileName = "appsettings.json";
        private readonly ApplicationDataContainer _localSettings;
        private string _scriptsFolder;

        public event EventHandler<string>? ScriptsFolderChanged;

        public string ScriptsFolder => _scriptsFolder;

        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
            
            // Initialize default scripts folder
            _scriptsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Scripts");
            
            // Load settings asynchronously on construction
            Task.Run(async () => await LoadSettingsAsync());
        }

        public async Task LoadSettingsAsync()
        {
            try
            {
                // Check if settings exist in ApplicationData
                if (_localSettings.Values.TryGetValue("ScriptsFolder", out var savedFolder))
                {
                    var folderPath = savedFolder as string;
                    if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                    {
                        _scriptsFolder = folderPath;
                    }
                }
                
                // Ensure the scripts folder exists
                if (!Directory.Exists(_scriptsFolder))
                {
                    Directory.CreateDirectory(_scriptsFolder);
                }
            }
            catch (Exception)
            {
                // If loading fails, use default folder
                _scriptsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Scripts");
                if (!Directory.Exists(_scriptsFolder))
                {
                    Directory.CreateDirectory(_scriptsFolder);
                }
            }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                _localSettings.Values["ScriptsFolder"] = _scriptsFolder;
            }
            catch (Exception)
            {
                // Silently handle save errors - settings will revert to default on next load
            }
        }

        public async Task SetScriptsFolderAsync(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Folder path cannot be empty", nameof(folderPath));

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Directory '{folderPath}' does not exist");

            var oldFolder = _scriptsFolder;
            _scriptsFolder = folderPath;
            
            await SaveSettingsAsync();
            
            // Notify listeners of the change
            ScriptsFolderChanged?.Invoke(this, _scriptsFolder);
        }
    }
}