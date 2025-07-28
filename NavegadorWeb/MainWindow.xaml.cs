using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Necesario para TextChangedEventArgs y SelectionChangedEventArgs

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

            // ******************************************************************************
            // * ¡¡¡ VERIFICA Y ELIMINA ESTO DE TU CÓDIGO ACTUAL SI LO TIENES !!!          *
            // ******************************************************************************
            // Si tienes CUALQUIER declaración de campos para los elementos de UI
            // que tienen un 'x:Name' en tu MainWindow.xaml, ELIMÍNALAS.
            // Ejemplos de lo que NO DEBES tener aquí (si ya están en XAML con x:Name):
            // public Border MainBorder;
            // public Grid TitleBarGrid;
            // public Button MinimizeButton;
            // public TextBox AddressBar;
            // public TabControl BrowserTabs;
            // public Image WindowIcon;
            // etc.

            // ******************************************************************************
            // * ¡¡¡ VERIFICA Y ELIMINA ESTO DE TU CÓDIGO ACTUAL SI LO TIENES !!!          *
            // ******************************************************************************
            // NO DEBES IMPLEMENTAR MANUALMENTE estos métodos. Son generados por WPF.
            /*
            public void InitializeComponent()
            {
                // ...código generado, ¡no lo copies aquí!
            }

            // O si ves una implementación explícita de la interfaz IComponentConnector:
            void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target)
            {
                // ...código generado, ¡no lo copies aquí!
            }
            */
            // ******************************************************************************
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lógica tras cargar la ventana (ej: iniciar WebView2, cargar página de inicio)
            // Asegúrate de que tu lógica para WebView2 se inicialice aquí o en el constructor
            // y asigne el WebView2 a la primera pestaña, o cree una nueva.
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Lógica al cerrar la ventana
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Lógica después de que la ventana se ha inicializado
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Lógica cuando el estado de la ventana cambia (Minimizar/Maximizar/Normal)
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove(); // Permite mover la ventana arrastrando la barra de título personalizada
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
                // Lógica para navegar a la URL en la barra de direcciones
            }
        }

        private void FindButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void FindTextBox_KeyDown(object sender, KeyEventArgs e) { /* ... */ }
        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e) { /* ... */ }
        private void FindPreviousButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void FindNextButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e) { /* ... */ }

        private void GeminiButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void PipButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void ReadAloudButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void ReaderModeButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void IncognitoButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void HistoryButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void BookmarksButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void DataExtractionButton_Click(object sender, RoutedEventArgs e) { /* ... */ }

        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e) { /* ... */ }
        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e) { /* ... */ }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de configuración.
            // Asegúrate de que 'SettingsWindow' exista como una clase en tu proyecto.
            // new SettingsWindow().ShowDialog();
        }

        // CORRECCIÓN IMPORTANTE: El tipo de evento para SelectionChanged es SelectionChangedEventArgs
        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Lógica para manejar el cambio de selección de pestaña.
            // Esto debería actualizar la barra de direcciones y otros elementos de UI
            // para reflejar la pestaña seleccionada actualmente.
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cerrar una pestaña específica.
            // El 'Tag' del botón en XAML está configurado para pasar el objeto 'TabItemData'
            // de la pestaña que se está cerrando.
            // Button closeButton = sender as Button;
            // if (closeButton != null && closeButton.Tag is TabItemData tabToClose)
            // {
            //     // Aquí debes tener la lógica para remover 'tabToClose' de la colección
            //     // a la que está enlazado 'BrowserTabs.ItemsSource' (ej. TabGroupManager.SelectedTabGroup.TabsInGroup).
            // }
        }
    }
}
