using System.Windows;

namespace NavegadorWeb
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public string HomePage { get; set; }
        public bool IsAdBlockerEnabled { get; set; } // Nuevo: Propiedad para el bloqueador

        public SettingsWindow(string currentHomePage, bool currentAdBlockerState) // Nuevo: Parámetro para el estado del bloqueador
        {
            InitializeComponent();
            HomePage = currentHomePage;
            HomePageTextBox.Text = HomePage;

            IsAdBlockerEnabled = currentAdBlockerState; // Cargar estado actual
            AdBlockerCheckBox.IsChecked = IsAdBlockerEnabled; // Asignar al CheckBox
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            HomePage = HomePageTextBox.Text;
            IsAdBlockerEnabled = AdBlockerCheckBox.IsChecked ?? false; // Guardar estado del CheckBox
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
