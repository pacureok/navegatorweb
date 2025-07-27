using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Importante para TextChangedEventArgs y SelectionChangedEventArgs

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
            // Por ejemplo: WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Lógica para guardar estado o confirmar cierre
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Ajustes tras inicializar la ventana (DPI, sombras, etc.)
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Cambiar icono de maximizar/restaurar si es necesario
            // if (WindowState == WindowState.Maximized)
            // {
            //     MaximizeRestoreButton.Content = "🗗"; // Icono de restaurar
            // }
            // else
            // {
            //     MaximizeRestoreButton.Content = "🗖"; // Icono de maximizar
            // }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove(); // Permite arrastrar la ventana desde cualquier parte
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
                // Ejemplo de navegación, asumiendo que tienes un WebView2 llamado 'browserView'
                // var uri = AddressBar.Text;
                // if (!uri.StartsWith("http://") && !uri.StartsWith("https://"))
                // {
                //     uri = "https://" + uri; // O buscar en un motor de búsqueda por defecto
                // }
                // browserView.Source = new Uri(uri);
            }
        }

        private void FindButton_Click(object sender, RoutedEventArgs e) { /* Lógica para mostrar la barra de búsqueda */ }
        private void FindTextBox_KeyDown(object sender, KeyEventArgs e) { /* Lógica para búsqueda al presionar Enter */ }
        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e) { /* Lógica para búsqueda en tiempo real */ }
        private void FindPreviousButton_Click(object sender, RoutedEventArgs e) { /* Lógica para buscar anterior */ }
        private void FindNextButton_Click(object sender, RoutedEventArgs e) { /* Lógica para buscar siguiente */ }
        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e) { /* Lógica para cerrar la barra de búsqueda */ }

        private void GeminiButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void PipButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void ReadAloudButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void ReaderModeButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void IncognitoButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void HistoryButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void BookmarksButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void DataExtractionButton_Click(object sender, RoutedEventArgs e) { /* ... */ }

        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para manejar el clic en un elemento de extensión
            // MenuItem menuItem = sender as MenuItem;
            // if (menuItem != null && menuItem.Tag is ExtensionData extension)
            // {
            //     extension.IsEnabled = menuItem.IsChecked;
            // }
        }

        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de gestión de extensiones
            // new SettingsWindow().ShowDialog(); // O una ventana específica de extensiones
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que SettingsWindow exista en tu proyecto
            // new SettingsWindow().ShowDialog();
        }

        // CORRECCIÓN: Cambiado de RoutedEventArgs a SelectionChangedEventArgs
        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Actualizar URL / estado de botones basado en la pestaña seleccionada
            // e.AddedItems y e.RemovedItems contendrán los TabItem seleccionados/deseleccionados
            // Por ejemplo:
            // if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTab)
            // {
            //     // Suponiendo que cada TabItem tiene un WebView2 en su contenido
            //     if (selectedTab.Content is wv2.WebView2 webView)
            //     {
            //         AddressBar.Text = webView.Source?.ToString() ?? "";
            //     }
            // }
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
