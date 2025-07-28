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
        // NO declares aquí campos para los elementos de UI que tienen un 'x:Name' en tu XAML.
        // WPF los genera automáticamente como miembros protegidos si la Acción de Compilación es 'Page'.
        // Por ejemplo, NO hagas esto: public TextBox AddressBar;
        // Simplemente usa directamente el nombre (ej. AddressBar.Text = ...).

        public MainWindow()
        {
            // ESTA ES LA ÚNICA LÍNEA InitializeComponent() que debe existir.
            // NO definas el cuerpo de este método aquí. WPF lo genera automáticamente.
            InitializeComponent();

            // Aquí puedes establecer el DataContext, por ejemplo:
            // this.DataContext = new MainViewModel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lógica que se ejecuta una vez que la ventana y su contenido han sido cargados.
            // Por ejemplo, podrías inicializar la primera pestaña o navegar a una URL predeterminada.
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Lógica para manejar el cierre de la ventana.
            // Aquí podrías guardar el estado de la sesión, confirmar con el usuario, etc.
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Ajustes que se realizan después de que la ventana ha sido inicializada,
            // útil para operaciones relacionadas con la ventana nativa (manejo de esquinas, sombras, etc.).
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Lógica para responder a cambios en el estado de la ventana (Minimizado, Maximizado, Normal).
            // Por ejemplo, para cambiar el icono del botón de maximizar/restaurar.
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Permite arrastrar la ventana desde la barra de título personalizada.
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
            => Close(); // Cierra la ventana.

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            // Lógica para manejar la pulsación de teclas en la barra de direcciones.
            // Por ejemplo, para navegar cuando se presiona Enter.
            if (e.Key == Key.Enter)
            {
                // Aquí deberías tener la lógica para navegar a la URL en AddressBar.Text
                // Ejemplo: SelectedTabItem.Url = AddressBar.Text;
            }
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para mostrar u ocultar la barra de búsqueda en la página.
            // Por ejemplo, podrías enlazar la visibilidad de FindBar a una propiedad en tu ViewModel.
            // FindBar.Visibility = FindBar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e) { /* Lógica para buscar al presionar Enter */ }
        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e) { /* Lógica para buscar al escribir */ }
        private void FindPreviousButton_Click(object sender, RoutedEventArgs e) { /* Lógica para buscar anterior */ }
        private void FindNextButton_Click(object sender, RoutedEventArgs e) { /* Lógica para buscar siguiente */ }
        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e) { /* Lógica para cerrar la barra de búsqueda */ }

        private void GeminiButton_Click(object sender, RoutedEventArgs e) { /* Lógica para interactuar con Gemini */ }
        private void PipButton_Click(object sender, RoutedEventArgs e) { /* Lógica para Picture-in-Picture */ }
        private void ReadAloudButton_Click(object sender, RoutedEventArgs e) { /* Lógica para lectura en voz alta */ }
        private void ReaderModeButton_Click(object sender, RoutedEventArgs e) { /* Lógica para modo lectura */ }
        private void IncognitoButton_Click(object sender, RoutedEventArgs e) { /* Lógica para modo incógnito */ }
        private void HistoryButton_Click(object sender, RoutedEventArgs e) { /* Lógica para historial */ }
        private void BookmarksButton_Click(object sender, RoutedEventArgs e) { /* Lógica para marcadores */ }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e) { /* Lógica para gestor de contraseñas */ }
        private void DataExtractionButton_Click(object sender, RoutedEventArgs e) { /* Lógica para extracción de datos */ }

        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e) { /* Lógica para habilitar/deshabilitar extensión */ }
        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e) { /* Lógica para abrir gestor de extensiones */ }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de configuración.
            // Asegúrate de que 'SettingsWindow' exista como una clase en tu proyecto.
            new Windows.SettingsWindow().ShowDialog(); // Asumiendo que SettingsWindow está en la carpeta Windows
        }

        // CORRECCIÓN IMPORTANTE: El tipo de evento para SelectionChanged es SelectionChangedEventArgs
        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Lógica para manejar el cambio de selección de pestaña.
            // Esto debería actualizar la barra de direcciones y otros elementos de UI
            // para reflejar la pestaña seleccionada actualmente.
            // Puedes acceder a la pestaña seleccionada a través de BrowserTabs.SelectedItem
            // o de las propiedades e.AddedItems y e.RemovedItems.
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cerrar una pestaña específica.
            // El 'Tag' del botón en XAML está configurado para pasar el objeto 'TabItemData'
            // de la pestaña que se está cerrando.
            Button closeButton = sender as Button;
            if (closeButton != null && closeButton.Tag is object tabToClose) // Cambia 'object' por tu tipo de datos de pestaña, por ejemplo 'TabItemData'
            {
                // Aquí debes tener la lógica para remover 'tabToClose' de la colección
                // a la que está enlazado 'BrowserTabs.ItemsSource' (ej. TabGroupManager.SelectedTabGroup.TabsInGroup).
                // Por ejemplo: (this.DataContext as MainViewModel)?.TabGroupManager.SelectedTabGroup.TabsInGroup.Remove(tabToClose);
            }
        }
    }
}
