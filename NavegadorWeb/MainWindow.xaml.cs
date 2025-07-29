using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Text.Json;
using System.Speech.Synthesis; // Necesario para SpeechSynthesizer
using System.Windows.Media.Imaging; // Necesario para BitmapImage
using System.Windows.Media; // Necesario para VisualTreeHelper, Border
using System.Diagnostics; // Necesario para Process.Start (si se usa para abrir ventanas externas)
using System.Collections.ObjectModel; // Necesario para ObservableCollection
using System.Threading.Tasks; // Necesario para Task
using System.ComponentModel; // Necesario para INotifyPropertyChanged
using System.Windows.Interop; // Necesario para HwndSource
using System.Runtime.InteropServices; // Necesario para DllImport
using System.Net.NetworkInformation; // Puede ser necesario para verificar la conexión a internet
using System.Timers; // Necesario para System.Timers.Timer para la suspensión de pestañas

// ELIMINA O COMENTA ESTA LÍNEA si no tienes la carpeta 'Windows' con clases dentro de NavegadorWeb.
// using NavegadorWeb.Windows; 

// Asegúrate de que estas directivas 'using' estén presentes para las clases auxiliares y de servicios
using NavegadorWeb.Classes; // Para TabItemData, TabGroup, TabGroupManager, CapturedPageData, RelayCommand, TabGroupState, ToolbarPosition, PasswordManager
using NavegadorWeb.Extensions; // Para CustomExtension, ExtensionManager
using NavegadorWeb.Services; // Para LanguageService, HistoryManager, SettingsManager, DownloadManager, PasswordManager

namespace NavegadorWeb
{
    // 'partial' es CLAVE. Significa que hay otra parte de esta clase (generada por WPF a partir del XAML).
    // NO declares INotifyPropertyChanged aquí si tu DataContext es un ViewModel.
    public partial class MainWindow : Window
    {
        // Propiedad para el ViewModel de la ventana principal
        public MainViewModel ViewModel { get; set; }

        public MainWindow()
        {
            // ESTA ES LA ÚNICA LÍNEA InitializeComponent() que debe existir.
            // NO definas el cuerpo de este método aquí. WPF lo genera automáticamente.
            InitializeComponent();

            // Aquí puedes establecer el DataContext, por ejemplo, si usas MVVM:
            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;

            // Asegúrate de que tu MainViewModel inicialice TabGroupManager y le añada una pestaña inicial.
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lógica que se ejecuta una vez que la ventana y su contenido han sido cargados.
            // Por ejemplo, podrías inicializar la primera pestaña o navegar a una URL predeterminada.
            // Si usas MVVM, esto podría estar en el constructor del ViewModel o en un comando.
            ViewModel.InitializeBrowser(); // Llama a la inicialización del ViewModel
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Lógica para manejar el cierre de la ventana.
            ViewModel.SaveSession(); // Guarda el estado de la sesión antes de cerrar
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Este es para comportamientos personalizados de la ventana, como redimensionar la ventana sin borde.
            // Puedes añadir lógica para la ventana sin borde aquí si es necesario.
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // Lógica para manejar los cambios de estado de la ventana (minimizada, maximizada, restaurada).
            if (this.WindowState == WindowState.Maximized)
            {
                // Ajustar el borde para evitar que la ventana maximizada se salga de la pantalla en Windows 10/11
                this.BorderThickness = new Thickness(0);
            }
            else
            {
                this.BorderThickness = new Thickness(1); // Restaurar borde normal
            }
        }

        // Métodos para los botones de control de la ventana
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

        // Permite mover la ventana arrastrando la barra de título
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // Doble clic para maximizar/restaurar
            {
                MaximizeRestoreButton_Click(sender, e);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.NavigateCommand.Execute(AddressBar.Text);
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow historyWindow = new HistoryWindow();
            if (historyWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(historyWindow.SelectedUrl))
                {
                    ViewModel.NavigateCommand.Execute(historyWindow.SelectedUrl);
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Aquí deberías crear y mostrar tu ventana de configuración.
            // Asegúrate de que SettingsWindow exista y esté en el espacio de nombres correcto.
            // Ejemplo:
            // SettingsWindow settingsWindow = new SettingsWindow();
            // settingsWindow.ShowDialog();
            MessageBox.Show("Funcionalidad de Configuración pendiente de implementar.", "Configuración", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = (FindBar.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            if (FindBar.Visibility == Visibility.Visible)
            {
                FindTextBox.Focus();
                FindTextBox.SelectAll();
            }
            else
            {
                // Limpia los resultados de la búsqueda al cerrar la barra.
                (this.BrowserTabs.SelectedItem as TabItemData)?.WebViewInstance.CoreWebView2.ClearFindResult();
            }
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindTextInWebView();
            }
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FindTextInWebView();
        }

        private void FindTextInWebView()
        {
            if (string.IsNullOrEmpty(FindTextBox.Text))
            {
                (this.BrowserTabs.SelectedItem as TabItemData)?.WebViewInstance.CoreWebView2.ClearFindResult();
                return;
            }

            // Lógica para encontrar el texto en la página.
            // Los parámetros son: searchText, matchCase, searchPrevious, searchNext.
            // Usamos searchNext = true para que busque desde el principio o la posición actual.
            (this.BrowserTabs.SelectedItem as TabItemData)?.WebViewInstance.CoreWebView2.Find(FindTextBox.Text, false, false, true);
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar la ocurrencia anterior del texto en la página.
            // searchPrevious = true, searchNext = false
            (this.BrowserTabs.SelectedItem as TabItemData)?.WebViewInstance.CoreWebView2.Find(FindTextBox.Text, false, true, false);
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para encontrar la siguiente ocurrencia del texto en la página.
            // searchPrevious = false, searchNext = true
            (this.BrowserTabs.SelectedItem as TabItemData)?.WebViewInstance.CoreWebView2.Find(FindTextBox.Text, false, false, true);
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Collapsed;
            // Limpia los resultados de la búsqueda al cerrar la barra.
            (this.BrowserTabs.SelectedItem as TabItemData)?.WebViewInstance.CoreWebView2.ClearFindResult();
        }

        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Lógica para manejar el cambio de selección de pestaña.
            // Esto debería actualizar la barra de direcciones y otros elementos de UI
            // para reflejar la pestaña seleccionada actualmente.
            // Si usas MVVM, la propiedad SelectedTabItem en tu ViewModel se actualizará automáticamente
            // debido al TwoWay binding en el XAML, y la lógica de UI se gestionaría allí.
            // Sin embargo, si necesitas una acción directa aquí:
            if (ViewModel.SelectedTabItem != null)
            {
                AddressBar.Text = ViewModel.SelectedTabItem.Url;
            }
        }

        // Métodos de utilidad para manejar imágenes, si los necesitas directamente en la ventana
        // (Aunque para un diseño MVVM, estas funciones podrían estar mejor en un ViewModel o servicio)

        // Convierte un BitmapImage a una cadena Base64
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
