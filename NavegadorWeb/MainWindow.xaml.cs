using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;
using System.Windows.Input;
using System.Configuration; // Nuevo: Para manejar configuraciones

namespace NavegadorWeb
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _defaultHomePage = "https://www.google.com"; // Página de inicio predeterminada
        private const string HomePageSettingKey = "DefaultHomePage"; // Clave para la configuración de la página de inicio

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings(); // Cargar configuraciones al iniciar la aplicación
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                // Asegúrate de que WebView2 esté inicializado
                await MyWebView.EnsureCoreWebView2Async(null);

                // Eventos para actualizar la interfaz de usuario
                MyWebView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
                MyWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                MyWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                MyWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged; // Nuevo: Para el título de la página

                // Cargar la página de inicio predeterminada (o la guardada)
                MyWebView.CoreWebView2.Navigate(_defaultHomePage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar WebView2: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            // Actualiza la barra de dirección cuando la URL cambia
            UrlTextBox.Text = MyWebView.CoreWebView2.Source;
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                MessageBox.Show($"La navegación a {MyWebView.CoreWebView2.Source} falló con el código de error {e.WebErrorStatus}", "Error de Navegación", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Puedes añadir lógica para ocultar una barra de progreso o similar aquí
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Aquí puedes añadir lógica para bloquear ciertas URLs o cambiar el User-Agent
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            // Actualiza el título de la ventana con el título de la página
            this.Title = MyWebView.CoreWebView2.DocumentTitle + " - Mi Navegador Web";
        }


        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl();
        }

        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToUrl();
            }
        }

        private void NavigateToUrl()
        {
            string url = UrlTextBox.Text;
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                // Intentar prefijar con http:// si no es una URL válida
                url = "http://" + url;
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    MessageBox.Show("Por favor, introduce una URL válida.", "URL Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                MyWebView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al navegar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyWebView.CoreWebView2.CanGoBack)
            {
                MyWebView.CoreWebView2.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyWebView.CoreWebView2.CanGoForward)
            {
                MyWebView.CoreWebView2.GoForward();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            MyWebView.CoreWebView2.Navigate(_defaultHomePage);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Abre la ventana de configuración y le pasa la página de inicio actual
            SettingsWindow settingsWindow = new SettingsWindow(_defaultHomePage);
            if (settingsWindow.ShowDialog() == true) // ShowDialog() espera a que la ventana se cierre
            {
                // Si el usuario hizo clic en "Guardar" y el resultado es true
                _defaultHomePage = settingsWindow.HomePage; // Actualiza la página de inicio en MainWindow
                SaveSettings(); // Guarda la nueva página de inicio en la configuración
                MessageBox.Show("Configuración guardada. La nueva página de inicio se aplicará al reiniciar el navegador o al hacer clic en 'Inicio'.", "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Métodos para cargar y guardar la configuración
        private void LoadSettings()
        {
            // Carga la página de inicio guardada en las configuraciones de la aplicación (App.config)
            string savedHomePage = ConfigurationManager.AppSettings[HomePageSettingKey];
            if (!string.IsNullOrEmpty(savedHomePage))
            {
                _defaultHomePage = savedHomePage;
            }
            // Si no hay una configuración guardada, se usará la _defaultHomePage inicial ("https://www.google.com")
        }

        private void SaveSettings()
        {
            // Guarda la página de inicio en las configuraciones de la aplicación (App.config)
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[HomePageSettingKey] == null)
            {
                // Si la clave no existe, la añade
                config.AppSettings.Settings.Add(HomePageSettingKey, _defaultHomePage);
            }
            else
            {
                // Si la clave existe, actualiza su valor
                config.AppSettings.Settings[HomePageSettingKey].Value = _defaultHomePage;
            }
            config.Save(ConfigurationSaveMode.Modified); // Guarda los cambios en el archivo de configuración
            ConfigurationManager.RefreshSection("appSettings"); // Refresca la sección para cargar los nuevos valores inmediatamente
        }
    }
}
