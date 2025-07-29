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
            // Permite arrastrar la ventana desde el borde.
            DragMove();
        }

        private void TitleBarGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Permite arrastrar la ventana desde la barra de título.
            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Aquí deberías tener la lógica para navegar a la URL ingresada en AddressBar.Text.
                // Por ejemplo: (this.DataContext as MainViewModel)?.Navigate(AddressBar.Text);
            }
        }

        private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
        {
            // Selecciona todo el texto en la barra de direcciones cuando se enfoca.
            AddressBar.SelectAll();
        }

        private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
        {
            // Deselecciona el texto cuando pierde el foco.
            AddressBar.Select(0, 0);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para navegar hacia atrás en el historial del WebView2 actual.
            // Por ejemplo: (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para navegar hacia adelante en el historial del WebView2 actual.
            // Por ejemplo: (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoForward();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para recargar la página actual en el WebView2 actual.
            // Por ejemplo: (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Reload();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para navegar a la página de inicio.
            // Por ejemplo: (this.DataContext as MainViewModel)?.Navigate("about:blank");
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para crear una nueva pestaña.
            // Por ejemplo: (this.DataContext as MainViewModel)?.TabGroupManager.SelectedTabGroup.AddNewTab();
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica para buscar texto en la página actual.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Find(FindTextBox.Text);
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Lógica para iniciar la búsqueda cuando se presiona Enter.
                // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Find(FindTextBox.Text);
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

        // Método para el botón de historial (asegúrate de que este método exista si tienes el botón en XAML)
        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana o panel de historial
            MessageBox.Show("Abrir historial de navegación.");
            // new HistoryWindow().ShowDialog(); // Si tienes una ventana de historial
        }

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
