using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using winui_scripts_app.Models;

namespace winui_scripts_app.Services
{
    public interface IScriptService
    {
        Task<List<ScriptInfo>> GetScriptsAsync();
        Task<bool> ExecuteScriptAsync(string scriptPath);
        Task<bool> DeleteScriptAsync(string scriptPath);
        event EventHandler<ScriptInfo>? ScriptAdded;
        event EventHandler<string>? ScriptRemoved;
        void StartWatching();
        void StopWatching();
    }
}