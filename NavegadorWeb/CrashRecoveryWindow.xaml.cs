using System.Windows;

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para CrashRecoveryWindow.xaml
    /// </summary>
    public partial class CrashRecoveryWindow : Window
    {
        public bool ShouldRestoreSession { get; private set; }

        public CrashRecoveryWindow()
        {
            InitializeComponent();
            ShouldRestoreSession = false; // Por defecto no restaurar
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldRestoreSession = true;
            this.Close();
        }

        private void NewSessionButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldRestoreSession = false;
            this.Close();
        }
    }
}
