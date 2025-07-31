using System.Threading.Tasks;

namespace winui_scripts_app.Services
{
    public interface ISettingsService
    {
        /// <summary>
        /// Gets the configured scripts folder path
        /// </summary>
        string ScriptsFolder { get; }

        /// <summary>
        /// Sets the scripts folder path and saves the setting
        /// </summary>
        /// <param name="folderPath">The new folder path</param>
        Task SetScriptsFolderAsync(string folderPath);

        /// <summary>
        /// Loads all settings from storage
        /// </summary>
        Task LoadSettingsAsync();

        /// <summary>
        /// Saves all settings to storage
        /// </summary>
        Task SaveSettingsAsync();

        /// <summary>
        /// Event fired when the scripts folder path changes
        /// </summary>
        event System.EventHandler<string>? ScriptsFolderChanged;
    }
}