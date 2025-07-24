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
        public string DefaultSearchEngineUrl { get; set; } // Nuevo: Propiedad para la URL del motor de búsqueda

        public SettingsWindow(string currentHomePage, bool currentAdBlockerState, string currentSearchEngineUrl) // Nuevo: Parámetro para la URL del motor de búsqueda
        {
            InitializeComponent();
            HomePage = currentHomePage;
            HomePageTextBox.Text = HomePage;

            IsAdBlockerEnabled = currentAdBlockerState;
            AdBlockerCheckBox.IsChecked = IsAdBlockerEnabled;

            DefaultSearchEngineUrl = currentSearchEngineUrl; // Cargar URL actual
            // Seleccionar el ComboBoxItem correcto
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
                SearchEngineComboBox.SelectedIndex = 0; // Seleccionar el primero si no hay coincidencia
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            HomePage = HomePageTextBox.Text;
            IsAdBlockerEnabled = AdBlockerCheckBox.IsChecked ?? false;
            DefaultSearchEngineUrl = (SearchEngineComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(); // Obtener la URL del motor de búsqueda seleccionado
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
