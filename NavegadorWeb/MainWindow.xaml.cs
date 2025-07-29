using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Necesario para TextChangedEventArgs y SelectionChangedEventArgs
// using NavegadorWeb.Windows; // Descomenta esta línea si realmente tienes una SettingsWindow en la carpeta 'Windows'

namespace NavegadorWeb
{
    // 'partial' es CLAVE. Significa que hay otra parte de esta clase (generada por WPF a partir del XAML).
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // ESTA ES LA ÚNICA LÍNEA InitializeComponent() que debe existir.
            // NO definas el cuerpo de este método aquí. WPF lo genera automáticamente.
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lógica que se ejecuta una vez que la ventana y su contenido han sido cargados.
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Lógica para manejar el cierre de la ventana.
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Este es para comportamientos personalizados de la ventana, como redimensionar la ventana sin borde.
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Lógica para manejar los cambios de estado de la ventana (minimizada, maximizada, normal).
            // Actualiza el icono del botón maximizar/restaurar.
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeRestoreButton.Content = "❐"; // Icono de restaurar
            }
            else
            {
                MaximizeRestoreButton.Content = "⬜"; // Icono de maximizar
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Permite arrastrar la ventana cuando se hace clic en la barra de título.
            if (e.ClickCount == 2) // Doble clic para maximizar/restaurar
            {
                MaximizeRestoreButton_Click(sender, e);
            }
            else // Un solo clic para arrastrar
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Atrás' presionado.");
            // Lógica para ir atrás en el navegador actual
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Adelante' presionado.");
            // Lógica para ir adelante en el navegador actual
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoForward();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Recargar' presionado.");
            // Lógica para recargar la página actual
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Reload();
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                MessageBox.Show($"Navegando a: {AddressBar.Text}");
                // Lógica para navegar a la URL ingresada en la barra de direcciones
                // (this.DataContext as MainViewModel)?.Navigate(AddressBar.Text);
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Inicio' presionado.");
            // Lógica para ir a la página de inicio predeterminada
            // (this.DataContext as MainViewModel)?.Navigate("about:blank"); // Ejemplo
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Nueva Pestaña' presionado.");
            // Lógica para abrir una nueva pestaña
            // (this.DataContext as MainViewModel)?.AddNewTab();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Configuración' presionado.");
            // Lógica para abrir la ventana de configuración.
            // new SettingsWindow().ShowDialog(); // Descomenta si tienes SettingsWindow
        }

        private void ShowFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Visible;
            MessageBox.Show("Botón 'Buscar en página' presionado.");
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Lógica para buscar texto en la página
            // if (e.Key == Key.Enter)
            // {
            //     (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Find(FindTextBox.Text);
            // }
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica para buscar texto en la página mientras el usuario escribe
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Find(FindTextBox.Text);
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar la ocurrencia anterior
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.FindPrevious(FindTextBox.Text);
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar la siguiente ocurrencia
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.FindNext(FindTextBox.Text);
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Collapsed;
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.ClearFindResult();
        }

        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Lógica para manejar el cambio de selección de pestaña.
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            Button closeButton = sender as Button;
            if (closeButton != null && closeButton.Tag is object tabToClose) 
            {
                // Aquí debes tener la lógica para remover 'tabToClose' de la colección
                // a la que está enlazado 'BrowserTabs.ItemsSource'.
            }
        }

        // MANEJADORES DE EVENTOS PARA LOS NUEVOS BOTONES CON IMAGEN
        private void AIButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Asistente IA' presionado.");
            // Lógica para el asistente de IA
        }

        private void PipButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Picture-in-Picture' presionado.");
            // Lógica para Picture-in-Picture
        }

        private void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Leer en voz alta' presionado.");
            // Lógica para lectura en voz alta
        }

        private void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Modo Lector' presionado.");
            // Lógica para modo lector
        }

        private void IncognitoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Modo Incógnito' presionado.");
            // Lógica para modo incógnito
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Historial' presionado.");
            // Lógica para historial
        }

        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Marcadores' presionado.");
            // Lógica para marcadores
        }

        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Administrador de Contraseñas' presionado.");
            // Lógica para administrador de contraseñas
        }

        private void DataExtractionButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Extracción de Datos' presionado.");
            // Lógica para extracción de datos
        }

        private void ExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Extensiones' presionado.");
            // Lógica para extensiones
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Descargas' presionado.");
            // Lógica para descargas
        }

        private void SplitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Vista Dividida' presionado.");
            // Lógica para vista dividida
        }

        private void PerformanceButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Monitor de Rendimiento' presionado.");
            // Lógica para monitor de rendimiento
        }

        private void PermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Permisos' presionado.");
            // Lógica para permisos
        }

        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Captura de Pantalla' presionado.");
            // Lógica para captura de pantalla
        }

        private void TabManagerButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Botón 'Administrador de Pestañas' presionado.");
            // Lógica para administrador de pestañas
        }
    }
}
