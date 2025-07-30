using System;
using System.Collections.Generic;
using System.Linq;

namespace winui_scripts_app.Models
{
    public class ScriptGroup
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<ScriptInfo> Scripts { get; set; } = new();
        public int Count => Scripts.Count;
        public string CountDisplay => $"({Count} scripts)";
        
        public static List<ScriptGroup> GroupScripts(IEnumerable<ScriptInfo> scripts)
        {
            return scripts
                .GroupBy(s => s.DisplayFolder)
                .OrderBy(g => g.Key == "Root" ? "" : g.Key) // Root folder first
                .Select(g => new ScriptGroup
                {
                    Key = g.Key,
                    DisplayName = g.Key == "Root" ? "Root Folder" : $"{g.Key}",
                    Scripts = g.OrderBy(s => s.Name).ToList()
                })
                .ToList();
        }
    }
}