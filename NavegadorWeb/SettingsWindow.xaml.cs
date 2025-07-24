using System.Windows;
using System.Windows.Controls; // Necesario para ComboBox

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public string HomePage { get; set; }
        public bool IsAdBlockerEnabled { get; set; }
        public string DefaultSearchEngineUrl { get; set; }
        public bool IsTabSuspensionEnabled { get; set; } // Propiedad para habilitar/deshabilitar la suspensión de pestañas

        // Delegados para los eventos que serán manejados por MainWindow
        public delegate void ClearBrowsingDataEventHandler(); // Renombrado de ClearBrowseDataEventHandler a ClearBrowsingDataEventHandler
        public event ClearBrowsingDataEventHandler OnClearBrowsingData; // Renombrado de OnClearBrowseData a OnClearBrowsingData

        public delegate void SuspendInactiveTabsEventHandler();
        public event SuspendInactiveTabsEventHandler OnSuspendInactiveTabs;


        /// <summary>
        /// Constructor de la ventana de configuración.
        /// </summary>
        /// <param name="currentHomePage">La página de inicio actual.</param>
        /// <param name="currentAdBlockerState">El estado actual del bloqueador de anuncios.</param>
        /// <param name="currentSearchEngineUrl">La URL actual del motor de búsqueda predeterminado.</param>
        /// <param name="currentTabSuspensionState">El estado actual de la suspensión de pestañas.</param>
        public SettingsWindow(string currentHomePage, bool currentAdBlockerState, string currentSearchEngineUrl, bool currentTabSuspensionState)
        {
            InitializeComponent();

            // Cargar valores iniciales en los controles de la UI
            HomePage = currentHomePage;
            HomePageTextBox.Text = HomePage;

            IsAdBlockerEnabled = currentAdBlockerState;
            AdBlockerCheckBox.IsChecked = IsAdBlockerEnabled;

            DefaultSearchEngineUrl = currentSearchEngineUrl;
            // Seleccionar el ComboBoxItem correcto basado en la URL del motor de búsqueda
            foreach (ComboBoxItem item in SearchEngineComboBox.Items)
            {
                if (item.Tag?.ToString() == DefaultSearchEngineUrl)
                {
                    SearchEngineComboBox.SelectedItem = item;
                    break;
                }
            }
            // Si no se encuentra una coincidencia, seleccionar el primer elemento si existe
            if (SearchEngineComboBox.SelectedItem == null && SearchEngineComboBox.Items.Count > 0)
            {
                SearchEngineComboBox.SelectedIndex = 0;
            }

            IsTabSuspensionEnabled = currentTabSuspensionState;
            EnableTabSuspensionCheckBox.IsChecked = IsTabSuspensionEnabled;
        }

        /// <summary>
        /// Maneja el clic en el botón "Guardar". Guarda los valores de los controles en las propiedades públicas.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            HomePage = HomePageTextBox.Text;
            IsAdBlockerEnabled = AdBlockerCheckBox.IsChecked ?? false;
            DefaultSearchEngineUrl = (SearchEngineComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            IsTabSuspensionEnabled = EnableTabSuspensionCheckBox.IsChecked ?? false;

            this.DialogResult = true; // Indica que los cambios fueron guardados
            this.Close();
        }

        /// <summary>
        /// Maneja el clic en el botón "Cancelar". Cierra la ventana sin guardar los cambios.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indica que los cambios no fueron guardados
            this.Close();
        }

        /// <summary>
        /// Manejador para el botón "Borrar datos de navegación".
        /// Invoca el evento OnClearBrowsingData para que MainWindow lo maneje.
        /// </summary>
        private async void ClearBrowsingDataButton_Click(object sender, RoutedEventArgs e) // Renombrado de ClearBrowseDataButton_Click
        {
            OnClearBrowsingData?.Invoke(); // Invocar el evento
            MessageBox.Show("Se ha iniciado la limpieza de datos de navegación. Los cambios serán visibles después de reiniciar el navegador o recargar las páginas.", "Limpieza de Datos", MessageBoxButton.OK, MessageBoxImage.Information);
            // No cerramos la ventana de configuración, el usuario puede seguir ajustando otras opciones
        }

        /// <summary>
        /// Manejador para el botón "Suspender todas las pestañas inactivas".
        /// Invoca el evento OnSuspendInactiveTabs para que MainWindow lo maneje.
        /// </summary>
        private void SuspendAllInactiveTabsButton_Click(object sender, RoutedEventArgs e)
        {
            OnSuspendInactiveTabs?.Invoke(); // Invocar el evento
            MessageBox.Show("Se han suspendido las pestañas inactivas. Deberán recargarse al volver a ellas para ahorrar recursos.", "Pestañas Suspendidas", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
