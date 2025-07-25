using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Para Color

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        // Propiedades para pasar datos de configuración
        public string HomePage { get; set; }
        public bool IsAdBlockerEnabled { get; set; }
        public string DefaultSearchEngineUrl { get; set; }
        public bool IsTabSuspensionEnabled { get; set; }
        public bool RestoreSessionOnStartup { get; set; }
        public bool IsTrackerProtectionEnabled { get; set; }
        public bool IsPdfViewerEnabled { get; set; }

        // Propiedades para los colores del tema
        public Color BrowserBackgroundColor { get; set; }
        public Color BrowserForegroundColor { get; set; }

        // NUEVO: Propiedad para la posición de la barra de herramientas
        public ToolbarPosition SelectedToolbarPosition { get; set; }


        // Eventos para notificar a MainWindow sobre acciones
        public event Action OnClearBrowseData;
        public event Action OnSuspendInactiveTabs;
        public event Action<Color, Color> OnColorsChanged;
        public event Action<ToolbarPosition> OnToolbarPositionChanged; // NUEVO: Evento para notificar cambio de posición


        public SettingsWindow(string homePage, bool isAdBlockerEnabled, string searchEngineUrl,
                              bool isTabSuspensionEnabled, bool restoreSessionOnStartup,
                              bool isTrackerProtectionEnabled, bool isPdfViewerEnabled,
                              Color backgroundColor, Color foregroundColor,
                              ToolbarPosition toolbarPosition) // NUEVO: Parámetro de orientación
        {
            InitializeComponent();

            // Inicializar los campos de la UI con los valores actuales
            HomePageTextBox.Text = homePage;
            AdBlockerCheckBox.IsChecked = isAdBlockerEnabled;
            SearchEngineTextBox.Text = searchEngineUrl;
            TabSuspensionCheckBox.IsChecked = isTabSuspensionEnabled;
            RestoreSessionCheckBox.IsChecked = restoreSessionOnStartup;
            TrackerProtectionCheckBox.IsChecked = isTrackerProtectionEnabled;
            PdfViewerCheckBox.IsChecked = isPdfViewerEnabled;

            // Inicializar los campos de color
            BackgroundColorTextBox.Text = backgroundColor.ToString();
            ForegroundColorTextBox.Text = foregroundColor.ToString();

            // NUEVO: Seleccionar el ítem correcto en el ComboBox
            ToolbarPositionComboBox.SelectedValue = toolbarPosition;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Asignar los valores de la UI a las propiedades públicas
            HomePage = HomePageTextBox.Text;
            IsAdBlockerEnabled = AdBlockerCheckBox.IsChecked ?? false;
            DefaultSearchEngineUrl = SearchEngineTextBox.Text;
            IsTabSuspensionEnabled = TabSuspensionCheckBox.IsChecked ?? false;
            RestoreSessionOnStartup = RestoreSessionCheckBox.IsChecked ?? false;
            IsTrackerProtectionEnabled = TrackerProtectionCheckBox.IsChecked ?? false;
            IsPdfViewerEnabled = PdfViewerCheckBox.IsChecked ?? false;

            // Intentar parsear y asignar los colores
            try
            {
                Color newBgColor = (Color)ColorConverter.ConvertFromString(BackgroundColorTextBox.Text);
                Color newFgColor = (Color)ColorConverter.ConvertFromString(ForegroundColorTextBox.Text);
                BrowserBackgroundColor = newBgColor;
                BrowserForegroundColor = newFgColor;
                OnColorsChanged?.Invoke(BrowserBackgroundColor, BrowserForegroundColor); // Invocar el evento
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar los colores. Asegúrate de usar un formato válido (ej. #RRGGBB): {ex.Message}", "Error de Color", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // NUEVO: Asignar y notificar el cambio de orientación
            if (ToolbarPositionComboBox.SelectedValue is ToolbarPosition newToolbarPosition)
            {
                SelectedToolbarPosition = newToolbarPosition;
                OnToolbarPositionChanged?.Invoke(SelectedToolbarPosition);
            }


            this.DialogResult = true; // Indica que se hizo clic en Guardar
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indica que se hizo clic en Cancelar
            this.Close();
        }

        private void ClearBrowseDataButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Esto borrará la caché, cookies, historial, contraseñas guardadas y otros datos de navegación.\n\n¿Estás seguro de que deseas continuar?",
                "Borrar Datos de Navegación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                OnClearBrowseData?.Invoke(); // Invocar el evento para que MainWindow lo maneje
            }
        }

        private void SuspendTabsButton_Click(object sender, RoutedEventArgs e)
        {
            OnSuspendInactiveTabs?.Invoke(); // Invocar el evento para que MainWindow lo maneje
        }
    }
}
