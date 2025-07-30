using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using winui_scripts_app.Models;

namespace winui_scripts_app.Services
{
    public class ExecutionHistoryService : IExecutionHistoryService
    {
        private readonly string _historyFilePath;
        private ExecutionHistory? _cachedHistory;

        public ExecutionHistoryService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "VBScriptRunner");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            _historyFilePath = Path.Combine(appFolder, "execution_history.json");
        }

        public async Task SaveExecutionAsync(string scriptPath, DateTime executionTime)
        {
            var history = await LoadHistoryAsync();
            history.UpdateExecution(scriptPath);
            
            var json = JsonConvert.SerializeObject(history, Formatting.Indented);
            await File.WriteAllTextAsync(_historyFilePath, json);
            
            _cachedHistory = history;
        }

        public async Task<DateTime?> GetLastExecutionAsync(string scriptPath)
        {
            var history = await LoadHistoryAsync();
            return history.GetLastExecution(scriptPath);
        }

        public async Task<ExecutionHistory> LoadHistoryAsync()
        {
            if (_cachedHistory != null)
                return _cachedHistory;

            try
            {
                if (!File.Exists(_historyFilePath))
                {
                    _cachedHistory = new ExecutionHistory();
                    return _cachedHistory;
                }

                var json = await File.ReadAllTextAsync(_historyFilePath);
                _cachedHistory = JsonConvert.DeserializeObject<ExecutionHistory>(json) ?? new ExecutionHistory();
                return _cachedHistory;
            }
            catch (Exception)
            {
                _cachedHistory = new ExecutionHistory();
                return _cachedHistory;
            }
        }
    }
}