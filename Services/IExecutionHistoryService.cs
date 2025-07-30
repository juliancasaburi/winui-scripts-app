using System;
using System.Threading.Tasks;
using winui_scripts_app.Models;

namespace winui_scripts_app.Services
{
    public interface IExecutionHistoryService
    {
        Task SaveExecutionAsync(string scriptPath, DateTime executionTime);
        Task<DateTime?> GetLastExecutionAsync(string scriptPath);
        Task<ExecutionHistory> LoadHistoryAsync();
    }
}