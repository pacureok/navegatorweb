using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Necesario para TextChangedEventArgs y SelectionChangedEventArgs
using Microsoft.Web.WebView2.Core; // Asegúrate de que esta esté
using Microsoft.Web.WebView2.Wpf; // Asegúrate de que esta esté
using System.IO;
using System.Text.Json;
using System.Speech.Synthesis;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Timers;

using NavegadorWeb.Classes;
using NavegadorWeb.Extensions;
using NavegadorWeb.Services;
using NavegadorWeb; // <--- ¡AÑADIDO/VERIFICADO PARA HistoryWindow!

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
            // Lógica para guardar el estado de la sesión antes de cerrar la aplicación.
            // Esto es crucial para restaurar pestañas y grupos en el próximo inicio.
            if (this.DataContext is MainViewModel viewModel)
            {
                viewModel.SaveSessionCommand.Execute(null);
            }
        }

        // Importaciones necesarias para manipular la ventana sin el estilo de borde por defecto.
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        private const int WM_SYSCOMMAND = 0x112;
        private const uint SC_RESTORE = 0xF120; // Restaurar
        private const uint SC_MOVE = 0xF010;    // Mover
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Deshabilita el elemento de menú "Mover" para evitar que el usuario mueva la ventana arrastrando la barra de título,
            // ya que implementamos un movimiento personalizado.
            IntPtr handle = new WindowInteropHelper(this).Handle;
            IntPtr sysMenu = GetSystemMenu(handle, false);
            if (sysMenu != IntPtr.Zero)
            {
                EnableMenuItem(sysMenu, SC_MOVE, MF_BYCOMMAND | MF_GRAYED);
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Actualiza la visibilidad de los botones de maximizar/restaurar
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeButton.Visibility = Visibility.Collapsed;
                RestoreButton.Visibility = Visibility.Visible;
            }
            else
            {
                MaximizeButton.Visibility = Visibility.Visible;
                RestoreButton.Visibility = Visibility.Collapsed;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Permite arrastrar la ventana desde la barra de título, excepto si está maximizada.
            if (e.ClickCount == 2) // Doble clic para maximizar/restaurar
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
            else if (e.LeftButton == MouseButtonState.Pressed && this.WindowState == WindowState.Normal)
            {
                this.DragMove(); // Mueve la ventana si no está maximizada
            }
        }

        private void OpenHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear una instancia de HistoryWindow
            HistoryWindow historyWindow = new HistoryWindow();
            // Mostrar la ventana de historial como un diálogo
            bool? result = historyWindow.ShowDialog();

            // Si el usuario seleccionó una URL y presionó Aceptar
            if (result == true && !string.IsNullOrEmpty(historyWindow.SelectedUrl))
            {
                // Navega a la URL seleccionada en la pestaña actual (o en una nueva si prefieres)
                if (this.DataContext is MainViewModel viewModel && viewModel.SelectedTabItem != null)
                {
                    viewModel.NavigateCommand.Execute(historyWindow.SelectedUrl);
                }
            }
        }

        private void ShowFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Visible;
            FindTextBox.Focus();
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.DataContext is MainViewModel viewModel && viewModel.SelectedTabItem != null)
            {
                // Nota: La API de búsqueda de WebView2 ha cambiado.
                // CoreWebView2.Find ahora requiere un objeto CoreWebView2FindParameters.
                // Esta línea comentada es un ejemplo de cómo se podría haber usado una API anterior
                // o si se manejara de otra forma la búsqueda.
                // Para una implementación correcta, consulta la documentación de WebView2 para CoreWebView2.Find.
                // Aquí, el texto del cuadro de búsqueda simplemente activa la visibilidad.
                // viewModel.SelectedTabItem.WebViewInstance.CoreWebView2.Find(FindTextBox.Text, false, true, false);
            }
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar la ocurrencia anterior.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebViewInstance.CoreWebView2.Find(FindTextBox.Text, false, false, false); // COMENTADO
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar la siguiente ocurrencia del texto en la página.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebViewInstance.CoreWebView2.Find(FindTextBox.Text, false, true, false); // COMENTADO
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Collapsed;
            // Limpia los resultados de la búsqueda al cerrar la barra.
            // (this.BrowserTabs.SelectedItem as TabItemData)?.WebViewInstance.CoreWebView2.ClearFindResult(); // COMENTADO
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

        // Métodos auxiliares para la captura de pantalla (si son necesarios aquí, sino se pueden mover al ViewModel)
        private BitmapImage? ConvertBase64ToBitmapImage(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return null;

            try
            {
                byte[] binaryData = Convert.FromBase64String(base64String);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(binaryData);
                bi.EndInit();
                return bi;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al convertir Base64 a BitmapImage: {ex.Message}");
                return null;
            }
        }

        private string ConvertBitmapImageToBase64(BitmapImage bitmapImage)
        {
            if (bitmapImage == null) return string.Empty;

            try
            {
                MemoryStream stream = new MemoryStream();
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(stream);
                byte[] bytes = stream.ToArray();
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al convertir BitmapImage a Base64: {ex.Message}");
                return string.Empty;
            }
        }

        // Guarda una cadena de imagen Base64 como un archivo PNG
        private void SaveBase64ImageAsPng(string base64String, string filePath)
        {
            if (string.IsNullOrEmpty(base64String)) return;

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String.Replace("data:image/png;base64,", ""));
                File.WriteAllBytes(filePath, imageBytes);
                MessageBox.Show($"Captura de pantalla guardada en: {filePath}", "Captura Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la captura de pantalla: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
