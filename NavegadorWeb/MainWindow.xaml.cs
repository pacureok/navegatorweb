using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Necesario para TextChangedEventArgs y SelectionChangedEventArgs
// using NavegadorWeb.Windows; // ELIMINA O COMENTA ESTA LÍNEA si no tienes SettingsWindow en la carpeta Windows.

namespace NavegadorWeb
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
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
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeRestoreButton.Content = "❐";
            }
            else
            {
                MaximizeRestoreButton.Content = "⬜";
            }
        }

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
            if (e.ClickCount == 2)
            {
                MaximizeRestoreButton_Click(sender, e);
            }
            else
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
            // Lógica para ir atrás
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para ir adelante
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para recargar
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para ir a inicio
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string url = AddressBar.Text;
                Keyboard.ClearFocus();
            }
        }

        private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
        {
            AddressBar.SelectAll();
        }

        private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
        {
            // Lógica cuando pierde el foco
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para nueva pestaña
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para historial
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para configuración
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = (FindBar.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            if (FindBar.Visibility == Visibility.Visible)
            {
                FindTextBox.Focus();
                FindTextBox.SelectAll();
            }
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica de búsqueda
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNextButton_Click(sender, e);
            }
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar anterior
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar siguiente
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Collapsed;
        }

        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Lógica de cambio de selección de pestaña.
        }
    }
}
