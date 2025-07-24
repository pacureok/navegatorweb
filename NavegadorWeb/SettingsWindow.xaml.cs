using System.Windows;

namespace NavegadorWeb
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public string HomePage { get; set; }

        public SettingsWindow(string currentHomePage)
        {
            InitializeComponent();
            HomePage = currentHomePage;
            HomePageTextBox.Text = HomePage;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            HomePage = HomePageTextBox.Text;
            this.DialogResult = true; // Indica que los cambios fueron guardados
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indica que los cambios no fueron guardados
            this.Close();
        }
    }
}
