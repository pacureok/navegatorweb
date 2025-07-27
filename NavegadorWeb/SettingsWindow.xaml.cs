// Este archivo contiene el código subyacente para SettingsWindow.xaml.
// Gestiona la configuración del navegador, como la página de inicio, el bloqueador de anuncios y la suspensión de pestañas.

using System; // Necesario para Enum
using System.Windows;
using NavegadorWeb.Classes; // Necesario para acceder al enum ToolbarPosition
using System.ComponentModel; // Necesario para INotifyPropertyChanged
using System.Configuration; // Necesario para ConfigurationManager

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        // Campos privados para las propiedades de configuración
        private string _homePage;
        private bool _isAdBlockerEnabled;
        private bool _isTabSuspensionEnabled;
        private string _defaultSearchEngine;
        private bool _restoreSessionOnStartup;
        private ToolbarPosition _toolbarPosition; // Ejemplo de una configuración que usa ToolbarPosition

        // Propiedades públicas con notificación de cambio (INotifyPropertyChanged)
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

        // Evento para la notificación de cambio de propiedad
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Inicializa una nueva instancia de la clase SettingsWindow.
        /// Carga la configuración actual desde la configuración de la aplicación.
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent(); // Inicializa los componentes de la UI definidos en SettingsWindow.xaml
            this.DataContext = this; // Establece el DataContext para el enlace de datos

            // Carga la configuración de App.config o valores predeterminados
            HomePage = ConfigurationManager.AppSettings["DefaultHomePage"] ?? "https://www.google.com";
            IsAdBlockerEnabled = bool.Parse(ConfigurationManager.AppSettings["AdBlockerEnabled"] ?? "false");
            IsTabSuspensionEnabled = bool.Parse(ConfigurationManager.AppSettings["TabSuspensionEnabled"] ?? "false");
            DefaultSearchEngine = ConfigurationManager.AppSettings["DefaultSearchEngine"] ?? "https://www.google.com/search?q=";
            RestoreSessionOnStartup = bool.Parse(ConfigurationManager.AppSettings["RestoreSessionOnStartup"] ?? "false");

            // Analiza ToolbarPosition desde la cadena (maneja posibles errores)
            if (Enum.TryParse(ConfigurationManager.AppSettings["ToolbarPosition"], out ToolbarPosition position))
            {
                ToolbarPosition = position;
            }
            else
            {
                ToolbarPosition = Classes.ToolbarPosition.Top; // Valor predeterminado si el análisis falla
            }
        }

        /// <summary>
        /// Controlador de eventos para el clic del botón "Guardar".
        /// Guarda la configuración actualizada en la configuración de la aplicación.
        /// </summary>
        /// <param name="sender">El objeto que generó el evento.</param>
        /// <param name="e">Los datos del evento.</param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Guarda la configuración en App.config
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["DefaultHomePage"].Value = HomePage;
            config.AppSettings.Settings["AdBlockerEnabled"].Value = IsAdBlockerEnabled.ToString();
            config.AppSettings.Settings["TabSuspensionEnabled"].Value = IsTabSuspensionEnabled.ToString();
            config.AppSettings.Settings["DefaultSearchEngine"].Value = DefaultSearchEngine;
            config.AppSettings.Settings["RestoreSessionOnStartup"].Value = RestoreSessionOnStartup.ToString();
            config.AppSettings.Settings["ToolbarPosition"].Value = ToolbarPosition.ToString();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings"); // Actualiza la sección para cargar nuevos valores

            this.DialogResult = true; // Indica que la configuración se guardó correctamente
            this.Close(); // Cierra la ventana de Configuración
        }

        /// <summary>
        /// Controlador de eventos para el clic del botón "Cancelar".
        /// Cierra la ventana sin guardar los cambios.
        /// </summary>
        /// <param name="sender">El objeto que generó el evento.</param>
        /// <param name="e">Los datos del evento.</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indica que la operación fue cancelada
            this.Close(); // Cierra la ventana de Configuración
        }
    }
}
