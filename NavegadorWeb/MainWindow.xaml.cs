using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Necesario para TextChangedEventArgs y SelectionChangedEventArgs
// using NavegadorWeb.Windows; // ELIMINA ESTA LÍNEA si no tienes SettingsWindow en la carpeta Windows
                               // o si SettingsWindow no está en ese namespace.

namespace NavegadorWeb
{
    // 'partial' es CLAVE. Significa que hay otra parte de esta clase (generada por WPF a partir del XAML).
    public partial class MainWindow : Window
    {
        // ******************************************************************************
        // * ¡¡¡ VERIFICA Y ELIMINA ESTO DE TU CÓDIGO ACTUAL SI LO TIENES !!!          *
        // ******************************************************************************
        // NO declares aquí campos para los elementos de UI que tienen un 'x:Name' en tu XAML.
        // WPF los genera automáticamente como miembros protegidos si la Acción de Compilación es 'Page'.
        // Por ejemplo, NO hagas esto:
        // public Border MainBorder;
        // public Grid TitleBarGrid;
        // public Button MinimizeButton;
        // public TextBox AddressBar;
        // public TabControl BrowserTabs;
        // public Image WindowIcon;
        // Simplemente usa directamente el nombre (ej. AddressBar.Text = ...).

        public MainWindow()
        {
            // ESTA ES LA ÚNICA LÍNEA InitializeComponent() que debe existir.
            // NO definas el cuerpo de este método aquí. WPF lo genera automáticamente.
            InitializeComponent();

            // ******************************************************************************
            // * ¡¡¡ VERIFICA Y ELIMINA ESTO SI EXISTE !!!                                  *
            // ******************************************************************************
            // Si tienes CUALQUIER implementación manual de los métodos Connect (IComponentConnector, IStyleConnector)
            // o un cuerpo para InitializeComponent() aquí, ELIMÍNALOS. Ejemplo de lo que NO DEBES tener:
            // public void InitializeComponent() { /* ... */ }
            // void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) { /* ... */ }
            // void System.Windows.Markup.IStyleConnector.Connect(int connectionId, object target) { /* ... */ }

            // Aquí puedes establecer el DataContext, por ejemplo:
            // this.DataContext = new MainViewModel(); // Descomenta si usas MVVM
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
            // Normalmente añadirías aquí código de interop.
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
            // Lógica para ir atrás en el navegador actual
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para ir adelante en el navegador actual
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.GoForward();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para recargar la página actual
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebView.Reload();
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Lógica para navegar a la URL ingresada en la barra de direcciones
                // (this.DataContext as MainViewModel)?.Navigate(AddressBar.Text);
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para ir a la página de inicio predeterminada
            // (this.DataContext as MainViewModel)?.Navigate("about:blank"); // Ejemplo
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir una nueva pestaña
            // (this.DataContext as MainViewModel)?.AddNewTab();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir la ventana de configuración.
            // Asegúrate de que 'SettingsWindow' exista como una clase en tu proyecto
            // y que la directiva using para su namespace sea correcta si está en otro.
            // new SettingsWindow().ShowDialog(); // Descomenta si tienes SettingsWindow
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
