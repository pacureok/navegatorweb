using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration; // Necesario para ConfigurationManager
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO; // Necesario para Path.Combine y operaciones de archivo

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _defaultHomePage = "https://www.google.com"; // Página de inicio predeterminada
        private const string HomePageSettingKey = "DefaultHomePage"; // Clave para la configuración de la página de inicio
        private const string AdBlockerSettingKey = "AdBlockerEnabled"; // Nueva clave para el estado del bloqueador de anuncios

        // Lista para mantener un seguimiento de todas las pestañas abiertas
        private List<BrowserTabItem> _browserTabs = new List<BrowserTabItem>();

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings(); // Cargar configuraciones al iniciar la aplicación
        }

        /// <summary>
        /// Se ejecuta cuando la ventana principal se ha cargado completamente.
        /// Aquí inicializamos la primera pestaña y cargamos la lista de dominios bloqueados.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Cargar dominios bloqueados desde un archivo (ej: "blocked_domains.txt" en la raíz de la app)
            // Asegúrate de que este archivo exista en la misma carpeta que el ejecutable de tu aplicación.
            string blockedDomainsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocked_domains.txt");
            AdBlocker.LoadBlockedDomainsFromFile(blockedDomainsFilePath);

            AddNewTab(_defaultHomePage); // Abre la primera pestaña con la página de inicio
        }

        /// <summary>
        /// Agrega una nueva pestaña al navegador.
        /// </summary>
        /// <param name="url">URL opcional para cargar en la nueva pestaña. Si es nulo, usa la página de inicio predeterminada.</param>
        private void AddNewTab(string url = null)
        {
            // Crear un nuevo TabItem (la pestaña visual)
            TabItem newTabItem = new TabItem();
            newTabItem.Name = "Tab" + (_browserTabs.Count + 1); // Nombre único para la pestaña

            // Crear un panel para el encabezado de la pestaña (título + botón de cerrar)
            DockPanel tabHeaderPanel = new DockPanel();
            TextBlock headerText = new TextBlock { Text = "Cargando..." }; // Texto inicial del encabezado
            Button closeButton = new Button
            {
                Content = "✖", // Carácter 'X' para cerrar
                Width = 20,
                Height = 20,
                Margin = new Thickness(5, 0, 0, 0),
                ToolTip = "Cerrar Pestaña" // Tooltip al pasar el ratón
            };
            closeButton.Click += CloseTabButton_Click; // Asignar evento al botón de cerrar
            closeButton.Tag = newTabItem; // Asociar el botón a su TabItem correspondiente
            DockPanel.SetDock(headerText, Dock.Left);
            DockPanel.SetDock(closeButton, Dock.Right);
            tabHeaderPanel.Children.Add(headerText);
            tabHeaderPanel.Children.Add(closeButton);
            newTabItem.Header = tabHeaderPanel; // Asignar el panel como encabezado de la pestaña

            // Crear una nueva instancia de WebView2 para el contenido de la pestaña
            WebView2 webView = new WebView2();
            webView.Source = new Uri(url ?? _defaultHomePage); // Cargar la URL especificada o la página de inicio
            webView.Name = "WebView" + (_browserTabs.Count + 1); // Nombre único para el WebView
            webView.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView.VerticalAlignment = VerticalAlignment.Stretch;

            // Enlazar eventos del WebView2 para esta pestaña
            webView.Loaded += WebView_Loaded; // Para asegurar que CoreWebView2 esté listo
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted; // Para adjuntar WebResourceRequested
            webView.NavigationStarting += WebView_NavigationStarting;
            webView.SourceChanged += WebView_SourceChanged;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.CoreWebView2.DocumentTitleChanged += WebView_DocumentTitleChanged;

            // Contenido de la pestaña: un Grid que contiene el WebView2
            Grid tabContent = new Grid();
            tabContent.Children.Add(webView);
            newTabItem.Content = tabContent;

            // Añadir la nueva pestaña al TabControl principal
            BrowserTabControl.Items.Add(newTabItem);
            BrowserTabControl.SelectedItem = newTabItem; // Seleccionar la nueva pestaña automáticamente

            // Crear un objeto BrowserTabItem para rastrear la pestaña y sus componentes
            BrowserTabItem browserTab = new BrowserTabItem
            {
                Tab = newTabItem,
                WebView = webView,
                HeaderTextBlock = headerText
            };
            _browserTabs.Add(browserTab); // Añadir a la lista de pestañas

            // Actualizar la barra de URL para reflejar la URL de la nueva pestaña activa
            UpdateUrlTextBoxFromCurrentTab();
        }

        /// <summary>
        /// Se ejecuta cuando un WebView2 se ha cargado en la interfaz de usuario.
        /// Asegura la inicialización de CoreWebView2.
        /// </summary>
        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null)
            {
                // Asegura que el CoreWebView2 subyacente esté inicializado.
                // Esto es crucial antes de interactuar con CoreWebView2.
                await currentWebView.EnsureCoreWebView2Async(null);
            }
        }

        /// <summary>
        /// Se ejecuta cuando CoreWebView2 ha completado su inicialización.
        /// Aquí es el lugar seguro para adjuntar el manejador de WebResourceRequested.
        /// </summary>
        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null && e.IsSuccess)
            {
                // Adjuntar el manejador de eventos para interceptar solicitudes de red.
                // Primero, desadjuntamos para evitar duplicados si el evento se adjunta varias veces.
                currentWebView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
                currentWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            }
        }

        /// <summary>
        /// Intercepta las solicitudes de recursos web para implementar el bloqueador de anuncios.
        /// </summary>
        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            // Si el bloqueador de anuncios está habilitado y la URL está en la lista de bloqueo, cancela la solicitud.
            if (AdBlocker.IsEnabled && AdBlocker.IsBlocked(e.Request.Uri))
            {
                // Crea una respuesta HTTP 403 (Forbidden) para bloquear el recurso.
                e.Response = ((WebView2)sender).CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Forbidden", "Content-Type: text/plain\nAccess-Control-Allow-Origin: *"
                );
            }
        }


        /// <summary>
        /// Se ejecuta cuando la URL del WebView2 cambia.
        /// Actualiza la barra de dirección si es la pestaña activa.
        /// </summary>
        private void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null && BrowserTabControl.SelectedItem != null &&
                ((TabItem)BrowserTabControl.SelectedItem).Content is Grid contentGrid &&
                contentGrid.Children.Contains(currentWebView))
            {
                // Solo actualiza la barra de dirección si este WebView pertenece a la pestaña seleccionada actualmente.
                UrlTextBox.Text = currentWebView.CoreWebView2.Source;
            }
        }

        /// <summary>
        /// Se ejecuta cuando la navegación en un WebView2 ha completado.
        /// Muestra un mensaje de error si la navegación falló.
        /// </summary>
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

        /// <summary>
        /// Se ejecuta antes de que comience una navegación en un WebView2.
        /// </summary>
        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Aquí puedes añadir lógica para bloquear ciertas URLs o modificar la solicitud antes de que comience la navegación.
        }

        /// <summary>
        /// Se ejecuta cuando el título del documento en un WebView2 cambia.
        /// Actualiza el encabezado de la pestaña y el título de la ventana principal.
        /// </summary>
        private void WebView_DocumentTitleChanged(object sender, object e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null)
            {
                // Encuentra la pestaña asociada a este WebView2.
                var tabItem = _browserTabs.FirstOrDefault(t => t.WebView == currentWebView);
                if (tabItem != null)
                {
                    // Actualiza el texto del encabezado de la pestaña.
                    tabItem.HeaderTextBlock.Text = currentWebView.CoreWebView2.DocumentTitle;
                }

                // Si es la pestaña activa, actualiza también el título de la ventana principal.
                if (BrowserTabControl.SelectedItem == tabItem?.Tab)
                {
                    this.Title = currentWebView.CoreWebView2.DocumentTitle + " - Mi Navegador Web";
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Ir" o la tecla Enter en la barra de URL.
        /// </summary>
        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrlInCurrentTab();
        }

        /// <summary>
        /// Maneja la tecla Enter en la barra de URL.
        /// </summary>
        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToUrlInCurrentTab();
            }
        }

        /// <summary>
        /// Navega a la URL en la pestaña actualmente activa.
        /// </summary>
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
                // Intentar prefijar con "http://" si no es una URL válida.
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

        /// <summary>
        /// Navega hacia atrás en el historial de la pestaña activa.
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null && currentWebView.CoreWebView2.CanGoBack)
            {
                currentWebView.CoreWebView2.GoBack();
            }
        }

        /// <summary>
        /// Navega hacia adelante en el historial de la pestaña activa.
        /// </summary>
        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null && currentWebView.CoreWebView2.CanGoForward)
            {
                currentWebView.CoreWebView2.GoForward();
            }
        }

        /// <summary>
        /// Recarga la página actual en la pestaña activa.
        /// </summary>
        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.Reload();
            }
        }

        /// <summary>
        /// Navega a la página de inicio predeterminada en la pestaña activa.
        /// </summary>
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.Navigate(_defaultHomePage);
            }
        }

        /// <summary>
        /// Agrega una nueva pestaña al hacer clic en el botón "+".
        /// </summary>
        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        /// <summary>
        /// Cierra una pestaña cuando se hace clic en su botón "X".
        /// </summary>
        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            Button closeButton = sender as Button;
            TabItem tabToClose = closeButton?.Tag as TabItem; // Obtener el TabItem asociado al botón

            if (tabToClose != null)
            {
                BrowserTabControl.Items.Remove(tabToClose); // Eliminar la pestaña del TabControl
                var browserTabItem = _browserTabs.FirstOrDefault(t => t.Tab == tabToClose);
                if (browserTabItem != null)
                {
                    // Desechar el WebView2 para liberar recursos asociados a esa pestaña.
                    browserTabItem.WebView.Dispose();
                    _browserTabs.Remove(browserTabItem); // Eliminar de nuestra lista de seguimiento
                }

                // Si no quedan pestañas, abre una nueva por defecto para evitar una ventana vacía.
                if (BrowserTabControl.Items.Count == 0)
                {
                    AddNewTab();
                }
            }
        }

        /// <summary>
        /// Se ejecuta cuando la selección de la pestaña en el TabControl cambia.
        /// Actualiza la barra de URL y el título de la ventana.
        /// </summary>
        private void BrowserTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUrlTextBoxFromCurrentTab();
        }

        /// <summary>
        /// Actualiza el texto de la barra de URL y el título de la ventana
        /// con la información de la pestaña actualmente seleccionada.
        /// </summary>
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
                // Si no hay pestaña activa o el WebView no está listo, limpia la barra de URL y el título.
                UrlTextBox.Text = string.Empty;
                this.Title = "Mi Navegador Web";
            }
        }

        /// <summary>
        /// Obtiene la instancia de WebView2 de la pestaña actualmente seleccionada.
        /// </summary>
        /// <returns>El WebView2 de la pestaña activa, o null si no hay una pestaña seleccionada o su contenido no es un WebView2.</returns>
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

        /// <summary>
        /// Maneja el clic en el botón "Opciones". Abre la ventana de configuración.
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Pasa la página de inicio actual y el estado actual del bloqueador de anuncios a la ventana de configuración.
            SettingsWindow settingsWindow = new SettingsWindow(_defaultHomePage, AdBlocker.IsEnabled);
            if (settingsWindow.ShowDialog() == true) // Muestra la ventana de configuración como un diálogo modal
            {
                // Si el usuario hizo clic en "Guardar" en la ventana de configuración
                _defaultHomePage = settingsWindow.HomePage; // Actualiza la página de inicio
                AdBlocker.IsEnabled = settingsWindow.IsAdBlockerEnabled; // Actualiza el estado del bloqueador
                SaveSettings(); // Guarda ambas configuraciones en App.config
                MessageBox.Show("Configuración guardada. La nueva página de inicio se aplicará al abrir nuevas pestañas o al hacer clic en 'Inicio'. La configuración del bloqueador de anuncios es efectiva inmediatamente.", "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Carga las configuraciones de la aplicación (página de inicio y estado del bloqueador) desde App.config.
        /// </summary>
        private void LoadSettings()
        {
            // Cargar la página de inicio guardada
            string savedHomePage = ConfigurationManager.AppSettings[HomePageSettingKey];
            if (!string.IsNullOrEmpty(savedHomePage))
            {
                _defaultHomePage = savedHomePage;
            }

            // Cargar el estado del bloqueador de anuncios
            string savedAdBlockerState = ConfigurationManager.AppSettings[AdBlockerSettingKey];
            if (bool.TryParse(savedAdBlockerState, out bool isEnabled))
            {
                AdBlocker.IsEnabled = isEnabled;
            }
            else
            {
                AdBlocker.IsEnabled = false; // Por defecto, el bloqueador está deshabilitado si no hay configuración o es inválida.
            }
        }

        /// <summary>
        /// Guarda las configuraciones actuales (página de inicio y estado del bloqueador) en App.config.
        /// </summary>
        private void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Guardar página de inicio
            if (config.AppSettings.Settings[HomePageSettingKey] == null)
            {
                config.AppSettings.Settings.Add(HomePageSettingKey, _defaultHomePage);
            }
            else
            {
                config.AppSettings.Settings[HomePageSettingKey].Value = _defaultHomePage;
            }

            // Guardar estado del bloqueador de anuncios
            if (config.AppSettings.Settings[AdBlockerSettingKey] == null)
            {
                config.AppSettings.Settings.Add(AdBlockerSettingKey, AdBlocker.IsEnabled.ToString());
            }
            else
            {
                config.AppSettings.Settings[AdBlockerSettingKey].Value = AdBlocker.IsEnabled.ToString();
            }

            config.Save(ConfigurationSaveMode.Modified); // Guarda los cambios en el archivo de configuración
            ConfigurationManager.RefreshSection("appSettings"); // Refresca la sección para que los nuevos valores estén disponibles inmediatamente
        }

        /// <summary>
        /// Clase auxiliar para gestionar la información de cada pestaña del navegador.
        /// </summary>
        private class BrowserTabItem
        {
            public TabItem Tab { get; set; } // El control TabItem de WPF
            public WebView2 WebView { get; set; } // La instancia de WebView2 dentro de la pestaña
            public TextBlock HeaderTextBlock { get; set; } // El TextBlock que muestra el título en el encabezado de la pestaña
        }
    }
}
