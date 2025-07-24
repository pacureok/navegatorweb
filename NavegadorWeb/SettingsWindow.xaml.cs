using System;
using System.Windows;
using System.Windows.Controls;

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        // Propiedades para almacenar las configuraciones
        public string HomePage { get; private set; }
        public bool IsAdBlockerEnabled { get; private set; }
        public string DefaultSearchEngineUrl { get; private set; }
        public bool IsTabSuspensionEnabled { get; private set; }
        public bool RestoreSessionOnStartup { get; private set; }
        public bool IsTrackerProtectionEnabled { get; private set; } // NUEVO: Propiedad para protección contra rastreadores

        // Eventos para notificar a MainWindow sobre acciones específicas
        public event Action OnClearBrowsingData;
        public event Action OnSuspendInactiveTabs;

        public SettingsWindow(string currentHomePage, bool isAdBlockerEnabled, string defaultSearchEngineUrl, bool isTabSuspensionEnabled, bool restoreSessionOnStartup, bool isTrackerProtectionEnabled) // NUEVO: Parámetro isTrackerProtectionEnabled
        {
            InitializeComponent();

            // Cargar las configuraciones actuales en los controles de la ventana
            HomePageTextBox.Text = currentHomePage;
            AdBlockerCheckBox.IsChecked = isAdBlockerEnabled;
            SearchEngineTextBox.Text = defaultSearchEngineUrl;
            TabSuspensionCheckBox.IsChecked = isTabSuspensionEnabled;
            RestoreSessionCheckBox.IsChecked = restoreSessionOnStartup;
            TrackerProtectionCheckBox.IsChecked = isTrackerProtectionEnabled; // NUEVO: Asignar estado al checkbox
        }

        /// <summary>
        /// Maneja el clic en el botón "Guardar".
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar la URL de la página de inicio
            if (!Uri.TryCreate(HomePageTextBox.Text, UriKind.Absolute, out Uri homePageUri) ||
                (homePageUri.Scheme != Uri.UriSchemeHttp && homePageUri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show("Por favor, introduce una URL de página de inicio válida (debe empezar con http:// o https://).", "Error de URL", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validar la URL del motor de búsqueda
            if (!Uri.TryCreate(SearchEngineTextBox.Text, UriKind.Absolute, out Uri searchEngineUri) ||
                (searchEngineUri.Scheme != Uri.UriSchemeHttp && searchEngineUri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show("Por favor, introduce una URL de motor de búsqueda válida (debe empezar con http:// o https://).", "Error de URL", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Guardar las configuraciones de los controles en las propiedades públicas
            HomePage = HomePageTextBox.Text;
            IsAdBlockerEnabled = AdBlockerCheckBox.IsChecked ?? false; // ?? false para manejar nullables
            DefaultSearchEngineUrl = SearchEngineTextBox.Text;
            IsTabSuspensionEnabled = TabSuspensionCheckBox.IsChecked ?? false;
            RestoreSessionOnStartup = RestoreSessionCheckBox.IsChecked ?? false;
            IsTrackerProtectionEnabled = TrackerProtectionCheckBox.IsChecked ?? false; // NUEVO: Guardar estado

            DialogResult = true; // Indicar que la ventana se cerró con éxito (Guardar)
            this.Close();
        }

        /// <summary>
        /// Maneja el clic en el botón "Cancelar".
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Indicar que la ventana se cerró sin guardar cambios
            this.Close();
        }

        /// <summary>
        /// Maneja el clic en el botón "Borrar Datos de Navegación".
        /// </summary>
        private void ClearBrowsingDataButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres borrar todos los datos de navegación (caché, cookies, historial, etc.)?", "Confirmar Borrado", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                OnClearBrowsingData?.Invoke(); // Disparar el evento para que MainWindow lo maneje
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Suspender Pestañas Inactivas Ahora".
        /// </summary>
        private void SuspendInactiveTabsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres suspender todas las pestañas inactivas ahora para liberar recursos?", "Confirmar Suspensión", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                OnSuspendInactiveTabs?.Invoke(); // Disparar el evento para que MainWindow lo maneje
            }
        }
    }
}

