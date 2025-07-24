using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic; // Nuevo
using System.Configuration;
using System.Linq; // Nuevo
using System.Windows;
using System.Windows.Controls; // Nuevo
using System.Windows.Input;

namespace NavegadorWeb
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _defaultHomePage = "https://www.google.com";
        private const string HomePageSettingKey = "DefaultHomePage";

        // Nueva lista para mantener un seguimiento de todas las pestañas
        private List<BrowserTabItem> _browserTabs = new List<BrowserTabItem>();

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings(); // Cargar configuraciones al iniciar
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Crear la primera pestaña al cargar la ventana
            AddNewTab(_defaultHomePage);
        }

        private void AddNewTab(string url = null)
        {
            // Crear un nuevo TabItem
            TabItem newTabItem = new TabItem();
            newTabItem.Header = "Nueva Pestaña"; // Título inicial
            newTabItem.Name = "Tab" + (_browserTabs.Count + 1); // Nombre único para la pestaña

            // Crear un StackPanel para contener el WebView2 y el botón de cerrar
            DockPanel tabHeaderPanel = new DockPanel();
            TextBlock headerText = new TextBlock { Text = "Cargando..." };
            Button closeButton = new Button { Content = "✖", Width = 20, Height = 20, Margin = new Thickness(5, 0, 0, 0) };
            closeButton.Click += CloseTabButton_Click;
            closeButton.Tag = newTabItem; // Asocia el botón a la pestaña
            DockPanel.SetDock(headerText, Dock.Left);
            DockPanel.SetDock(closeButton, Dock.Right);
            tabHeaderPanel.Children.Add(headerText);
            tabHeaderPanel.Children.Add(closeButton);
            newTabItem.Header = tabHeaderPanel;


            // Crear una nueva instancia de WebView2
            WebView2 webView = new WebView2();
            webView.Source = new Uri(url ?? _defaultHomePage); // Cargar URL o página de inicio
            webView.Name = "WebView" + (_browserTabs.Count + 1); // Nombre único para el WebView
            webView.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView.VerticalAlignment = VerticalAlignment.Stretch;

            // Enlazar eventos del WebView2
            webView.Loaded += WebView_Loaded; // Para inicializar CoreWebView2
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            webView.NavigationStarting += WebView_NavigationStarting;
            webView.SourceChanged += WebView_SourceChanged;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.CoreWebView2.DocumentTitleChanged += WebView_DocumentTitleChanged;


            // Contenido de la pestaña
            Grid tabContent = new Grid();
            tabContent.Children.Add(webView);
            newTabItem.Content = tabContent;

            // Añadir la pestaña al TabControl
            BrowserTabControl.Items.Add(newTabItem);
            BrowserTabControl.SelectedItem = newTabItem; // Seleccionar la nueva pestaña

            // Crear un objeto BrowserTabItem para rastrear la pestaña
            BrowserTabItem browserTab = new BrowserTabItem
            {
                Tab = newTabItem,
                WebView = webView,
                HeaderTextBlock = headerText
            };
            _browserTabs.Add(browserTab);

            // Actualizar el contexto del URL TextBox
            UpdateUrlTextBoxFromCurrentTab();
        }

        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            // Este evento es crucial para asegurar que CoreWebView2 esté inicializado.
            // Es la forma recomendada para manejar la inicialización del CoreWebView2.
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null)
            {
                await currentWebView.EnsureCoreWebView2Async(null);
                // Si la URL se establece antes de CoreWebView2 estar listo,
                // se puede navegar aquí de nuevo.
                // currentWebView.CoreWebView2.Navigate(currentWebView.Source.OriginalString);
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null && e.IsSuccess)
            {
                // Solo añadir los manejadores si no están ya añadidos
                // (pueden ser añadidos una vez por cada CoreWebView2)
                // Se han movido al AddNewTab para ser más directo.
                // Asegúrate de que los manejadores de eventos están adjuntos aquí si WebView2 se reutiliza.
            }
        }


        // Métodos de evento para cada WebView2 (se aplican a la pestaña activa)
        private void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null && BrowserTabControl.SelectedItem != null &&
                ((TabItem)BrowserTabControl.SelectedItem).Content is Grid contentGrid &&
                contentGrid.Children.Contains(currentWebView))
            {
                // Solo actualiza si este WebView es el de la pestaña actualmente seleccionada
                UrlTextBox.Text = currentWebView.CoreWebView2.Source;
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null && BrowserTabControl.SelectedItem != null &&
                ((TabItem)BrowserTabControl.SelectedItem).Content is Grid contentGrid &&
                contentGrid.Children.Contains(currentWebView))
            {
                if (!e.IsSuccess)
                {
                    MessageBox.Show($"La navegación a {currentWebView.CoreWebView2.Source} falló con el código de error {e.WebErrorStatus}", "Error de Navegación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Aquí puedes añadir lógica para bloquear ciertas URLs si lo deseas
        }

        private void WebView_DocumentTitleChanged(object sender, object e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null)
            {
                // Encuentra la pestaña asociada a este WebView2
                var tabItem = _browserTabs.FirstOrDefault(t => t.WebView == currentWebView);
                if (tabItem != null)
                {
                    ((TextBlock)((DockPanel)tabItem.Tab.Header).Children[0]).Text = currentWebView.CoreWebView2.DocumentTitle;
                }

                // Si es la pestaña activa, actualiza también el título de la ventana principal
                if (BrowserTabControl.SelectedItem == tabItem.Tab)
                {
                    this.Title = currentWebView.CoreWebView2.DocumentTitle + " - Mi Navegador Web";
                }
            }
        }


        // Eventos de botones de navegación (aplicados a la pestaña activa)
        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrlInCurrentTab();
        }

        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToUrlInCurrentTab();
            }
        }

        private void NavigateToUrlInCurrentTab()
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una pestaña activa o el navegador no está listo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
                currentWebView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al navegar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null && currentWebView.CoreWebView2.CanGoBack)
            {
                currentWebView.CoreWebView2.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null && currentWebView.CoreWebView2.CanGoForward)
            {
                currentWebView.CoreWebView2.GoForward();
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e) // Nuevo: Botón Recargar
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.Reload();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.Navigate(_defaultHomePage);
            }
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            Button closeButton = sender as Button;
            TabItem tabToClose = closeButton?.Tag as TabItem;

            if (tabToClose != null)
            {
                BrowserTabControl.Items.Remove(tabToClose);
                var browserTabItem = _browserTabs.FirstOrDefault(t => t.Tab == tabToClose);
                if (browserTabItem != null)
                {
                    // Desechar el WebView2 para liberar recursos
                    browserTabItem.WebView.Dispose();
                    _browserTabs.Remove(browserTabItem);
                }

                if (BrowserTabControl.Items.Count == 0)
                {
                    // Si no quedan pestañas, añade una nueva o cierra la ventana
                    AddNewTab(); // Optamos por añadir una nueva pestaña
                    // Application.Current.Shutdown(); // O podrías cerrar la aplicación
                }
            }
        }

        private void BrowserTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUrlTextBoxFromCurrentTab();
        }

        private void UpdateUrlTextBoxFromCurrentTab()
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                UrlTextBox.Text = currentWebView.CoreWebView2.Source;
                this.Title = currentWebView.CoreWebView2.DocumentTitle + " - Mi Navegador Web";
            }
            else
            {
                UrlTextBox.Text = string.Empty;
                this.Title = "Mi Navegador Web";
            }
        }


        private WebView2 GetCurrentWebView()
        {
            if (BrowserTabControl.SelectedItem is TabItem selectedTabItem)
            {
                if (selectedTabItem.Content is Grid contentGrid && contentGrid.Children.Count > 0 && contentGrid.Children[0] is WebView2 webView)
                {
                    return webView;
                }
            }
            return null;
        }


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow(_defaultHomePage);
            if (settingsWindow.ShowDialog() == true)
            {
                _defaultHomePage = settingsWindow.HomePage;
                SaveSettings();
                MessageBox.Show("Configuración guardada. La nueva página de inicio se aplicará al abrir nuevas pestañas o al hacer clic en 'Inicio'.", "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadSettings()
        {
            string savedHomePage = ConfigurationManager.AppSettings[HomePageSettingKey];
            if (!string.IsNullOrEmpty(savedHomePage))
            {
                _defaultHomePage = savedHomePage;
            }
        }

        private void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[HomePageSettingKey] == null)
            {
                config.AppSettings.Settings.Add(HomePageSettingKey, _defaultHomePage);
            }
            else
            {
                config.AppSettings.Settings[HomePageSettingKey].Value = _defaultHomePage;
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        // Clase auxiliar para gestionar la información de cada pestaña
        private class BrowserTabItem
        {
            public TabItem Tab { get; set; }
            public WebView2 WebView { get; set; }
            public TextBlock HeaderTextBlock { get; set; } // Para actualizar el texto del encabezado
        }
    }
}
