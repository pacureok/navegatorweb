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
using System.Text.Json; // Necesario para JsonSerializer (para historial y marcadores)

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _defaultHomePage = "https://www.google.com"; // Página de inicio predeterminada
        private const string HomePageSettingKey = "DefaultHomePage"; // Clave para la configuración de la página de inicio
        private const string AdBlockerSettingKey = "AdBlockerEnabled"; // Clave para el estado del bloqueador de anuncios
        private const string DefaultSearchEngineSettingKey = "DefaultSearchEngine"; // Clave para el motor de búsqueda predeterminado
        private const string TabSuspensionSettingKey = "TabSuspensionEnabled"; // Nueva clave para el estado de la suspensión de pestañas

        private string _defaultSearchEngineUrl = "https://www.google.com/search?q="; // URL base del motor de búsqueda predeterminado
        private bool _isTabSuspensionEnabled = false; // Estado de la suspensión de pestañas

        // Lista para mantener un seguimiento de todas las pestañas abiertas
        private List<BrowserTabItem> _browserTabs = new List<BrowserTabItem>();

        // Entornos de WebView2
        private CoreWebView2Environment _defaultEnvironment;
        private CoreWebView2Environment _incognitoEnvironment; // Para el modo incógnito

        // Nuevo: Contenido del script de modo lectura
        private string _readerModeScript = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings(); // Cargar configuraciones al iniciar la aplicación
            InitializeEnvironments(); // Inicializar los entornos de WebView2
            LoadReaderModeScript(); // NUEVO: Cargar el script de modo lectura
        }

        /// <summary>
        /// Carga el contenido del archivo ReaderMode.js.
        /// </summary>
        private void LoadReaderModeScript()
        {
            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReaderMode.js");
                if (File.Exists(scriptPath))
                {
                    _readerModeScript = File.ReadAllText(scriptPath);
                }
                else
                {
                    MessageBox.Show("Advertencia: El archivo 'ReaderMode.js' no se encontró. El modo lectura no funcionará.", "Archivo Faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el script de modo lectura: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Inicializa los entornos CoreWebView2 para el modo normal e incógnito.
        /// </summary>
        private async void InitializeEnvironments()
        {
            try
            {
                // Entorno para navegación normal (persistente)
                string defaultUserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiNavegadorWeb", "UserData");
                _defaultEnvironment = await CoreWebView2Environment.CreateAsync(null, defaultUserDataFolder);

                // Entorno para navegación incógnito (no persistente)
                // Se crea un directorio temporal que se eliminará al cerrar la aplicación.
                string incognitoUserDataFolder = Path.Combine(Path.GetTempPath(), "MiNavegadorWebIncognito", Guid.NewGuid().ToString());
                _incognitoEnvironment = await CoreWebView2Environment.CreateAsync(null, incognitoUserDataFolder, new CoreWebView2EnvironmentOptions {
                    IsCustomCrashReportingEnabled = false // Para modo incógnito, puedes deshabilitar crash reporting si quieres más privacidad
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar los entornos del navegador: {ex.Message}\nPor favor, asegúrate de tener WebView2 Runtime instalado.", "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
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
        /// <param name="isIncognito">Indica si la nueva pestaña debe abrirse en modo incógnito.</param>
        private async void AddNewTab(string url = null, bool isIncognito = false)
        {
            // Esperar a que los entornos se inicialicen
            if (_defaultEnvironment == null || _incognitoEnvironment == null)
            {
                await System.Threading.Tasks.Task.Delay(100); // Pequeña espera si no están listos
                if (_defaultEnvironment == null || _incognitoEnvironment == null)
                {
                    MessageBox.Show("El navegador no está listo. Por favor, reinicia la aplicación.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Crear un nuevo TabItem (la pestaña visual)
            TabItem newTabItem = new TabItem();
            newTabItem.Name = "Tab" + (_browserTabs.Count + 1); // Nombre único para la pestaña
            newTabItem.Tag = isIncognito; // Guardar el estado de incógnito en el Tag de la pestaña

            // Crear un panel para el encabezado de la pestaña (título + botón de cerrar)
            DockPanel tabHeaderPanel = new DockPanel();
            TextBlock headerText = new TextBlock { Text = "Cargando..." }; // Texto inicial del encabezado
            if (isIncognito)
            {
                headerText.Text = "(Incógnito) Cargando...";
            }

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

            // ***** Importante: Usar el entorno correcto para el WebView2 *****
            // La inicialización del CoreWebView2 con el entorno correcto debe ocurrir antes de que se establezca el Source
            // o se navegue. El evento CoreWebView2InitializationCompleted es el lugar más seguro.
            if (isIncognito)
            {
                webView.CoreWebView2InitializationCompleted += (s, e) => ConfigureCoreWebView2(s as WebView2, e, _incognitoEnvironment);
            }
            else
            {
                webView.CoreWebView2InitializationCompleted += (s, e) => ConfigureCoreWebView2(s as WebView2, e, _defaultEnvironment);
            }

            // Enlazar eventos comunes del WebView2 para esta pestaña
            webView.Loaded += WebView_Loaded;
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
                HeaderTextBlock = headerText,
                IsIncognito = isIncognito // Marcar si es incógnito
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
                // Espera por la inicialización de CoreWebView2 si no ha ocurrido ya
                await currentWebView.EnsureCoreWebView2Async(null);
            }
        }

        /// <summary>
        /// Se ejecuta cuando CoreWebView2 ha completado su inicialización.
        /// Configura el CoreWebView2 con el entorno y eventos necesarios.
        /// </summary>
        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e, CoreWebView2Environment environment)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null && e.IsSuccess)
            {
                // Asegurar que se use el entorno correcto
                if (currentWebView.CoreWebView2 == null || currentWebView.CoreWebView2.Environment != environment)
                {
                    // Esto solo debería ocurrir si el WebView2 ya estaba inicializado con otro entorno,
                    // lo cual no debería pasar en AddNewTab. Es una medida de seguridad.
                    // Para cambiar el entorno de un WebView2 existente, se debe recrear el WebView2.
                    // currentWebView.CoreWebView2 = await environment.CreateCoreWebView2Async(currentWebView.Handle); // Esto no es directo con el control WPF
                }

                // Desvincular eventos antes de (posiblemente) re-adjuntar para evitar duplicados
                currentWebView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
                currentWebView.CoreWebView2.DownloadStarting -= CoreWebView2_DownloadStarting;

                // Adjuntar el manejador de eventos para interceptar solicitudes de red (bloqueador de anuncios).
                currentWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

                // Habilita las herramientas de desarrollador (F12)
                currentWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                // Suscribirse al evento DownloadStarting
                currentWebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
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
        /// Maneja el inicio de una descarga desde WebView2.
        /// Permite al usuario elegir la ruta de guardado y actualiza el gestor de descargas.
        /// </summary>
        private async void CoreWebView2_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            // Cancelar la descarga predeterminada de WebView2 para manejarla manualmente
            e.Handled = true;

            // Crear una entrada de descarga inicial
            DownloadEntry newDownload = new DownloadEntry
            {
                FileName = e.ResultFilePath.Split('\\').Last(), // Obtener solo el nombre del archivo
                Url = e.DownloadOperation.Uri,
                TotalBytes = e.DownloadOperation.TotalBytesToReceive,
                TargetPath = e.ResultFilePath, // Ruta predeterminada
                State = CoreWebView2DownloadState.InProgress,
                Progress = 0
            };

            // Mostrar un diálogo para elegir la ubicación de guardado
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = newDownload.FileName,
                Filter = "Todos los archivos (*.*)|*.*",
                Title = "Guardar descarga como..."
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                newDownload.TargetPath = saveFileDialog.FileName; // Actualizar la ruta de guardado elegida
                e.ResultFilePath = saveFileDialog.FileName; // Informar a WebView2 la nueva ruta

                // Añadir/actualizar la descarga en el gestor
                DownloadManager.AddOrUpdateDownload(newDownload);

                // Suscribirse a los eventos de progreso y estado de la operación de descarga
                e.DownloadOperation.BytesReceivedChanged += (s, args) =>
                {
                    newDownload.ReceivedBytes = e.DownloadOperation.BytesReceived;
                    if (newDownload.TotalBytes > 0)
                    {
                        newDownload.Progress = (int)((double)newDownload.ReceivedBytes / newDownload.TotalBytes * 100);
                    }
                    // Actualizar la UI del gestor de descargas (si está abierta)
                    DownloadManager.AddOrUpdateDownload(newDownload);
                };

                e.DownloadOperation.StateChanged += (s, args) =>
                {
                    newDownload.State = e.DownloadOperation.State;
                    newDownload.IsActive = (e.DownloadOperation.State == CoreWebView2DownloadState.InProgress);
                    if (newDownload.State == CoreWebView2DownloadState.Completed || newDownload.State == CoreWebView2DownloadState.Interrupted)
                    {
                        newDownload.EndTime = DateTime.Now;
                        MessageBox.Show($"Descarga de '{newDownload.FileName}' ha {newDownload.State}.", "Descarga Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    // Actualizar la UI del gestor de descargas (si está abierta)
                    DownloadManager.AddOrUpdateDownload(newDownload);
                };
            }
            else
            {
                // Si el usuario cancela el diálogo de guardado, cancelar la descarga
                e.Cancel = true;
                MessageBox.Show("Descarga cancelada por el usuario.", "Descarga Cancelada", MessageBoxButton.OK, MessageBoxImage.Information);
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
        /// Muestra un mensaje de error si la navegación falló y oculta el indicador de carga.
        /// </summary>
        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            var browserTab = _browserTabs.FirstOrDefault(t => t.WebView == currentWebView);

            if (currentWebView != null && BrowserTabControl.SelectedItem != null &&
                ((TabItem)BrowserTabControl.SelectedItem).Content is Grid contentGrid &&
                contentGrid.Children.Contains(currentWebView))
            {
                if (!e.IsSuccess)
                {
                    MessageBox.Show($"La navegación a {currentWebView.CoreWebView2.Source} falló con el código de error {e.WebErrorStatus}", "Error de Navegación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Añadir la página al historial SOLO SI NO ES UNA PESTAÑA INCÓGNITO
                    if (browserTab != null && !browserTab.IsIncognito)
                    {
                        HistoryManager.AddHistoryEntry(currentWebView.CoreWebView2.Source, currentWebView.CoreWebView2.DocumentTitle);
                    }
                }
            }
            LoadingProgressBar.Visibility = Visibility.Collapsed; // Ocultar el indicador de carga
        }

        /// <summary>
        /// Se ejecuta antes de que comience una navegación en un WebView2.
        /// Muestra el indicador de carga.
        /// </summary>
        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            LoadingProgressBar.Visibility = Visibility.Visible; // Mostrar el indicador de carga
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
                    string title = currentWebView.CoreWebView2.DocumentTitle;
                    if (tabItem.IsIncognito)
                    {
                        tabItem.HeaderTextBlock.Text = "(Incógnito) " + title;
                    }
                    else
                    {
                        tabItem.HeaderTextBlock.Text = title;
                    }
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
        /// Si el texto no es una URL válida, lo trata como una búsqueda.
        /// </summary>
        private void NavigateToUrlInCurrentTab()
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una pestaña activa o el navegador no está listo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string input = UrlTextBox.Text.Trim();
            string urlToNavigate = input;

            // Intentar crear una URI para validar si es una URL bien formada
            if (!Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                // Si no es una URL válida (o no tiene http/https), asumimos que es una búsqueda
                urlToNavigate = _defaultSearchEngineUrl + Uri.EscapeDataString(input);
            }
            // Si es una URL válida pero no tiene esquema (ej. "google.com"), añadir "http://"
            else if (uriResult.IsAbsoluteUri && string.IsNullOrEmpty(uriResult.Scheme))
            {
                urlToNavigate = "http://" + input;
            }


            try
            {
                currentWebView.CoreWebView2.Navigate(urlToNavigate);
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
            AddNewTab(); // Abre una pestaña normal por defecto
        }

        /// <summary>
        /// Maneja el clic en el botón "Historial". Abre la ventana del historial.
        /// </summary>
        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow historyWindow = new HistoryWindow();
            if (historyWindow.ShowDialog() == true) // Muestra la ventana del historial como un diálogo
            {
                // Si el usuario seleccionó una URL del historial y hizo doble clic
                if (!string.IsNullOrEmpty(historyWindow.SelectedUrl))
                {
                    UrlTextBox.Text = historyWindow.SelectedUrl;
                    NavigateToUrlInCurrentTab(); // Navega a la URL seleccionada
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Marcadores". Abre la ventana de marcadores.
        /// </summary>
        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            BookmarksWindow bookmarksWindow = new BookmarksWindow();
            if (bookmarksWindow.ShowDialog() == true) // Muestra la ventana de marcadores como un diálogo
            {
                // Si el usuario seleccionó una URL de los marcadores y hizo doble clic
                if (!string.IsNullOrEmpty(bookmarksWindow.SelectedUrl))
                {
                    UrlTextBox.Text = bookmarksWindow.SelectedUrl;
                    NavigateToUrlInCurrentTab(); // Navega a la URL seleccionada
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Añadir Marcador". Añade la página actual a los marcadores.
        /// </summary>
        private void AddBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                var browserTab = _browserTabs.FirstOrDefault(t => t.WebView == currentWebView);
                if (browserTab != null && browserTab.IsIncognito)
                {
                    MessageBox.Show("No se pueden añadir marcadores en modo incógnito.", "Error al Añadir Marcador", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string url = currentWebView.CoreWebView2.Source;
                string title = currentWebView.CoreWebView2.DocumentTitle;

                if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(title))
                {
                    BookmarkManager.AddBookmark(url, title);
                }
                else
                {
                    MessageBox.Show("No se pudo añadir la página a marcadores. Asegúrate de que la página esté cargada y tenga un título.", "Error al Añadir Marcador", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("No hay una página activa para añadir a marcadores.", "Error al Añadir Marcador", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Descargas". Abre la ventana del gestor de descargas.
        /// </summary>
        private void DownloadsButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadsWindow downloadsWindow = new DownloadsWindow();
            downloadsWindow.Show(); // Mostrar la ventana de descargas (no modal, para que el usuario pueda seguir navegando)
        }

        /// <summary>
        /// Nuevo: Maneja el clic en el botón "Modo Lectura". Inyecta el script para activar/desactivar el modo lectura.
        /// </summary>
        private async void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                if (!string.IsNullOrEmpty(_readerModeScript))
                {
                    try
                    {
                        // Inyectar el script en la página actual
                        await currentWebView.CoreWebView2.ExecuteScriptAsync(_readerModeScript);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al aplicar el modo lectura: {ex.Message}", "Error de Modo Lectura", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("El script de modo lectura no está cargado. Asegúrate de que 'ReaderMode.js' exista.", "Error de Modo Lectura", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("No hay una página activa para aplicar el modo lectura.", "Error de Modo Lectura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Nuevo: Maneja el clic en el botón "Modo Incógnito". Abre una nueva ventana en modo incógnito.
        /// </summary>
        private void IncognitoButton_Click(object sender, RoutedEventArgs e)
        {
            // Para simplicidad en este ejemplo, abriremos una nueva PESTAÑA incógnito.
            AddNewTab(_defaultHomePage, isIncognito: true);
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
                    browserTabItem.WebView?.Dispose(); // Usar ?. para seguridad si WebView ya es null
                    _browserTabs.Remove(browserTabItem); // Eliminar de nuestra lista de seguimiento
                }

                // Si no quedan pestañas, abre una nueva por defecto para evitar una ventana vacía.
                if (BrowserTabControl.Items.Count == 0)
                {
                    // Si se cerró la última pestaña, abrimos una nueva, por defecto normal
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

            // Si la pestaña seleccionada está suspendida, reactivarla
            if (BrowserTabControl.SelectedItem is TabItem selectedTabItem)
            {
                var browserTab = _browserTabs.FirstOrDefault(t => t.Tab == selectedTabItem);
                if (browserTab != null && browserTab.WebView == null) // Si el WebView es nulo, la pestaña está suspendida
                {
                    if (_isTabSuspensionEnabled) // Solo reactivar si la suspensión está habilitada
                    {
                        // Recrear WebView2 y cargar la URL
                        string urlToReload = selectedTabItem.Tag?.ToString(); // Obtener la URL guardada

                        WebView2 newWebView = new WebView2();
                        newWebView.Source = new Uri(urlToReload ?? _defaultHomePage);
                        newWebView.Name = "WebView" + (BrowserTabControl.Items.IndexOf(selectedTabItem) + 1);
                        newWebView.HorizontalAlignment = HorizontalAlignment.Stretch;
                        newWebView.VerticalAlignment = VerticalAlignment.Stretch;

                        // Enlazar eventos (similar a AddNewTab, asegurando el entorno correcto)
                        newWebView.Loaded += WebView_Loaded;
                        // Usar el entorno por defecto para pestañas reactivadas si no eran incógnito
                        CoreWebView2Environment envToUse = browserTab.IsIncognito ? _incognitoEnvironment : _defaultEnvironment;
                        newWebView.CoreWebView2InitializationCompleted += (s, ev) => ConfigureCoreWebView2(newWebView, ev, envToUse); // Pasar e y envToUse
                        newWebView.NavigationStarting += WebView_NavigationStarting;
                        newWebView.SourceChanged += WebView_SourceChanged;
                        newWebView.NavigationCompleted += WebView_NavigationCompleted;
                        newWebView.CoreWebView2.DocumentTitleChanged += WebView_DocumentTitleChanged;

                        // Reemplazar el contenido de la pestaña
                        Grid tabContent = new Grid();
                        tabContent.Children.Add(newWebView);
                        selectedTabItem.Content = tabContent;

                        browserTab.WebView = newWebView; // Actualizar la referencia al nuevo WebView

                        // Restaurar el título original (quitar "(Suspendida)")
                        string originalHeaderText = browserTab.HeaderTextBlock.Text;
                        if (originalHeaderText.StartsWith("(Suspendida) ")) // Evitar duplicar el prefijo
                        {
                            tabItem.HeaderTextBlock.Text = originalHeaderText.Replace("(Suspendida) ", "");
                        }
                    }
                    else
                    {
                        // Si la suspensión no está activa pero la pestaña está suspendida (ej. se deshabilitó la opción)
                        // recargarla como una nueva pestaña normal.
                        string urlToReload = selectedTabItem.Tag?.ToString();
                        AddNewTab(urlToReload);
                        BrowserTabControl.Items.Remove(selectedTabItem); // Quitar la pestaña vieja y suspendida
                    }
                }
            }
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
            // Pasa la página de inicio actual, el estado del bloqueador, la URL del motor de búsqueda y el estado de suspensión
            SettingsWindow settingsWindow = new SettingsWindow(_defaultHomePage, AdBlocker.IsEnabled, _defaultSearchEngineUrl, _isTabSuspensionEnabled);

            // Suscribirse a los nuevos eventos de la ventana de configuración
            settingsWindow.OnClearBrowsingData += SettingsWindow_OnClearBrowsingData;
            settingsWindow.OnSuspendInactiveTabs += SettingsWindow_OnSuspendInactiveTabs;


            if (settingsWindow.ShowDialog() == true) // Muestra la ventana de configuración como un diálogo modal
            {
                // Si el usuario hizo clic en "Guardar" en la ventana de configuración
                _defaultHomePage = settingsWindow.HomePage; // Actualiza la página de inicio
                AdBlocker.IsEnabled = settingsWindow.IsAdBlockerEnabled; // Actualiza el estado del bloqueador
                _defaultSearchEngineUrl = settingsWindow.DefaultSearchEngineUrl; // Actualiza la URL del motor de búsqueda
                _isTabSuspensionEnabled = settingsWindow.IsTabSuspensionEnabled; // Actualiza el estado de suspensión
                SaveSettings(); // Guarda todas las configuraciones en App.config
                MessageBox.Show("Configuración guardada. Los cambios se aplicarán al abrir nuevas pestañas o al hacer clic en 'Inicio'.", "Configuración Guardada", MessageBoxButton.OK, Image.Information);
            }

            // Es importante desuscribirse de los eventos para evitar fugas de memoria
            settingsWindow.OnClearBrowsingData -= SettingsWindow_OnClearBrowsingData;
            settingsWindow.OnSuspendInactiveTabs -= SettingsWindow_OnSuspendInactiveTabs;
        }

        /// <summary>
        /// Nuevo: Manejador para borrar datos de navegación.
        /// Se invoca desde la ventana de configuración.
        /// </summary>
        private async void SettingsWindow_OnClearBrowsingData()
        {
            // Esto borrará datos de la carpeta UserData del entorno predeterminado.
            WebView2 anyWebView = GetCurrentWebView(); // Solo necesitamos una instancia para acceder al entorno

            if (_defaultEnvironment != null)
            {
                CoreWebView2BrowserDataKinds dataKinds =
                    CoreWebView2BrowserDataKinds.Cookies |
                    CoreWebView2BrowserDataKinds.DiskCache |
                    CoreWebView2BrowserDataKinds.Downloads |
                    CoreWebView2BrowserDataKinds.GeneralAutofill |
                    CoreWebView2BrowserDataKinds.ReadAloud |
                    CoreWebView2BrowserDataKinds.History |
                    CoreWebView2BrowserDataKinds.IndexedDb |
                    CoreWebView2BrowserDataKinds.LocalStorage |
                    CoreWebView2BrowserDataKinds.PasswordAutofill |
                    CoreWebView2BrowserDataKinds.OtherData;

                await _defaultEnvironment.ClearBrowsingDataAsync(dataKinds);
                MessageBox.Show("Datos de navegación (caché, cookies, etc.) borrados con éxito.", "Limpieza Completa", MessageBoxButton.OK, Image.Information);
            }
            else
            {
                MessageBox.Show("No se pudo acceder al motor del navegador para borrar los datos del perfil normal.", "Error de Limpieza", MessageBoxButton.OK, Image.Error);
            }
        }

        /// <summary>
        /// Nuevo: Manejador para suspender pestañas inactivas.
        /// Se invoca desde la ventana de configuración.
        /// </summary>
        private void SettingsWindow_OnSuspendInactiveTabs()
        {
            if (!_isTabSuspensionEnabled)
            {
                MessageBox.Show("La suspensión de pestañas no está habilitada en la configuración. Habilítela para usar esta función.", "Suspensión Deshabilitada", MessageBoxButton.OK, Image.Warning);
                return;
            }

            foreach (var tabItem in _browserTabs)
            {
                // No suspender la pestaña activa ni las pestañas incógnito (ya que su data no es persistente)
                if (tabItem.Tab != BrowserTabControl.SelectedItem && !tabItem.IsIncognito)
                {
                    // Un enfoque simple para "suspender": reemplazar el contenido con un mensaje y liberar el WebView2.
                    // Cuando el usuario vuelve a la pestaña, el WebView2 se recrea y se recarga la URL.
                    if (tabItem.WebView != null && tabItem.WebView.CoreWebView2 != null)
                    {
                        // Guardar la URL actual antes de desechar
                        string suspendedUrl = tabItem.WebView.Source.OriginalString;

                        // Desechar el WebView2 para liberar recursos
                        tabItem.WebView.Dispose();
                        tabItem.WebView = null; // Marcar como nulo para indicar que está suspendido

                        // Cambiar el contenido de la pestaña a una pantalla de "suspendido"
                        TextBlock suspendedMessage = new TextBlock
                        {
                            Text = $"Pestaña suspendida para ahorrar recursos.\nHaz clic para recargar: {suspendedUrl}",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            FontSize = 14,
                            Padding = new Thickness(20),
                            TextWrapping = TextWrapping.Wrap
                        };
                        tabItem.Tab.Content = suspendedMessage;
                        tabItem.Tab.Tag = suspendedUrl; // Guardar la URL en el Tag del TabItem para recargarla

                        // Cambiar el encabezado de la pestaña para indicar que está suspendida
                        string originalHeaderText = tabItem.HeaderTextBlock.Text;
                        if (!originalHeaderText.StartsWith("(Suspendida) ")) // Evitar duplicar el prefijo
                        {
                            tabItem.HeaderTextBlock.Text = "(Suspendida) " + originalHeaderText;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Carga las configuraciones de la aplicación (página de inicio, estado del bloqueador, motor de búsqueda, suspensión de pestañas) desde App.config.
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

            // Cargar la URL del motor de búsqueda predeterminado
            string savedSearchEngineUrl = ConfigurationManager.AppSettings[DefaultSearchEngineSettingKey];
            if (!string.IsNullOrEmpty(savedSearchEngineUrl))
            {
                _defaultSearchEngineUrl = savedSearchEngineUrl;
            }
            // Si no hay configuración guardada, se usará la _defaultSearchEngineUrl inicial ("https://www.google.com/search?q=")

            // Cargar el estado de la suspensión de pestañas
            string savedTabSuspensionState = ConfigurationManager.AppSettings[TabSuspensionSettingKey];
            if (bool.TryParse(savedTabSuspensionState, out bool isTabSuspensionEnabled))
            {
                _isTabSuspensionEnabled = isTabSuspensionEnabled;
            }
            else
            {
                _isTabSuspensionEnabled = false; // Por defecto deshabilitado
            }
        }

        /// <summary>
        /// Guarda las configuraciones actuales (página de inicio, estado del bloqueador, motor de búsqueda, suspensión de pestañas) en App.config.
        /// </summary>
        private void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Guardar página de inicio
            if (config.AppSettings.Settings[HomePageSettingKey] == null)
                config.AppSettings.Settings.Add(HomePageSettingKey, _defaultHomePage);
            else
                config.AppSettings.Settings[HomePageSettingKey].Value = _defaultHomePage;

            // Guardar estado del bloqueador de anuncios
            if (config.AppSettings.Settings[AdBlockerSettingKey] == null)
                config.AppSettings.Settings.Add(AdBlockerSettingKey, AdBlocker.IsEnabled.ToString());
            else
                config.AppSettings.Settings[AdBlockerSettingKey].Value = AdBlocker.IsEnabled.ToString();

            // Guardar URL del motor de búsqueda predeterminado
            if (config.AppSettings.Settings[DefaultSearchEngineSettingKey] == null)
                config.AppSettings.Settings.Add(DefaultSearchEngineSettingKey, _defaultSearchEngineUrl);
            else
                config.AppSettings.Settings[DefaultSearchEngineSettingKey].Value = _defaultSearchEngineUrl;

            // Guardar estado de la suspensión de pestañas
            if (config.AppSettings.Settings[TabSuspensionSettingKey] == null)
                config.AppSettings.Settings.Add(TabSuspensionSettingKey, _isTabSuspensionEnabled.ToString());
            else
                config.AppSettings.Settings[TabSuspensionSettingKey].Value = _isTabSuspensionEnabled.ToString();

            config.Save(ConfigurationSaveMode.Modified); // Guarda los cambios en el archivo de configuración
            ConfigurationManager.RefreshSection("appSettings"); // Refresca la sección para que los nuevos valores estén disponibles inmediatamente
        }

        /// <summary>
        /// Se ejecuta cuando la ventana se está cerrando. Limpia los recursos del entorno incógnito.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // Asegúrate de desechar los entornos de WebView2 al cerrar la aplicación
            // Esto es crucial para liberar recursos y limpiar el directorio temporal del modo incógnito.
            if (_incognitoEnvironment != null)
            {
                // El entorno incógnito crea una carpeta temporal. Al cerrarse, se limpia automáticamente.
                // Sin embargo, podemos asegurarnos de que no haya restos.
                string incognitoUserDataFolder = _incognitoEnvironment.UserDataFolder;
                _incognitoEnvironment = null; // Liberar la referencia para que GC pueda limpiar
                try
                {
                    if (Directory.Exists(incognitoUserDataFolder))
                    {
                        Directory.Delete(incognitoUserDataFolder, true); // Eliminar recursivamente la carpeta temporal
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al eliminar la carpeta de datos de incógnito: {ex.Message}");
                    // No es crítico si falla la limpieza de la carpeta, pero es bueno intentarlo.
                }
            }
            if (_defaultEnvironment != null)
            {
                // El entorno por defecto no necesita ser eliminado, sus datos son persistentes.
                _defaultEnvironment = null;
            }
        }

        /// <summary>
        /// Clase auxiliar para gestionar la información de cada pestaña del navegador.
        /// </summary>
        private class BrowserTabItem
        {
            public TabItem Tab { get; set; } // El control TabItem de WPF
            public WebView2 WebView { get; set; } // La instancia de WebView2 dentro de la pestaña (puede ser null si la pestaña está suspendida)
            public TextBlock HeaderTextBlock { get; set; } // El TextBlock que muestra el título en el encabezado de la pestaña
            public bool IsIncognito { get; set; } // Indica si la pestaña está en modo incógnito
        }
    }
}
