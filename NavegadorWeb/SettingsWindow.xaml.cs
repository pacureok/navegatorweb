// This file contains the code-behind for the SettingsWindow.xaml.
// It manages the browser's settings, such as home page, ad blocker, and tab suspension.

using System.Windows;
using NavegadorWeb.Classes; // Required to access the ToolbarPosition enum (if used for settings)
using System.ComponentModel; // Required for INotifyPropertyChanged
using System.Configuration; // Required for ConfigurationManager

namespace NavegadorWeb
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        // Private fields for settings properties
        private string _homePage;
        private bool _isAdBlockerEnabled;
        private bool _isTabSuspensionEnabled;
        private string _defaultSearchEngine;
        private bool _restoreSessionOnStartup;
        private ToolbarPosition _toolbarPosition; // Example of a setting using ToolbarPosition

        // Public properties with change notification (INotifyPropertyChanged)
        public string HomePage
        {
            get => _homePage;
            set
            {
                if (_homePage != value)
                {
                    _homePage = value;
                    OnPropertyChanged(nameof(HomePage));
                }
            }
        }

        public bool IsAdBlockerEnabled
        {
            get => _isAdBlockerEnabled;
            set
            {
                if (_isAdBlockerEnabled != value)
                {
                    _isAdBlockerEnabled = value;
                    OnPropertyChanged(nameof(IsAdBlockerEnabled));
                }
            }
        }

        public bool IsTabSuspensionEnabled
        {
            get => _isTabSuspensionEnabled;
            set
            {
                if (_isTabSuspensionEnabled != value)
                {
                    _isTabSuspensionEnabled = value;
                    OnPropertyChanged(nameof(IsTabSuspensionEnabled));
                }
            }
        }

        public string DefaultSearchEngine
        {
            get => _defaultSearchEngine;
            set
            {
                if (_defaultSearchEngine != value)
                {
                    _defaultSearchEngine = value;
                    OnPropertyChanged(nameof(DefaultSearchEngine));
                }
            }
        }

        public bool RestoreSessionOnStartup
        {
            get => _restoreSessionOnStartup;
            set
            {
                if (_restoreSessionOnStartup != value)
                {
                    _restoreSessionOnStartup = value;
                    OnPropertyChanged(nameof(RestoreSessionOnStartup));
                }
            }
        }

        public ToolbarPosition ToolbarPosition
        {
            get => _toolbarPosition;
            set
            {
                if (_toolbarPosition != value)
                {
                    _toolbarPosition = value;
                    OnPropertyChanged(nameof(ToolbarPosition));
                }
            }
        }

        // Event for property change notification
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Initializes a new instance of the SettingsWindow class.
        /// Loads current settings from the application configuration.
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent(); // Initializes the UI components defined in SettingsWindow.xaml
            this.DataContext = this; // Sets the DataContext for data binding

            // Load settings from App.config or default values
            HomePage = ConfigurationManager.AppSettings["DefaultHomePage"] ?? "https://www.google.com";
            IsAdBlockerEnabled = bool.Parse(ConfigurationManager.AppSettings["AdBlockerEnabled"] ?? "false");
            IsTabSuspensionEnabled = bool.Parse(ConfigurationManager.AppSettings["TabSuspensionEnabled"] ?? "false");
            DefaultSearchEngine = ConfigurationManager.AppSettings["DefaultSearchEngine"] ?? "https://www.google.com/search?q=";
            RestoreSessionOnStartup = bool.Parse(ConfigurationManager.AppSettings["RestoreSessionOnStartup"] ?? "false");

            // Parse ToolbarPosition from string (handle potential errors)
            if (Enum.TryParse(ConfigurationManager.AppSettings["ToolbarPosition"], out ToolbarPosition position))
            {
                ToolbarPosition = position;
            }
            else
            {
                ToolbarPosition = Classes.ToolbarPosition.Top; // Default if parsing fails
            }
        }

        /// <summary>
        /// Event handler for the "Save" button click.
        /// Saves the updated settings to the application configuration.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save settings to App.config
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["DefaultHomePage"].Value = HomePage;
            config.AppSettings.Settings["AdBlockerEnabled"].Value = IsAdBlockerEnabled.ToString();
            config.AppSettings.Settings["TabSuspensionEnabled"].Value = IsTabSuspensionEnabled.ToString();
            config.AppSettings.Settings["DefaultSearchEngine"].Value = DefaultSearchEngine;
            config.AppSettings.Settings["RestoreSessionOnStartup"].Value = RestoreSessionOnStartup.ToString();
            config.AppSettings.Settings["ToolbarPosition"].Value = ToolbarPosition.ToString();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings"); // Refresh the section to load new values

            this.DialogResult = true; // Indicates that settings were saved successfully
            this.Close(); // Closes the Settings window
        }

        /// <summary>
        /// Event handler for the "Cancel" button click.
        /// Closes the window without saving changes.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indicates that the operation was canceled
            this.Close(); // Closes the Settings window
        }
    }
}
