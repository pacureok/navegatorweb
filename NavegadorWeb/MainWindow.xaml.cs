using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace NavegadorWeb
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Ventana cargada
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Aquí tu lógica de arranque
        }

        // Antes de cerrar la ventana
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Guardar estado o confirmar cierre
        }

        // Inicialización tras el handle de la ventana
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Por ejemplo, ajustar sombras o compatibilidad DPI
        }

        // Detecta cambios de minimizado/maximizado
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Actualizar icono de maximizar/restaurar
        }

        // Permitir arrastrar la ventana al hacer clic en la barra de título
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // Botones de control de la ventana
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
            => WindowState = (WindowState == WindowState.Maximized)
                ? WindowState.Normal
                : WindowState.Maximized;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Close();

        // Barra de direcciones
        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Navegar a la URL de AddressBar.Text
            }
        }

        // Find in page
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar FindBar
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Buscar texto
            }
        }

        private void FindTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Actualizar conteo de resultados
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Buscar hacia arriba
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Buscar hacia abajo
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            // Ocultar FindBar
        }

        // Otras funcionalidades (Gemini, PIP, lector, incognito…)
        private void GeminiButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void PipButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void ReadAloudButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void ReaderModeButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void IncognitoButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void HistoryButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void BookmarksButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e) { /* … */ }
        private void DataExtractionButton_Click(object sender, RoutedEventArgs e) { /* … */ }

        // Extensiones
        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Habilitar/deshabilitar extensión
        }

        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar ventana de gestión de extensiones
        }

        // Ajustes
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        // Cambio de pestaña
        private void BrowserTabControl_SelectionChanged_Grouped(object sender, RoutedEventArgs e)
        {
            // Actualizar barra de direcciones y botones
        }
    }
}
