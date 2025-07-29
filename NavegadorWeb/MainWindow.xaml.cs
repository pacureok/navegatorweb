using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Necesario para TextChangedEventArgs y SelectionChangedEventArgs
using NavegadorWeb.Windows; // Asegúrate de que este using esté si tienes SettingsWindow en la carpeta Windows

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
            // Este es para comportamientos personalizados de la ventana, como redimensionar la ventana sin borde.
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Lógica para manejar los cambios de estado de la ventana (minimizada, maximizada, restaurada).
        }

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Lógica para arrastrar la ventana sin borde.
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void TitleBarGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Lógica para arrastrar la ventana desde la barra de título.
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
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
            // Lógica para ir hacia atrás en el historial de navegación de la pestaña actual.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para ir hacia adelante en el historial de navegación de la pestaña actual.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoForward();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para recargar la página actual en la pestaña actual.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Reload();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para ir a la página de inicio predeterminada en la pestaña actual.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.CoreWebView2.Navigate("about:blank"); // O una URL real
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            // Lógica para navegar cuando se presiona Enter en la barra de direcciones.
            if (e.Key == Key.Enter)
            {
                // string url = AddressBar.Text;
                // if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                // {
                //     url = "http://" + url; // Añadir http:// si no está presente
                // }
                // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.CoreWebView2.Navigate(url);
            }
        }

        private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
        {
            AddressBar.SelectAll();
        }

        private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
        {
            // Puedes añadir lógica aquí si necesitas hacer algo cuando la barra pierde el foco.
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para crear una nueva pestaña.
            // (this.DataContext as MainViewModel)?.TabGroupManager.SelectedTabGroup.AddNewTab();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir una ventana o panel de historial.
            // Por ejemplo: new HistoryWindow().ShowDialog();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de configuración.
            // Asegúrate de que 'SettingsWindow' exista como una clase en tu proyecto.
            // new SettingsWindow().ShowDialog();
        }

        // Métodos para la barra de búsqueda (Find Bar)
        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica para iniciar la búsqueda a medida que se escribe.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Find(FindTextBox.Text);
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.FindNext(FindTextBox.Text);
            }
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
