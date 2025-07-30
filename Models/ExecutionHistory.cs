using System;
using System.Collections.Generic;

namespace winui_scripts_app.Models
{
    public class ExecutionHistory
    {
        public Dictionary<string, DateTime> ScriptExecutions { get; set; } = new();
        
        public void UpdateExecution(string scriptPath)
        {
            ScriptExecutions[scriptPath] = DateTime.Now;
        }
        
        public DateTime? GetLastExecution(string scriptPath)
        {
            return ScriptExecutions.TryGetValue(scriptPath, out var lastExec) ? lastExec : null;
        }
    }
}