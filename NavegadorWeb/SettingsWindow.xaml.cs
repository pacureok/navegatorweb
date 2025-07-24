using System.Windows;
using System.Windows.Controls;

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
        public bool IsTabSuspensionEnabled { get; set; } // Nuevo: Propiedad para habilitar suspensión

        // Delegados para los eventos que serán manejados por MainWindow
        public delegate void ClearBrowseDataEventHandler();
        public event ClearBrowseDataEventHandler OnClearBrowseData;

        public delegate void SuspendInactiveTabsEventHandler();
        public event SuspendInactiveTabsEventHandler OnSuspendInactiveTabs;


        public SettingsWindow(string currentHomePage, bool currentAdBlockerState, string currentSearchEngineUrl, bool currentTabSuspensionState) // Nuevo: Parámetro para suspensión
        {
            InitializeComponent();
            HomePage = currentHomePage;
            HomePageTextBox.Text = HomePage;

            IsAdBlockerEnabled = currentAdBlockerState;
            AdBlockerCheckBox.IsChecked = IsAdBlockerEnabled;

            DefaultSearchEngineUrl = currentSearchEngineUrl;
            foreach (ComboBoxItem item in SearchEngineComboBox.Items)
            {
                if (item.Tag?.ToString() == DefaultSearchEngineUrl)
                {
                    SearchEngineComboBox.SelectedItem = item;
                    break;
                }
            }
            if (SearchEngineComboBox.SelectedItem == null && SearchEngineComboBox.Items.Count > 0)
            {
                SearchEngineComboBox.SelectedIndex = 0;
            }

            IsTabSuspensionEnabled = currentTabSuspensionState; // Cargar estado de suspensión
            EnableTabSuspensionCheckBox.IsChecked = IsTabSuspensionEnabled;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            HomePage = HomePageTextBox.Text;
            IsAdBlockerEnabled = AdBlockerCheckBox.IsChecked ?? false;
            DefaultSearchEngineUrl = (SearchEngineComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            IsTabSuspensionEnabled = EnableTabSuspensionCheckBox.IsChecked ?? false; // Guardar estado de suspensión

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Nuevo: Manejador para el botón "Borrar datos de navegación"
        private async void ClearBrowseDataButton_Click(object sender, RoutedEventArgs e)
        {
            // Invocar el evento para que MainWindow lo maneje
            OnClearBrowseData?.Invoke();
            MessageBox.Show("Se ha iniciado la limpieza de datos de navegación. Los cambios serán visibles después de reiniciar el navegador o recargar las páginas.", "Limpieza de Datos", MessageBoxButton.OK, MessageBoxImage.Information);
            // No cerramos la ventana de configuración, el usuario puede seguir ajustando otras opciones
        }

        // Nuevo: Manejador para el botón "Suspender todas las pestañas inactivas"
        private void SuspendAllInactiveTabsButton_Click(object sender, RoutedEventArgs e)
        {
            // Invocar el evento para que MainWindow lo maneje
            OnSuspendInactiveTabs?.Invoke();
            MessageBox.Show("Se han suspendido las pestañas inactivas. Deberán recargarse al volver a ellas para ahorrar recursos.", "Pestañas Suspendidas", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
