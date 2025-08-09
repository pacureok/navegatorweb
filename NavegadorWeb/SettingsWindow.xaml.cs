using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace NavegadorWeb.Windows
{
    // Un ViewModel simple para la ventana de configuración
    public partial class SettingsViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isDarkModeEnabled;

        public SettingsViewModel()
        {
            // Inicializar las propiedades, por ejemplo, cargando desde un archivo de configuración
            IsDarkModeEnabled = false; 
        }
    }

    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            // Asigna el ViewModel a la ventana para el enlace de datos
            DataContext = new SettingsViewModel();
        }
    }
}
