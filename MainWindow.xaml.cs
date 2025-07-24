using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;
using System.Windows.Input;

namespace NavegadorWeb
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _defaultHomePage = "https://www.google.com";

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                // Asegúrate de que WebView2 esté inicializado
                await MyWebView.EnsureCoreWebView2Async(null);

                // Evento para actualizar la barra de URL cuando la página cambia
                MyWebView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
                MyWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                MyWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;


                // Cargar la página de inicio predeterminada
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
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Aquí puedes añadir lógica para bloquear ciertas URLs si lo deseas
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
            MessageBox.Show("Aquí irán las opciones de configuración del navegador.", "Opciones", MessageBoxButton.OK, MessageBoxImage.Information);
            // Más adelante, aquí podríamos abrir una nueva ventana para la configuración
            // Donde se pueda cambiar la página de inicio, el motor de búsqueda por defecto, etc.
        }
    }
}
