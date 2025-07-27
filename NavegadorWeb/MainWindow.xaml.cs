using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Necesario para TextChangedEventArgs y SelectionChangedEventArgs

namespace NavegadorWeb
{
    public partial class MainWindow : Window // 'partial' es crucial para combinar con el código generado
    {
        public MainWindow()
        {
            InitializeComponent(); // Única llamada. El método y sus funcionalidades son generados automáticamente.
                                   // NO declares aquí campos como 'public Border MainBorder;'
                                   // NO implementes aquí InitializeComponent()
                                   // NO implementes aquí IComponentConnector o IStyleConnector
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
        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e) { /* … */ }
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
            // Asegúrate de que SettingsWindow exista en tu proyecto
            new SettingsWindow().ShowDialog();
        }

        // CORRECCIÓN: Cambiado de RoutedEventArgs a SelectionChangedEventArgs
        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Actualizar URL / estado de botones
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cerrar la pestaña.
            // El Tag del botón de cerrar tab en el XAML se ha configurado para pasar el TabItemData.
            // Button closeButton = sender as Button;
            // if (closeButton != null && closeButton.Tag is TabItemData tabToClose) // Asume que tienes una clase TabItemData
            // {
            //     // Lógica para remover tabToClose de tu colección de pestañas, que está binded a BrowserTabs.ItemsSource
            // }
        }
    }
}
