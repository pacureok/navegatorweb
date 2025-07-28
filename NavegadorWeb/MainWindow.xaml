using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Necesario para TextChangedEventArgs y SelectionChangedEventArgs
using NavegadorWeb.Windows; // Asegúrate de que esta ruta sea correcta si SettingsWindow está en una subcarpeta 'Windows'
using Microsoft.Web.WebView2.Wpf; // Asegúrate de tener este using para WebView2 si lo necesitas directamente

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

            // Aquí puedes establecer el DataContext para tu ViewModel si usas MVVM.
            // Por ejemplo: this.DataContext = new MainViewModel();

            // Lógica de inicialización adicional después de InitializeComponent()
            // Puedes acceder a los controles con x:Name directamente aquí, por ejemplo:
            // AddressBar.Text = "https://www.google.com";
        }

        // --- MANEJADORES DE EVENTOS DE LA VENTANA ---
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lógica que se ejecuta una vez que la ventana y su contenido han sido cargados.
            // Por ejemplo, cargar una página de inicio en la primera pestaña.
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

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Lógica para responder a cambios en el estado de la ventana (Minimizado, Maximizado, Normal).
            // Por ejemplo, para cambiar el icono del botón de maximizar/restaurar.
            // if (WindowState == WindowState.Maximized) { MaximizeRestoreButton.Content = "❐"; }
            // else { MaximizeRestoreButton.Content = "⬜"; }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Permite arrastrar la ventana desde la barra de título personalizada.
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // --- MANEJADORES DE EVENTOS DE LA BARRA DE TÍTULO / CONTROLES DE LA VENTANA ---
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
            => WindowState = (WindowState == WindowState.Maximized)
                ? WindowState.Normal
                : WindowState.Maximized;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Close(); // Cierra la ventana.

        // --- MANEJADORES DE EVENTOS DE LA BARRA DE DIRECCIONES Y NAVEGACIÓN ---
        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Lógica para navegar a la URL en AddressBar.Text
                // Ejemplo: if (BrowserTabs.SelectedItem is BrowserTabItem selectedTab) { selectedTab.Url = AddressBar.Text; }
            }
        }

        // --- MANEJADORES DE EVENTOS DE LA BARRA DE BÚSQUEDA (FindBar) ---
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle la visibilidad de la barra de búsqueda
            FindBar.Visibility = (FindBar.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            if (FindBar.Visibility == Visibility.Visible)
            {
                FindTextBox.Focus();
                FindTextBox.SelectAll();
            }
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Lógica para iniciar la búsqueda en la página
                // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.FindNext(FindTextBox.Text, false);
            }
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica para actualizar la búsqueda en tiempo real o preparar la siguiente búsqueda
            // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.Find(FindTextBox.Text);
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para buscar la ocurrencia anterior
            // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.FindNext(FindTextBox.Text, true);
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para buscar la ocurrencia siguiente
            // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.FindNext(FindTextBox.Text, false);
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Collapsed;
            FindTextBox.Clear();
            // También detener la búsqueda activa en WebView2
            // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.StopFind(WebView2.Core.WebView2.Core.CoreWebView2StopFindAction.ClearSelection);
        }

        // --- MANEJADORES DE EVENTOS DE CARACTERÍSTICAS ADICIONALES ---
        private void GeminiButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de interacción con Gemini
            // new AskGeminiWindow().ShowDialog();
        }

        private void PipButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para Picture-in-Picture
            // new PipWindow().Show();
        }

        private void ReadAloudButton_Click(object sender, RoutedEventArgs e) { /* Lógica para lectura en voz alta */ }
        private void ReaderModeButton_Click(object sender, RoutedEventArgs e) { /* Lógica para modo lectura */ }
        private void IncognitoButton_Click(object sender, RoutedEventArgs e) { /* Lógica para modo incógnito */ }
        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de historial
            // new HistoryWindow().ShowDialog();
        }
        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de marcadores
            // new BookmarksWindow().ShowDialog();
        }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para gestor de contraseñas
            // new PasswordManagerWindow().ShowDialog();
        }
        private void DataExtractionButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para extracción de datos
            // new DataExtractionWindow().ShowDialog();
        }

        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e) { /* Lógica para habilitar/deshabilitar extensión */ }
        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir gestor de extensiones
            // new ExtensionsWindow().ShowDialog();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de configuración.
            // Asegúrate de que 'SettingsWindow' exista como una clase en tu proyecto.
            // new SettingsWindow().ShowDialog();
        }

        // --- MANEJADORES DE EVENTOS DE PESTAÑAS ---
        // CORRECCIÓN IMPORTANTE: El tipo de evento para SelectionChanged es SelectionChangedEventArgs
        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Lógica para manejar el cambio de selección de pestaña.
            // Esto debería actualizar la barra de direcciones y otros elementos de UI
            // para reflejar la pestaña seleccionada actualmente.
            // Puedes acceder a la pestaña seleccionada a través de BrowserTabs.SelectedItem
            // o de las propiedades e.AddedItems y e.RemovedItems.
            // Ejemplo: if (BrowserTabs.SelectedItem is BrowserTabItem selectedTab) { AddressBar.Text = selectedTab.Url; }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cerrar una pestaña específica.
            // El 'Tag' del botón en XAML está configurado para pasar el objeto de la pestaña que se está cerrando.
            Button closeButton = sender as Button;
            if (closeButton != null && closeButton.Tag != null) // Usar 'object' genérico o el tipo de tu pestaña (ej. 'BrowserTabItem')
            {
                // Ejemplo:
                // if (closeButton.Tag is BrowserTabItem tabToClose)
                // {
                //     // Aquí debes tener la lógica para remover 'tabToClose' de la colección
                //     // a la que está enlazado 'BrowserTabs.ItemsSource' (ej. TabGroupManager.SelectedTabGroup.TabsInGroup).
                //     // (this.DataContext as MainViewModel)?.TabGroupManager.SelectedTabGroup.TabsInGroup.Remove(tabToClose);
                // }
            }
        }
    }
}
