using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace NavegadorWeb.Windows
{
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isDarkModeEnabled;

        public SettingsViewModel()
        {
            IsDarkModeEnabled = false; 
        }
    }

    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
