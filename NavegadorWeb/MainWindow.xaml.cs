using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Necesario para TextChangedEventArgs y SelectionChangedEventArgs
// using NavegadorWeb.Windows; // ELIMINA O COMENTA ESTA LÍNEA si no tienes SettingsWindow en la carpeta Windows.

namespace NavegadorWeb
{
    // 'partial' es CLAVE. Significa que hay otra parte de esta clase (generada por WPF a partir del XAML).
    public partial class MainWindow : Window
    {
        // *** IMPORTANTE: NO DECLARAR AQUÍ CAMPOS PARA LOS ELEMENTOS DE UI QUE TIENEN UN 'x:Name' EN TU XAML. ***
        // *** WPF LOS GENERA AUTOMÁTICAMENTE EN EL ARCHIVO MainWindow.g.cs. LA DUPLICIDAD CAUSA LOS ERRORES CS0102. ***
        // Por ejemplo, NO hagas esto: public TextBox AddressBar;
        // Simplemente usa directamente el nombre del control (ej. AddressBar.Text = ...).

        public MainWindow()
        {
            // ESTA ES LA ÚNICA LÍNEA InitializeComponent() que debe existir.
            // NO definas el cuerpo de este método aquí. WPF lo genera automáticamente.
            InitializeComponent();

            // Aquí puedes establecer el DataContext, por ejemplo, si usas MVVM:
            // this.DataContext = new MainViewModel();
            // Asegúrate de que tu MainViewModel inicialice TabGroupManager y le añada una pestaña inicial.
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lógica que se ejecuta una vez que la ventana y su contenido han sido cargados.
            // Por ejemplo, podrías inicializar la primera pestaña o navegar a una URL predeterminada.
            // Si usas MVVM, esto podría estar en el constructor del ViewModel o en un comando.
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Lógica para manejar el cierre de la ventana.
            // Aquí podrías guardar el estado de la sesión, confirmar con el usuario, etc.
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Este es para comportamientos personalizados de la ventana, como redimensionar la ventana sin borde.
            // Puedes añadir lógica para la interacción con las API de Windows aquí.
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Lógica para manejar los cambios de estado de la ventana (minimizada, maximizada, normal).
            // Actualiza el icono del botón maximizar/restaurar.
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeRestoreButton.Content = "❐"; // Icono de restaurar (cuadrado doble)
            }
            else
            {
                MaximizeRestoreButton.Content = "⬜"; // Icono de maximizar (cuadrado simple)
            }
        }

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Permite arrastrar la ventana desde cualquier parte del borde si es necesario.
            // Usualmente se usa más en la TitleBar.
            if (e.ClickCount == 2)
            {
                MaximizeRestoreButton_Click(sender, e);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TitleBarGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            // Lógica para ir atrás en el navegador actual
            // Si usas MVVM, cada TabItem puede tener su propio WebView2 y su lógica de navegación.
            // Ejemplo: (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para ir adelante en el navegador actual
            // Ejemplo: (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoForward();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para recargar la página actual
            // Ejemplo: (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Reload();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para navegar a la página de inicio
            // Ejemplo: (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.CoreWebView2.Navigate("about:blank");
            // o a una URL predefinida.
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Lógica para navegar a la URL ingresada en la barra de direcciones.
                string url = AddressBar.Text;
                // Aquí deberías añadir una validación de URL básica (ej. si no tiene http:// o https://, añadirlo)
                // y luego navegar el WebView2 de la pestaña actual.
                // Ejemplo: (this.BrowserTabs.SelectedItem as TabItemData)?.NavigateTo(url);
                Keyboard.ClearFocus(); // Quita el foco de la barra de direcciones después de navegar.
            }
        }

        private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
        {
            // Opcional: Seleccionar todo el texto cuando la barra de direcciones obtiene el foco.
            AddressBar.SelectAll();
        }

        private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
        {
            // Opcional: Lógica cuando la barra de direcciones pierde el foco.
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para crear una nueva pestaña.
            // Si usas MVVM, esto añadiría un nuevo objeto TabItemData/ViewModel a tu colección de pestañas.
            // Ejemplo: (this.DataContext as MainViewModel)?.TabGroupManager.SelectedTabGroup.AddNewTab();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para mostrar el historial del navegador.
            // Podría abrir una nueva ventana o una nueva pestaña con el historial.
            // Ejemplo: MessageBox.Show("Abrir historial");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de configuración.
            // Ejemplo:
            // SettingsWindow settingsWindow = new SettingsWindow();
            // settingsWindow.Owner = this; // Opcional: para que se comporte como ventana modal.
            // settingsWindow.ShowDialog();
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            // Muestra u oculta la barra de búsqueda en la página.
            FindBar.Visibility = (FindBar.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            if (FindBar.Visibility == Visibility.Visible)
            {
                FindTextBox.Focus();
                FindTextBox.SelectAll();
            }
            // Si la barra se oculta, también deberías limpiar cualquier resultado de búsqueda activo en el WebView2.
            // Ejemplo: (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.CoreWebView2.ClearFindResult();
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Inicia la búsqueda a medida que el usuario escribe.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.CoreWebView2.Find(FindTextBox.Text, false, false, false);
            // Necesitas la implementación real de la búsqueda en WebView2.
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Al presionar Enter, busca la siguiente ocurrencia.
                FindNextButton_Click(sender, e);
            }
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar la ocurrencia anterior del texto en la página.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.CoreWebView2.Find(FindTextBox.Text, true, false, false);
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar la siguiente ocurrencia del texto en la página.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.CoreWebView2.Find(FindTextBox.Text, false, true, false);
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Collapsed;
            // Limpia los resultados de la búsqueda al cerrar la barra.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.CoreWebView2.ClearFindResult();
        }

        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Lógica para manejar el cambio de selección de pestaña.
            // Esto debería actualizar la barra de direcciones y otros elementos de UI
            // para reflejar la pestaña seleccionada actualmente.
            // Puedes acceder a la pestaña seleccionada a través de BrowserTabs.SelectedItem
            // o de las propiedades e.AddedItems y e.RemovedItems.

            // Ejemplo: Si usas MVVM y tu SelectedTabItem está enlazado en el ViewModel,
            // la lógica para actualizar la AddressBar se haría en el setter de SelectedTabItem en el ViewModel,
            // o aquí si manejas la UI directamente.
            // if (BrowserTabs.SelectedItem is TabItemData selectedTab)
            // {
            //     AddressBar.Text = selectedTab.CurrentUrl;
            // }
        }
    }
}
