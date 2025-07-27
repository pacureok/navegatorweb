using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Asegúrate de que esta línea esté presente

namespace NavegadorWeb
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lógica tras cargar la ventana
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Guardar estado o confirmar cierre
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Ajustes tras inicializar la ventana (DPI, sombras…)
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Cambiar icono de maximizar/restaurar
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
            => WindowState = (WindowState == WindowState.Maximized)
                ? WindowState.Normal
                : WindowState.Maximized;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Close();

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Navegar a la dirección de AddressBar.Text
            }
        }

        private void FindButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void FindTextBox_KeyDown(object sender, KeyEventArgs e) { /* … */ }
        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e) { /* … */ } // Asegúrate de que el tipo sea TextChangedEventArgs
        private void FindPreviousButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void FindNextButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e) { /* … */ }

        private void GeminiButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void PipButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void ReadAloudButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void ReaderModeButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void IncognitoButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void HistoryButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void BookmarksButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void DataExtractionButton_Click(object sender, RoutedEventArgs e) { /* … */ }

        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e) { /* … */ }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e) // Asegúrate de que el tipo sea SelectionChangedEventArgs
        {
            // Actualizar URL / estado de botones
        }

        // Si tienes el manejador de eventos CloseTabButton_Click en el XAML, asegúrate de que esté aquí también.
        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cerrar la pestaña, por ejemplo:
            // Button closeButton = sender as Button;
            // if (closeButton != null && closeButton.Tag is TabItemData tabToClose)
            // {
            //     // Lógica para remover tabToClose de tu colección de pestañas
            // }
        }
    }
}
