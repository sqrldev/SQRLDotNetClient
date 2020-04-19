using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLDotNetClientUI.ViewModels
{
    /// <summary>
    /// A view model representing the "app settings" screen.
    /// </summary>
    class AppSettingsViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets or sets a value that determines if the app should start 
        /// minimized to the tray icon.
        /// </summary>
        public bool StartMinimized
        {
            get { return _appSettings.StartMinimized; }
            set { _appSettings.StartMinimized = value; }
        }

        /// <summary>
        /// Creates a new instance and performs some initialization tasks.
        /// </summary>
        public AppSettingsViewModel()
        {
            this.Title = _loc.GetLocalizationValue("AppSettingsTitle");
        }

        /// <summary>
        /// Returns back to the previous screen without changing or saving 
        /// any settings.
        /// </summary>
        public void Cancel()
        {
            Log.Information("Cancelling out of app settings screen");

            // Discard any pending changes
            _appSettings.Reload();

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }

        /// <summary>
        /// Saves the settings set by the user.
        /// </summary>
        public void Save()
        {
            Log.Information("Saving app settings");

            _appSettings.Save();

            ((MainWindowViewModel)_mainWindow.DataContext).Content =
                ((MainWindowViewModel)_mainWindow.DataContext).PriorContent;
        }
    }
}
