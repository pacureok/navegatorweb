
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
using System.Text.Json; // Necesario para JsonSerializer (para historial, marcadores y sesiones)
using System.Speech.Synthesis; // Necesario para Text-to-Speech
using System.Windows.Media.Imaging; // Necesario para BitmapFrame, PngBitmapEncoder
using System.Windows.Media; // Necesario para Brushes
using System.Diagnostics; // Necesario para Process
using System.Collections.ObjectModel; // Para ObservableCollection

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
        private const string TabSuspensionSettingKey = "TabSuspensionEnabled"; // Clave para el estado de la suspensión de pestañas
        private const string RestoreSessionSettingKey = "RestoreSessionOnStartup"; // Clave para la configuración de restaurar sesión
        private const string LastSessionUrlsSettingKey = "LastSessionUrls"; // Clave para guardar las URLs de la última sesión
        private const string TrackerProtectionSettingKey = "TrackerProtectionEnabled"; // Clave para el estado de la protección contra rastreadores
        private const string PdfViewerSettingKey = "PdfViewerEnabled"; // Clave para el estado del visor de PDF
        private const string UncleanShutdownFlagKey = "UncleanShutdown"; // Clave para detectar cierre inesperado

        private string _defaultSearchEngineUrl = "https://www.google.com/search?q="; // URL base del motor de búsqueda predeterminado
        private bool _isTabSuspensionEnabled = false; // Estado de la suspensión de pestañas
        private bool _restoreSessionOnStartup = true; // Estado de restaurar sesión al inicio (por defecto true)
        private bool _isPdfViewerEnabled = true; // Estado del visor de PDF (por defecto true)

        // NUEVO: Instancia del gestor de grupos de pestañas
        private TabGroupManager _tabGroupManager;

        // NUEVO: Propiedad para mantener la pestaña seleccionada globalmente para el Binding en XAML
        public BrowserTabItem SelectedTabItem
        {
            get { return (BrowserTabItem)GetValue(SelectedTabItemProperty); }
            set { SetValue(SelectedTabItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedTabItemProperty =
            DependencyProperty.Register("SelectedTabItem", typeof(BrowserTabItem), typeof(MainWindow), new PropertyMetadata(null));


        // Entornos de WebView2
        private CoreWebView2Environment _defaultEnvironment;
        private CoreWebView2Environment _incognitoEnvironment; // Para el modo incógnito

        // Contenido del script de modo lectura y modo oscuro
        private string _readerModeScript = string.Empty;
        private string _darkModeScript = string.Empty; // Contenido del script de modo oscuro

        // Instancia del sintetizador de voz
        private SpeechSynthesizer _speechSynthesizer;
        private bool _isReadingAloud = false;

        // Variables para la función Buscar en Página
        private bool _isFindBarVisible = false;
        private CoreWebView2FindInPage _findInPage;


        public MainWindow()
        {
            InitializeComponent();
            _tabGroupManager = new TabGroupManager();
            this.DataContext = this; // Establecer el DataContext de la ventana a sí misma para el Binding de TabGroups y SelectedTabItem

            LoadSettings(); // Cargar configuraciones al iniciar la aplicación
            InitializeEnvironments(); // Inicializar los entornos de WebView2
            LoadReaderModeScript(); // Cargar el script de modo lectura
            LoadDarkModeScript(); // Cargar el script de modo oscuro

            // Inicializar el sintetizador de voz
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice(); // Usar el dispositivo de audio predeterminado
            _speechSynthesizer.SpeakCompleted += SpeechSynthesizer_SpeakCompleted; // Manejar el evento de finalización
        }

        /// <summary>
        /// Maneja el evento cuando el sintetizador de voz termina de hablar.
        /// </summary>
        private void SpeechSynthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            _isReadingAloud = false;
            Dispatcher.Invoke(() => ReadAloudButton.Content = "🔊"); // Restaurar icono
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
                    MessageBox.Show("Advertencia: El archivo 'ReaderMode.js' no se encontró. El modo lectura no funcionará.", "Archivo Faltante", MessageBoxButton.OK, Image.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el script de modo lectura: {ex.Message}", "Error", MessageBoxButton.OK, Image.Error);
            }
        }

        /// <summary>
        /// Carga el contenido del archivo DarkMode.js.
        /// </summary>
        private void LoadDarkModeScript()
        {
            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DarkMode.js");
                if (File.Exists(scriptPath))
                {
                    _darkModeScript = File.ReadAllText(scriptPath);
                }
                else
                {
                    MessageBox.Show("Advertencia: El archivo 'DarkMode.js' no se encontró. El modo oscuro global no funcionará.", "Archivo Faltante", MessageBoxButton.OK, Image.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el script de modo oscuro: {ex.Message}", "Error", MessageBoxButton.OK, Image.Error);
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
                string defaultUserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AuroraBrowser", "UserData"); // Nombre de carpeta cambiado
                _defaultEnvironment = await CoreWebView2Environment.CreateAsync(null, defaultUserDataFolder);

                // Entorno para navegación incógnito (no persistente)
                // Se crea un directorio temporal que se eliminará al cerrar la aplicación.
                string incognitoUserDataFolder = Path.Combine(Path.GetTempPath(), "AuroraBrowserIncognito", Guid.NewGuid().ToString()); // Nombre de carpeta cambiado
                _incognitoEnvironment = await CoreWebView2Environment.CreateAsync(null, incognitoUserDataFolder, new CoreWebView2EnvironmentOptions {
                    IsCustomCrashReportingEnabled = false // Para modo incógnito, puedes deshabilitar crash reporting si quieres más privacidad
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar los entornos del navegador: {ex.Message}\nPor favor, asegúrate de tener WebView2 Runtime instalado.", "Error de Inicialización", MessageBoxButton.OK, Image.Error);
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
            string blockedDomainsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocked_domains.txt");
            AdBlocker.LoadBlockedDomainsFromFile(blockedDomainsFilePath);

            // Cargar dominios de rastreo desde un archivo (ej: "tracker_domains.txt")
            string trackerDomainsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tracker_domains.txt");
            TrackerBlocker.LoadBlockedTrackerDomainsFromFile(trackerDomainsFilePath);

            // NUEVO: Comprobar si hubo un cierre inesperado
            bool uncleanShutdown = false;
            if (ConfigurationManager.AppSettings[UncleanShutdownFlagKey] != null && bool.TryParse(ConfigurationManager.AppSettings[UncleanShutdownFlagKey], out bool flag))
            {
                uncleanShutdown = flag;
            }

            // Restablecer el flag de cierre inesperado inmediatamente
            UpdateUncleanShutdownFlag(true); // Establecerlo a true al inicio, si se cierra limpiamente, se pondrá a false

            // Restaurar sesión anterior si la configuración lo permite Y hubo un cierre inesperado
            if (_restoreSessionOnStartup && uncleanShutdown)
            {
                string savedUrlsJson = ConfigurationManager.AppSettings[LastSessionUrlsSettingKey];
                if (!string.IsNullOrEmpty(savedUrlsJson))
                {
                    try
                    {
                        List<string> savedUrls = JsonSerializer.Deserialize<List<string>>(savedUrlsJson);
                        if (savedUrls != null && savedUrls.Any())
                        {
                            // Mostrar la ventana de recuperación de fallos
                            CrashRecoveryWindow recoveryWindow = new CrashRecoveryWindow();
                            recoveryWindow.ShowDialog();

                            if (recoveryWindow.ShouldRestoreSession)
                            {
                                // Limpiar la pestaña inicial predeterminada si hay URLs guardadas
                                // Ahora se hace de forma diferente con los grupos
                                // Cerrar todas las pestañas existentes antes de restaurar
                                foreach (var group in _tabGroupManager.TabGroups.ToList()) // ToList para evitar modificar la colección mientras se itera
                                {
                                    foreach (var tabItem in group.TabsInGroup.ToList())
                                    {
                                        CloseBrowserTab(tabItem.Tab);
                                    }
                                    if (!group.TabsInGroup.Any()) // Si el grupo está vacío, eliminarlo
                                    {
                                        _tabGroupManager.RemoveGroup(group);
                                    }
                                }
                                // Asegurarse de que al menos un grupo exista si todos fueron eliminados
                                if (!_tabGroupManager.TabGroups.Any())
                                {
                                    _tabGroupManager.AddGroup("General");
                                }


                                foreach (string url in savedUrls)
                                {
                                    AddNewTab(url);
                                }
                            }
                            else
                            {
                                AddNewTab(_defaultHomePage); // Iniciar una nueva sesión
                            }
                        }
                        else
                        {
                            AddNewTab(_defaultHomePage); // No hay URLs guardadas, abre la página de inicio
                        }
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"Error al leer la sesión guardada: {ex.Message}. Se iniciará con la página de inicio.", "Error de Sesión", MessageBoxButton.OK, Image.Error);
                        AddNewTab(_defaultHomePage);
                    }
                }
                else
                {
                    AddNewTab(_defaultHomePage); // No hay sesión guardada, abre la página de inicio
                }
            }
            else
            {
                AddNewTab(_defaultHomePage); // La restauración de sesión está deshabilitada o no hubo cierre inesperado, abre la página de inicio
            }

            // Vincular los grupos de pestañas al ItemsControl
            TabGroupContainer.ItemsSource = _tabGroupManager.TabGroups;
        }

        /// <summary>
        /// Actualiza el flag de cierre inesperado en App.config.
        /// </summary>
        /// <param name="isUnclean">True si el cierre es inesperado, False si es limpio.</param>
        private void UpdateUncleanShutdownFlag(bool isUnclean)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[UncleanShutdownFlagKey] == null)
                config.AppSettings.Settings.Add(UncleanShutdownFlagKey, isUnclean.ToString());
            else
                config.AppSettings.Settings[UncleanShutdownFlagKey].Value = isUnclean.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }


        /// <summary>
        /// Agrega una nueva pestaña al navegador.
        /// </summary>
        /// <param name="url">URL opcional para cargar en la nueva pestaña. Si es nulo, usa la página de inicio predeterminada.</param>
        /// <param name="isIncognito">Indica si la nueva pestaña debe abrirse en modo incógnito.</param>
        /// <param name="targetGroup">Grupo al que añadir la pestaña. Si es nulo, se añade al grupo por defecto.</param>
        private async void AddNewTab(string url = null, bool isIncognito = false, TabGroup targetGroup = null)
        {
            // Esperar a que los entornos se inicialicen
            if (_defaultEnvironment == null || _incognitoEnvironment == null)
            {
                await System.Threading.Tasks.Task.Delay(100); // Pequeña espera si no están listos
                if (_defaultEnvironment == null || _incognitoEnvironment == null)
                {
                    MessageBox.Show("El navegador no está listo. Por favor, reinicia la aplicación.", "Error", MessageBoxButton.OK, Image.Error);
                    return;
                }
            }

            // Determinar el grupo objetivo
            TabGroup groupToAdd = targetGroup ?? _tabGroupManager.GetDefaultGroup();

            // Crear un nuevo TabItem (la pestaña visual)
            TabItem newTabItem = new TabItem();
            newTabItem.Name = "Tab" + (groupToAdd.TabsInGroup.Count + 1); // Nombre único para la pestaña dentro del grupo

            // Crear una nueva instancia de BrowserTabItem
            BrowserTabItem browserTab = new BrowserTabItem
            {
                Tab = newTabItem,
                IsIncognito = isIncognito, // Marcar si es incógnito
                IsSplit = false, // Inicialmente no está en modo dividido
                ParentGroup = groupToAdd // Asignar el grupo padre
            };
            // No añadir a _browserTabs directamente, ahora se gestiona por grupos.

            // Crear un panel para el encabezado de la pestaña (ahora usa propiedades de BrowserTabItem)
            DockPanel tabHeaderPanel = new DockPanel();
            browserTab.FaviconImage = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            browserTab.AudioIconImage = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            browserTab.HeaderTextBlock = new TextBlock { Text = "Cargando...", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };

            // Enlazar las propiedades de la UI a la instancia de BrowserTabItem
            browserTab.FaviconImage.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("FaviconSource") { Source = browserTab });
            // El icono de audio ya se obtiene de forma estática en XAML, aquí solo se enlaza la visibilidad
            browserTab.AudioIconImage.SetBinding(Image.VisibilityProperty, new System.Windows.Data.Binding("IsAudioPlaying") { Source = browserTab, Converter = (System.Windows.Data.IValueConverter)this.FindResource("BooleanToVisibilityConverter") });
            browserTab.AudioIconImage.MouseLeftButtonUp += AudioIcon_MouseLeftButtonUp; // Asignar evento al icono de audio

            if (isIncognito)
            {
                browserTab.HeaderTextBlock.Text = "(Incógnito) Cargando...";
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

            // Orden de los elementos en el encabezado
            DockPanel.SetDock(browserTab.FaviconImage, Dock.Left);
            DockPanel.SetDock(browserTab.AudioIconImage, Dock.Left);
            DockPanel.SetDock(browserTab.HeaderTextBlock, Dock.Left);
            DockPanel.SetDock(closeButton, Dock.Right);

            tabHeaderPanel.Children.Add(browserTab.FaviconImage);
            tabHeaderPanel.Children.Add(browserTab.AudioIconImage);
            tabHeaderPanel.Children.Add(browserTab.HeaderTextBlock);
            tabHeaderPanel.Children.Add(closeButton);
            newTabItem.Header = tabHeaderPanel; // Asignar el panel como encabezado de la pestaña

            // Crear la primera instancia de WebView2 para el contenido de la pestaña
            WebView2 webView1 = new WebView2();
            webView1.Source = new Uri(url ?? _defaultHomePage); // Cargar la URL especificada o la página de inicio
            webView1.Name = "WebView1_Tab" + (groupToAdd.TabsInGroup.Count + 1); // Nombre único
            webView1.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView1.VerticalAlignment = VerticalAlignment.Stretch;

            if (isIncognito)
            {
                webView1.CoreWebView2InitializationCompleted += (s, e) => ConfigureCoreWebView2(s as WebView2, e, _incognitoEnvironment);
            }
            else
            {
                webView1.CoreWebView2InitializationCompleted += (s, e) => ConfigureCoreWebView2(s as WebView2, e, _defaultEnvironment);
            }

            // Enlazar eventos comunes del WebView2 para esta pestaña
            webView1.Loaded += WebView_Loaded;
            webView1.NavigationStarting += WebView_NavigationStarting;
            webView1.SourceChanged += WebView_SourceChanged;
            webView1.NavigationCompleted += WebView_NavigationCompleted;
            webView1.CoreWebView2.DocumentTitleChanged += WebView_DocumentTitleChanged;
            webView1.CoreWebView2.FindInPageCompleted += CoreWebView2_FindInPageCompleted;
            webView1.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;
            webView1.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            webView1.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            webView1.CoreWebView2.FaviconChanged += CoreWebView2_FaviconChanged;
            webView1.CoreWebView2.IsAudioPlayingChanged += CoreWebView2_IsAudioPlayingChanged;
            webView1.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;


            // Contenido inicial de la pestaña: solo un WebView2 en un Grid
            Grid tabContent = new Grid();
            tabContent.Children.Add(webView1);
            newTabItem.Content = tabContent;

            // Añadir la nueva pestaña al grupo
            groupToAdd.TabsInGroup.Add(browserTab);
            browserTab.LeftWebView = webView1; // Este es el WebView principal por defecto

            // Seleccionar la nueva pestaña
            newTabItem.IsSelected = true;
            SelectedTabItem = browserTab; // Actualizar la propiedad de la ventana para el binding

            // Actualizar la barra de URL para reflejar la URL de la nueva pestaña activa
            UpdateUrlTextBoxFromCurrentTab();

            // Sugerir suspensión de pestañas si hay demasiadas
            CheckAndSuggestTabSuspension();
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
        private void ConfigureCoreWebView2(WebView2 currentWebView, CoreWebView2InitializationCompletedEventArgs e, CoreWebView2Environment environment)
        {
            if (currentWebView != null && e.IsSuccess)
            {
                currentWebView.CoreWebView2.Environment.SetCustomFileExtensions(new[] { ".pdf", ".docx", ".xlsx" }); // Ejemplo

                // Desvincular eventos antes de (posiblemente) re-adjuntar para evitar duplicados
                currentWebView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
                currentWebView.CoreWebView2.DownloadStarting -= CoreWebView2_DownloadStarting;
                currentWebView.CoreWebView2.DocumentTitleChanged -= WebView_DocumentTitleChanged;
                currentWebView.CoreWebView2.SourceChanged -= WebView_SourceChanged;
                currentWebView.CoreWebView2.NavigationCompleted -= WebView_NavigationCompleted;
                currentWebView.CoreWebView2.NavigationStarting -= WebView_NavigationStarting;
                currentWebView.CoreWebView2.FindInPageCompleted -= CoreWebView2_FindInPageCompleted;
                currentWebView.CoreWebView2.PermissionRequested -= CoreWebView2_PermissionRequested;
                currentWebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                currentWebView.CoreWebView2.DOMContentLoaded -= CoreWebView2_DOMContentLoaded;
                currentWebView.CoreWebView2.FaviconChanged -= CoreWebView2_FaviconChanged;
                currentWebView.CoreWebView2.IsAudioPlayingChanged -= CoreWebView2_IsAudioPlayingChanged;
                currentWebView.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed;


                // Adjuntar el manejador de eventos para interceptar solicitudes de red (bloqueador de anuncios y rastreadores).
                currentWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

                // Habilita las herramientas de desarrollador (F12)
                currentWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                // Habilitar zoom y pinch-zoom para PDFs
                currentWebView.CoreWebView2.Settings.IsPinchZoomEnabled = true;
                currentWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;


                // Suscribirse al evento DownloadStarting
                currentWebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

                // Re-adjuntar eventos comunes para este WebView2
                currentWebView.CoreWebView2.DocumentTitleChanged += WebView_DocumentTitleChanged;
                currentWebView.CoreWebView2.SourceChanged += WebView_SourceChanged;
                currentWebView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
                currentWebView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
                currentWebView.CoreWebView2.FindInPageCompleted += CoreWebView2_FindInPageCompleted;
                currentWebView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;
                currentWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                currentWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                currentWebView.CoreWebView2.FaviconChanged += CoreWebView2_FaviconChanged;
                currentWebView.CoreWebView2.IsAudioPlayingChanged += CoreWebView2_IsAudioPlayingChanged;
                currentWebView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
            }
        }


        /// <summary>
        /// Intercepta las solicitudes de recursos web para implementar el bloqueador de anuncios y rastreadores.
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
                return; // Importante: si ya se bloqueó por anuncios, no seguir comprobando
            }

            // Si la protección contra rastreadores está habilitada y la URL es un rastreador, cancela la solicitud.
            if (TrackerBlocker.IsEnabled && TrackerBlocker.IsBlocked(e.Request.Uri))
            {
                e.Response = ((WebView2)sender).CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Forbidden", "Content-Type: text/plain\nAccess-Control-Allow-Origin: *"
                );
                return;
            }
        }

        /// <summary>
        /// Maneja el inicio de una descarga desde WebView2.
        /// Permite al usuario elegir la ruta de guardado y actualiza el gestor de descargas.
        /// </summary>
        private async void CoreWebView2_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            // Si el visor de PDF está habilitado y es un PDF, abrirlo en el visor en lugar de descargar.
            if (_isPdfViewerEnabled && e.DownloadOperation.Uri.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                e.Handled = true; // Indicar que manejaremos la descarga
                // Abrir el PDF en la ventana del visor de PDF
                PdfViewerWindow pdfViewer = new PdfViewerWindow(e.DownloadOperation.Uri, _defaultEnvironment);
                pdfViewer.Show();
                return; // Salir de la función, ya no necesitamos procesar la descarga
            }

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
                        MessageBox.Show($"Descarga de '{newDownload.FileName}' ha {newDownload.State}.", "Descarga Finalizada", MessageBoxButton.OK, Image.Information);
                    }
                    // Actualizar la UI del gestor de descargas (si está abierta)
                    DownloadManager.AddOrUpdateDownload(newDownload);
                };
            }
            else
            {
                // Si el usuario cancela el diálogo de guardado, cancelar la descarga
                e.Cancel = true;
                MessageBox.Show("Descarga cancelada por el usuario.", "Descarga Cancelada", MessageBoxButton.OK, Image.Information);
            }
        }

        /// <summary>
        /// Maneja las solicitudes de permisos de sitios web (ej. cámara, micrófono).
        /// </summary>
        private void CoreWebView2_PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            // Mostrar un MessageBox para preguntar al usuario
            MessageBoxResult result = MessageBox.Show(
                $"El sitio web '{e.Uri}' solicita permiso para usar: {e.PermissionKind}.\n¿Deseas permitirlo?",
                "Solicitud de Permiso",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                e.State = CoreWebView2PermissionState.Allow; // Permitir el permiso
            }
            else
            {
                e.State = CoreWebView2PermissionState.Deny; // Denegar el permiso
            }
        }

        /// <summary>
        /// Se ejecuta cuando la URL de un WebView2 cambia.
        /// Actualiza la barra de dirección si es la pestaña activa y el WebView principal.
        /// </summary>
        private void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            var browserTab = GetBrowserTabItemFromWebView(currentWebView); // Obtener la pestaña asociada

            // Solo actualiza la barra de dirección si este WebView es el principal (LeftWebView) de la pestaña seleccionada
            if (browserTab != null && SelectedTabItem == browserTab) // Comparar con SelectedTabItem
            {
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
            var browserTab = GetBrowserTabItemFromWebView(currentWebView);

            if (browserTab != null && SelectedTabItem == browserTab) // Comparar con SelectedTabItem
            {
                if (!e.IsSuccess)
                {
                    // Mostrar página de error personalizada para errores HTTP
                    if (e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationAborted) // Ignorar si fue abortada (ej. por navegación a PDF)
                    {
                        string errorPagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomErrorPage.html");
                        if (File.Exists(errorPagePath))
                        {
                            currentWebView.CoreWebView2.Navigate($"file:///{errorPagePath.Replace("\\", "/")}");
                            // Opcional: Pasar el mensaje de error a la página HTML si es posible
                            // await currentWebView.CoreWebView2.ExecuteScriptAsync($"document.getElementById('errorMessage').innerText = 'Error: {e.WebErrorStatus}';");
                        }
                        else
                        {
                            MessageBox.Show($"La navegación a {currentWebView.CoreWebView2.Source} falló con el código de error {e.WebErrorStatus}", "Error de Navegación", MessageBoxButton.OK, Image.Error);
                        }
                    }
                }
                else
                {
                    // Añadir la página al historial SOLO SI NO ES UNA PESTAÑA INCÓGNITO y es el WebView principal
                    if (!browserTab.IsIncognito && browserTab.LeftWebView == currentWebView)
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

            // Si el visor de PDF está habilitado y la URL es un PDF, abrirlo en el visor en lugar de navegar en la pestaña actual.
            if (_isPdfViewerEnabled && e.Uri.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true; // Cancelar la navegación en la pestaña actual
                PdfViewerWindow pdfViewer = new PdfViewerWindow(e.Uri, _defaultEnvironment);
                pdfViewer.Show();
                return; // Salir de la función, ya no necesitamos navegar en esta pestaña
            }
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
                var browserTab = GetBrowserTabItemFromWebView(currentWebView);
                if (browserTab != null)
                {
                    // Si es el WebView izquierdo (principal), actualiza el encabezado de la pestaña.
                    if (browserTab.LeftWebView == currentWebView)
                    {
                        string title = currentWebView.CoreWebView2.DocumentTitle;
                        if (browserTab.IsIncognito)
                        {
                            browserTab.HeaderTextBlock.Text = "(Incógnito) " + title;
                        }
                        else
                        {
                            browserTab.HeaderTextBlock.Text = title;
                        }
                    }
                }

                // Si es la pestaña activa y es el WebView principal, actualiza también el título de la ventana principal.
                if (SelectedTabItem == browserTab && browserTab.LeftWebView == currentWebView) // Comparar con SelectedTabItem
                {
                    this.Title = currentWebView.CoreWebView2.DocumentTitle + " - Aurora Browser"; // Título cambiado a Aurora Browser
                }
            }
        }

        /// <summary>
        /// NUEVO: Maneja el cambio del favicon de una página.
        /// </summary>
        private async void CoreWebView2_FaviconChanged(object sender, object e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            var browserTab = GetBrowserTabItemFromWebView(currentWebView);
            if (browserTab == null) return;

            try
            {
                using (var stream = await currentWebView.CoreWebView2.GetFaviconAsync())
                {
                    if (stream != null && stream.Length > 0)
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Congelar para que pueda ser accedido desde otros hilos si es necesario
                        browserTab.FaviconSource = bitmap;
                    }
                    else
                    {
                        browserTab.FaviconSource = browserTab.GetDefaultGlobeIcon(); // Establecer el icono de globo por defecto
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener favicon: {ex.Message}");
                browserTab.FaviconSource = browserTab.GetDefaultGlobeIcon(); // Establecer el icono de globo por defecto en caso de error
            }
        }

        /// <summary>
        /// NUEVO: Maneja el cambio en el estado de reproducción de audio de una página.
        /// </summary>
        private void CoreWebView2_IsAudioPlayingChanged(object sender, object e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            var browserTab = GetBrowserTabItemFromWebView(currentWebView);
            if (browserTab == null) return;

            browserTab.IsAudioPlaying = currentWebView.CoreWebView2.IsAudioPlaying;
        }

        /// <summary>
        /// NUEVO: Maneja el clic en el icono de audio para silenciar/reactivar.
        /// </summary>
        private void AudioIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image audioIcon = sender as Image;
            if (audioIcon == null) return;

            // Encontrar la pestaña asociada a este icono
            // Se asume que el DataContext del Image es el BrowserTabItem
            BrowserTabItem browserTab = audioIcon.DataContext as BrowserTabItem;
            if (browserTab == null || browserTab.LeftWebView == null || browserTab.LeftWebView.CoreWebView2 == null) return;

            // Alternar el estado de silencio
            browserTab.LeftWebView.CoreWebView2.IsMuted = !browserTab.LeftWebView.CoreWebView2.IsMuted;
            // La propiedad IsAudioPlayingChanged se encargará de actualizar el icono si el audio está realmente reproduciéndose.
            // Aquí solo actualizamos el ToolTip para reflejar la acción.
            audioIcon.ToolTip = browserTab.LeftWebView.CoreWebView2.IsMuted ? "Audio silenciado (clic para reactivar)" : "Reproduciendo audio (clic para silenciar/reactivar)";
        }


        /// <summary>
        /// NUEVO: Maneja los fallos del proceso de WebView2 (ej. página colgada).
        /// </summary>
        private void CoreWebView2_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            WebView2 failedWebView = sender as WebView2;
            if (failedWebView == null) return;

            var browserTab = GetBrowserTabItemFromWebView(failedWebView);
            if (browserTab == null) return;

            string message = $"El proceso de la página '{failedWebView.Source}' ha fallado.\n" +
                             $"Tipo de fallo: {e.ProcessFailedKind}\n" +
                             $"Estado del error: {e.Reason}";

            MessageBoxResult result = MessageBox.Show(message + "\n\n¿Deseas recargar la página?",
                                                      "Página No Responde",
                                                      MessageBoxButton.YesNo,
                                                      MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                failedWebView.CoreWebView2.Reload();
            }
            else
            {
                // Navegar a una página de error local
                string errorPagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomErrorPage.html");
                if (File.Exists(errorPagePath))
                {
                    failedWebView.CoreWebView2.Navigate($"file:///{errorPagePath.Replace("\\", "/")}");
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
            WebView2 currentWebView = GetCurrentWebView(); // Obtiene el WebView principal de la pestaña activa
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una pestaña activa o el navegador no está listo.", "Error", MessageBoxButton.OK, Image.Error);
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
                MessageBox.Show($"Error al navegar: {ex.Message}", "Error", MessageBoxButton.OK, Image.Error);
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
        /// Recarga la página actual en la pestaña activa (panel izquierdo si está dividido).
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
        /// Navega a la página de inicio predeterminada en la pestaña activa (panel izquierdo si está dividido).
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
            AddNewTab(); // Abre una pestaña normal por defecto en el grupo por defecto
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
                var browserTab = SelectedTabItem; // Obtener la pestaña seleccionada
                if (browserTab != null && browserTab.IsIncognito)
                {
                    MessageBox.Show("No se pueden añadir marcadores en modo incógnito.", "Error al Añadir Marcador", MessageBoxButton.OK, Image.Warning);
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
                    MessageBox.Show("No se pudo añadir la página a marcadores. Asegúrate de que la página esté cargada y tenga un título.", "Error al Añadir Marcador", MessageBoxButton.OK, Image.Warning);
                }
            }
            else
            {
                MessageBox.Show("No hay una página activa para añadir a marcadores.", "Error al Añadir Marcador", MessageBoxButton.OK, Image.Error);
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
        /// Maneja el clic en el botón "Modo Lectura". Inyecta el script para activar/desactivar el modo lectura.
        /// </summary>
        private async void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para aplicar el modo lectura.", "Error de Modo Lectura", MessageBoxButton.OK, Image.Error);
                return;
            }

            if (!string.IsNullOrEmpty(_readerModeScript))
            {
                try
                {
                    // Inyectar el script en la página actual
                    await currentWebView.CoreWebView2.ExecuteScriptAsync(_readerModeScript);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al aplicar el modo lectura: {ex.Message}", "Error de Modo Lectura", MessageBoxButton.OK, Image.Error);
                }
            }
            else
            {
                MessageBox.Show("Advertencia: El archivo 'ReaderMode.js' no se encontró. El modo lectura no funcionará.", "Archivo Faltante", MessageBoxButton.OK, Image.Warning);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Leer en Voz Alta". Inicia o detiene la lectura del contenido de la página.
        /// </summary>
        private async void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll(); // Detener toda la lectura en curso
                _isReadingAloud = false;
                ReadAloudButton.Content = "🔊"; // Restaurar icono
                return;
            }

            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para leer en voz alta.", "Leer en Voz Alta", MessageBoxButton.OK, Image.Error);
                return;
            }

            try
            {
                // JavaScript para extraer el texto principal de la página
                string script = @"
                        (function() {
                            let text = '';
                            let mainContent = document.querySelector('article, main, .post-content, .entry-content, #content, #main');

                            if (mainContent) {
                                text = mainContent.innerText || mainContent.textContent;
                            } else {
                                // Fallback a todo el cuerpo si no se encuentra contenido principal
                                text = document.body.innerText || document.body.textContent;
                            }

                            // Limpiar el texto (eliminar espacios en blanco excesivos, saltos de línea)
                            text = text.replace(/(\r\n|\n|\r)/gm, ' ').replace(/\s+/g, ' ').trim();

                            return text;
                        })();
                    ";
                string pageText = await currentWebView.CoreWebView2.ExecuteScriptAsync(script);

                // El resultado de ExecuteScriptAsync viene como una cadena JSON (con comillas si es string)
                // Necesitamos deserializarla para obtener el valor real de la cadena.
                pageText = System.Text.Json.JsonSerializer.Deserialize<string>(pageText);

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    _speechSynthesizer.SpeakAsync(pageText);
                    _isReadingAloud = true;
                    ReadAloudButton.Content = "⏸️"; // Cambiar icono a pausa
                }
                else
                {
                    MessageBox.Show("No se encontró texto legible en la página actual.", "Leer en Voz Alta", MessageBoxButton.OK, Image.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al leer en voz alta: {ex.Message}", "Error de Lectura", MessageBoxButton.OK, Image.Error);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Pantalla Dividida". Alterna el modo de pantalla dividida para la pestaña actual.
        /// </summary>
        private async void SplitScreenButton_Click(object sender, RoutedEventArgs e)
        {
            var currentTab = SelectedTabItem; // Usar SelectedTabItem
            if (currentTab == null || currentTab.LeftWebView == null || currentTab.LeftWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una pestaña activa o el navegador no está listo.", "Error", MessageBoxButton.OK, Image.Error);
                return;
            }

            if (currentTab.IsSplit)
            {
                // Desactivar modo dividido
                DisableSplitScreenForCurrentTab(currentTab);
                SplitScreenButton.Content = "↔️"; // Restaurar icono
            }
            else
            {
                // Activar modo dividido
                await EnableSplitScreenForCurrentTab(currentTab, _defaultHomePage); // Cargar página de inicio por defecto
                SplitScreenButton.Content = "➡️"; // Cambiar icono a "derecha" (indicando que está dividido)
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "IA". Activa la pantalla dividida y carga Gemini en el panel derecho.
        /// </summary>
        private async void AIButton_Click(object sender, RoutedEventArgs e)
        {
            var currentTab = SelectedTabItem; // Usar SelectedTabItem
            if (currentTab == null || currentTab.LeftWebView == null || currentTab.LeftWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una pestaña activa o el navegador no está listo.", "Error", MessageBoxButton.OK, Image.Error);
                return;
            }

            // Si no está en modo dividido, activarlo primero
            if (!currentTab.IsSplit)
            {
                await EnableSplitScreenForCurrentTab(currentTab, "https://gemini.google.com/");
                SplitScreenButton.Content = "➡️"; // Asegurar que el icono de pantalla dividida se actualice
            }
            else
            {
                // Si ya está en modo dividido, simplemente navegar el panel derecho a Gemini
                if (currentTab.RightWebView != null && currentTab.RightWebView.CoreWebView2 != null)
                {
                    currentTab.RightWebView.CoreWebView2.Navigate("https://gemini.google.com/");
                }
                else
                {
                    // Esto no debería pasar si IsSplit es true, pero como fallback
                    await EnableSplitScreenForCurrentTab(currentTab, "https://gemini.google.com/");
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Captura de Pantalla". Captura la vista actual del WebView2.
        /// </summary>
        private async void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView(); // Obtiene el WebView principal de la pestaña activa
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para capturar.", "Error de Captura", MessageBoxButton.OK, Image.Error);
                return;
            }

            try
            {
                // Crear un SaveFileDialog para que el usuario elija dónde guardar la imagen
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Captura_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    DefaultExt = ".png",
                    Filter = "Archivos PNG (*.png)|*.png|Todos los archivos (*.*)|*.*",
                    Title = "Guardar Captura de Pantalla"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    // Capturar la vista del WebView2 en un MemoryStream
                    using (MemoryStream stream = new MemoryStream())
                    {
                        await currentWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);

                        // Guardar el stream en el archivo elegido
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            stream.WriteTo(fileStream);
                        }
                    }
                    MessageBox.Show($"Captura de pantalla guardada en:\n{filePath}", "Captura Exitosa", MessageBoxButton.OK, Image.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al realizar la captura de pantalla: {ex.Message}", "Error de Captura", MessageBoxButton.OK, Image.Error);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Administrador de Pestañas". Abre la ventana del administrador.
        /// </summary>
        private void TabManagerButton_Click(object sender, RoutedEventArgs e)
        {
            // Pasamos delegados para que la ventana del administrador pueda obtener y cerrar pestañas
            // Ahora se pasa la lista de todos los BrowserTabItem de todos los grupos
            TabManagerWindow tabManagerWindow = new TabManagerWindow(() => _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).ToList(), CloseBrowserTab, GetCurrentBrowserTabItemInternal);
            tabManagerWindow.Show(); // Mostrar la ventana (no modal)
        }

        /// <summary>
        /// Maneja el clic en el botón "Extractor de Datos Web". Abre la ventana de extracción de datos.
        /// </summary>
        private void DataExtractionButton_Click(object sender, RoutedEventArgs e)
        {
            // Pasamos un delegado para que la ventana de extracción pueda obtener el WebView2 actual
            DataExtractionWindow dataExtractionWindow = new DataExtractionWindow(GetCurrentWebView);
            dataExtractionWindow.Show(); // Mostrar la ventana (no modal)
        }

        /// <summary>
        /// Maneja el clic en el botón "Modo Oscuro Global". Inyecta el script para activar/desactivar el modo oscuro.
        /// </summary>
        private async void DarkModeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para aplicar el modo oscuro.", "Error de Modo Oscuro", MessageBoxButton.OK, Image.Error);
                return;
            }

            if (!string.IsNullOrEmpty(_darkModeScript))
            {
                try
                {
                    // Inyectar el script en la página actual
                    await currentWebView.CoreWebView2.ExecuteScriptAsync(_darkModeScript);

                    // Opcional: Cambiar el icono del botón para indicar el estado
                    // Esto requeriría que el JavaScript devuelva el estado actual, o que el C# lo gestione.
                    // Por simplicidad, el JS se encarga de la lógica de toggle.
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al aplicar el modo oscuro: {ex.Message}", "Error de Modo Oscuro", MessageBoxButton.OK, Image.Error);
                }
            }
            else
            {
                MessageBox.Show("Advertencia: El script de modo oscuro no está cargado. Asegúrate de que 'DarkMode.js' exista.", "Error de Modo Oscuro", MessageBoxButton.OK, Image.Warning);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Monitor de Rendimiento". Abre la ventana del monitor.
        /// </summary>
        private void PerformanceMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            // Pasamos un delegado para que la ventana del monitor pueda obtener la lista de pestañas
            // Ahora se pasa la lista de todos los BrowserTabItem de todos los grupos
            PerformanceMonitorWindow monitorWindow = new PerformanceMonitorWindow(() => _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).ToList());
            monitorWindow.Show(); // Mostrar la ventana (no modal)
        }

        /// <summary>
        /// Maneja el clic en el botón "Buscar en Página". Muestra u oculta la barra de búsqueda.
        /// </summary>
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            // Alternar la visibilidad de la barra de búsqueda
            _isFindBarVisible = !_isFindBarVisible;
            FindBar.Visibility = _isFindBarVisible ? Visibility.Visible : Visibility.Collapsed;

            if (_isFindBarVisible)
            {
                FindTextBox.Focus(); // Poner el foco en el campo de texto de búsqueda
                // Limpiar resultados anteriores al abrir la barra
                FindResultsTextBlock.Text = "0/0";
                ClearFindResults(); // Limpiar el resaltado anterior
            }
            else
            {
                ClearFindResults(); // Limpiar el resaltado al cerrar la barra
            }
        }

        /// <summary>
        /// Maneja el cambio de texto en el cuadro de búsqueda. Inicia una nueva búsqueda.
        /// </summary>
        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformFindInPage(FindTextBox.Text);
        }

        /// <summary>
        /// Maneja la pulsación de tecla Enter en el cuadro de búsqueda.
        /// </summary>
        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformFindInPage(FindTextBox.Text, CoreWebView2FindInPageKind.Next); // Buscar siguiente al presionar Enter
            }
        }

        /// <summary>
        /// Realiza la búsqueda en la página actual.
        /// </summary>
        /// <param name="searchText">El texto a buscar.</param>
        /// <param name="findKind">El tipo de búsqueda (siguiente, anterior, etc.).</param>
        private async void PerformFindInPage(string searchText, CoreWebView2FindInPageKind findKind = CoreWebView2FindInPageKind.None)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null || string.IsNullOrWhiteSpace(searchText))
            {
                FindResultsTextBlock.Text = "0/0";
                ClearFindResults();
                return;
            }

            // Si es una nueva búsqueda o el texto ha cambiado, reiniciamos la búsqueda
            if (_findInPage == null || _findInPage.SearchText != searchText || findKind == CoreWebView2FindInPageKind.None)
            {
                // Iniciar una nueva búsqueda
                _findInPage = currentWebView.CoreWebView2.FindInPage(searchText, CoreWebView2FindInPageKind.None);
            }
            else
            {
                // Continuar la búsqueda
                _findInPage = currentWebView.CoreWebView2.FindInPage(searchText, findKind);
            }
        }

        /// <summary>
        /// Maneja el evento de finalización de búsqueda en página.
        /// </summary>
        private void CoreWebView2_FindInPageCompleted(object sender, CoreWebView2FindInPageCompletedEventArgs e)
        {
            // Actualizar la interfaz de usuario con los resultados de la búsqueda
            FindResultsTextBlock.Text = $"{e.ActiveMatchIndex + 1}/{e.Matches}"; // +1 porque ActiveMatchIndex es base 0
            if (e.Matches == 0)
            {
                FindResultsTextBlock.Text = "0/0"; // No hay coincidencias
            }
        }

        /// <summary>
        /// Limpia el resaltado de búsqueda en la página.
        /// </summary>
        private void ClearFindResults()
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.FindInPage(string.Empty, CoreWebView2FindInPageKind.None); // Buscar cadena vacía para limpiar
                FindResultsTextBlock.Text = "0/0";
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Siguiente" de la búsqueda.
        /// </summary>
        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFindInPage(FindTextBox.Text, CoreWebView2FindInPageKind.Next);
        }

        /// <summary>
        /// Maneja el clic en el botón "Anterior" de la búsqueda.
        /// </summary>
        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFindInPage(FindTextBox.Text, CoreWebView2FindInPageKind.Previous);
        }

        /// <summary>
        /// Maneja el clic en el botón "Cerrar" de la barra de búsqueda.
        /// </summary>
        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            _isFindBarVisible = false;
            FindBar.Visibility = Visibility.Collapsed;
            ClearFindResults();
        }

        /// <summary>
        /// Maneja el clic en el botón "Gestor de Permisos". Abre la ventana del gestor de permisos.
        /// </summary>
        private void PermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Pasamos un delegado para que la ventana de permisos pueda obtener el entorno de WebView2
            PermissionsManagerWindow permissionsWindow = new PermissionsManagerWindow(GetDefaultEnvironment);
            permissionsWindow.Show(); // Mostrar la ventana (no modal)
        }

        /// <summary>
        /// Maneja el clic en el botón "Picture-in-Picture (PIP)". Intenta extraer un video a una ventana flotante.
        /// </summary>
        private async void PipButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para extraer un video.", "Error de PIP", MessageBoxButton.OK, Image.Error);
                return;
            }

            try
            {
                // JavaScript para encontrar el primer elemento <video> visible y obtener su src
                // Esto es una simplificación; un enfoque más robusto podría buscar reproductores específicos (YouTube, Vimeo)
                // y extraer su URL de video o URL de embebido.
                string script = @"
                    (function() {
                        let video = document.querySelector('video');
                        if (video && video.src) {
                            // Pausar el video original
                            video.pause();
                            return video.src;
                        }
                        // Intenta buscar si es un iframe de YouTube o similar que contenga el video
                        let youtubeIframe = document.querySelector('iframe[src*=""youtube.com/embed""]');
                        if (youtubeIframe && youtubeIframe.src) {
                            return youtubeIframe.src;
                        }
                        // Fallback para videos de YouTube incrustados en un iframe con watch?v=
                        let youtubeWatchIframe = document.querySelector('iframe[src*=""youtube.com/watch?v=""]');
                        if (youtubeWatchIframe && youtubeWatchIframe.src) {
                            return youtubeWatchIframe.src;
                        }
                        return null;
                    })();
                ";
                string videoUrlJson = await currentWebView.CoreWebView2.ExecuteScriptAsync(script);
                string videoUrl = System.Text.Json.JsonSerializer.Deserialize<string>(videoUrlJson);

                if (!string.IsNullOrEmpty(videoUrl))
                {
                    // Abrir la ventana PIP con la URL del video
                    PipWindow pipWindow = new PipWindow(videoUrl, currentWebView);
                    pipWindow.Show();
                }
                else
                {
                    MessageBox.Show("No se encontró ningún video reproducible en la página actual.", "Video no Encontrado", MessageBoxButton.OK, Image.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al intentar el modo Picture-in-Picture: {ex.Message}", "Error de PIP", MessageBoxButton.OK, Image.Error);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Gestor de Contraseñas". Abre la ventana del gestor.
        /// </summary>
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            PasswordManagerWindow passwordWindow = new PasswordManagerWindow();
            passwordWindow.ShowDialog(); // Mostrar como diálogo modal
        }

        /// <summary>
        /// NUEVO: Maneja el clic en el botón "Resaltar Texto (Extensión)".
        /// Inyecta un script para resaltar texto en la página actual.
        /// </summary>
        private async void HighlightButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para ejecutar la extensión.", "Error de Extensión", MessageBoxButton.OK, Image.Error);
                return;
            }

            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HighlighterExtension.js");
                if (File.Exists(scriptPath))
                {
                    string scriptContent = File.ReadAllText(scriptPath);
                    await currentWebView.CoreWebView2.ExecuteScriptAsync(scriptContent);
                    MessageBox.Show("Extensión de resaltado ejecutada. Puede que necesites hacer clic de nuevo para deshabilitar el resaltado.", "Extensión", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("El script 'HighlighterExtension.js' no se encontró. Asegúrate de que el archivo exista.", "Error de Extensión", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al ejecutar la extensión de resaltado: {ex.Message}", "Error de Extensión", MessageBoxButton.OK, Image.Error);
            }
        }


        /// <summary>
        /// Se ejecuta cuando la página ha terminado de cargar su DOM.
        /// Aquí inyectamos el script para detectar formularios de inicio de sesión y autocompletar.
        /// </summary>
        private async void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            // No autocompletar/guardar en modo incógnito
            var browserTab = GetBrowserTabItemFromWebView(currentWebView);
            if (browserTab != null && browserTab.IsIncognito) return;

            string currentUrl = currentWebView.CoreWebView2.Source;
            string username = null;
            string password = null;

            // 1. Intentar autocompletar si hay credenciales guardadas
            // Para simplificar, solo buscamos la primera credencial para el dominio.
            // Una implementación más avanzada podría permitir al usuario elegir entre múltiples credenciales.
            var allPasswords = PasswordManager.GetAllPasswords();
            var matchingEntry = allPasswords.FirstOrDefault(p =>
                new Uri(p.Url).Host.Equals(new Uri(currentUrl).Host, StringComparison.OrdinalIgnoreCase));

            if (matchingEntry != null)
            {
                username = matchingEntry.Username;
                password = PasswordManager.DecryptPassword(matchingEntry.EncryptedPassword);

                // Inyectar JavaScript para rellenar los campos del formulario
                string autofillScript = $@"
                    (function() {{
                        let usernameFields = document.querySelectorAll('input[type=""text""], input[type=""email""]');
                        let passwordFields = document.querySelectorAll('input[type=""password""]');

                        if (usernameFields.length > 0 && passwordFields.length > 0) {{
                            usernameFields[0].value = '{username}';
                            passwordFields[0].value = '{password}';
                            // Opcional: enfocar el campo de contraseña o el botón de submit
                            // passwordFields[0].focus();
                        }}
                    }})();
                ";
                await currentWebView.CoreWebView2.ExecuteScriptAsync(autofillScript);
            }

            // 2. Inyectar script para monitorear envíos de formularios y enviar credenciales a C#
            string scriptToInject = @"
                (function() {
                    document.querySelectorAll('form').forEach(form => {
                        form.addEventListener('submit', (event) => {
                            let usernameInput = form.querySelector('input[type=""text""], input[type=""email""]');
                            let passwordInput = form.querySelector('input[type=""password""]');

                            if (usernameInput && passwordInput && usernameInput.value && passwordInput.value) {
                                // Enviar las credenciales a la aplicación C#
                                window.chrome.webview.postMessage({
                                    type: 'loginSubmit',
                                    url: window.location.href,
                                    username: usernameInput.value,
                                    password: passwordInput.value
                                });
                            }
                        });
                    });
                })();
            ";
            await currentWebView.CoreWebView2.ExecuteScriptAsync(scriptToInject);
        }

        /// <summary>
        /// Maneja los mensajes recibidos desde JavaScript (ej. credenciales de login).
        /// </summary>
        private async void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            // No guardar en modo incógnito
            var browserTab = GetBrowserTabItemFromWebView(currentWebView);
            if (browserTab != null && browserTab.IsIncognito) return;

            string message = e.WebMessageAsJson;
            try
            {
                // Deserializar el mensaje JSON
                using (JsonDocument doc = JsonDocument.Parse(message))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("type", out JsonElement typeElement) && typeElement.GetString() == "loginSubmit")
                    {
                        string url = root.GetProperty("url").GetString();
                        string username = root.GetProperty("username").GetString();
                        string password = root.GetProperty("password").GetString();

                        // Preguntar al usuario si desea guardar la contraseña
                        MessageBoxResult result = MessageBox.Show(
                            $"¿Deseas guardar la contraseña para el usuario '{username}' en '{new Uri(url).Host}'?",
                            "Guardar Contraseña",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                        if (result == MessageBoxResult.Yes)
                        {
                            PasswordManager.AddOrUpdatePassword(url, username, password);
                            MessageBox.Show("Contraseña guardada con éxito.", "Contraseña Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Esto puede ocurrir si el mensaje JSON no tiene el formato esperado
                Debug.WriteLine($"Error al procesar mensaje web: {ex.Message}");
            }
        }


        /// <summary>
        /// Método público para que el Gestor de Permisos pueda obtener el entorno predeterminado.
        /// </summary>
        /// <returns>El CoreWebView2Environment predeterminado.</returns>
        public CoreWebView2Environment GetDefaultEnvironment()
        {
            return _defaultEnvironment;
        }


        /// <summary>
        /// Método público para que el Administrador de Pestañas y el Monitor de Rendimiento puedan obtener la lista de todas las pestañas.
        /// </summary>
        /// <returns>Una lista de BrowserTabItem.</returns>
        // Este método ahora devuelve todas las pestañas de todos los grupos.
        public List<BrowserTabItem> GetBrowserTabItems()
        {
            return _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).ToList();
        }

        /// <summary>
        /// Método público para que el Administrador de Pestañas pueda cerrar una pestaña específica.
        /// Se llama al método CloseTabButton_Click interno para reutilizar la lógica de cierre.
        /// </summary>
        /// <param name="tabToClose">El TabItem que se desea cerrar.</param>
        public void CloseBrowserTab(TabItem tabToClose)
        {
            Button closeButton = null;
            // Intentar encontrar el botón de cerrar si la pestaña tiene el header con DockPanel
            if (tabToClose.Header is DockPanel headerPanel)
            {
                closeButton = headerPanel.Children.OfType<Button>().FirstOrDefault(b => b.Content.ToString() == "✖");
            }

            if (closeButton != null)
            {
                CloseTabButton_Click(closeButton, new RoutedEventArgs());
            }
            else
            {
                // Fallback si no se encuentra el botón de cerrar (ej. pestaña suspendida que no tiene el botón en el header)
                // En ese caso, la lógica de eliminación directa del TabControl y la lista _browserTabs
                var browserTabItem = GetBrowserTabItemFromTabItem(tabToClose);
                if (browserTabItem != null)
                {
                    browserTabItem.LeftWebView?.Dispose();
                    browserTabItem.RightWebView?.Dispose();
                    browserTabItem.ParentGroup?.TabsInGroup.Remove(browserTabItem); // Eliminar del grupo
                }

                // Si el grupo se queda vacío, eliminarlo (excepto el grupo por defecto si es el único)
                foreach (var group in _tabGroupManager.TabGroups.ToList())
                {
                    if (!group.TabsInGroup.Any() && _tabGroupManager.TabGroups.Count > 1)
                    {
                        _tabGroupManager.RemoveGroup(group);
                    }
                }

                // Si no quedan pestañas en ningún grupo, abre una nueva por defecto para evitar una ventana vacía.
                if (!_tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Any())
                {
                    AddNewTab();
                }
            }
        }

        /// <summary>
        /// Método interno para obtener la pestaña activa (para pasar al TabManagerWindow).
        /// </summary>
        /// <returns>El TabItem actualmente seleccionado.</returns>
        private TabItem GetCurrentBrowserTabItemInternal()
        {
            return SelectedTabItem?.Tab; // Ahora usa SelectedTabItem
        }


        /// <summary>
        /// Activa el modo de pantalla dividida para la pestaña actual.
        /// </summary>
        /// <param name="tabItem">La pestaña a dividir.</param>
        /// <param name="rightPanelUrl">La URL a cargar en el panel derecho.</param>
        private async System.Threading.Tasks.Task EnableSplitScreenForCurrentTab(BrowserTabItem tabItem, string rightPanelUrl)
        {
            // Detener lectura en voz alta si está activa
            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _isReadingAloud = false;
                ReadAloudButton.Content = "🔊";
            }

            // Crear el segundo WebView2
            WebView2 webView2 = new WebView2();
            webView2.Source = new Uri(rightPanelUrl); // La segunda vista empieza en la URL especificada
            webView2.Name = "WebView2_Tab" + tabItem.ParentGroup.TabsInGroup.IndexOf(tabItem); // Nombre único
            webView2.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView2.VerticalAlignment = VerticalAlignment.Stretch;

            // Usar el mismo entorno que el LeftWebView para consistencia
            CoreWebView2Environment envToUse = tabItem.IsIncognito ? _incognitoEnvironment : _defaultEnvironment;
            webView2.CoreWebView2InitializationCompleted += (s, ev) => ConfigureCoreWebView2(webView2, ev, envToUse);

            await webView2.EnsureCoreWebView2Async(null); // Asegurar inicialización

            // Crear el Grid para los dos WebView2s
            Grid splitGrid = new Grid();
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Para el splitter
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Mover el LeftWebView al primer panel
            Grid.SetColumn(tabItem.LeftWebView, 0);
            splitGrid.Children.Add(tabItem.LeftWebView);

            // Añadir el GridSplitter
            GridSplitter splitter = new GridSplitter
            {
                Width = 5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.LightGray,
                ResizeBehavior = GridResizeBehavior.PreviousAndNext
            };
            Grid.SetColumn(splitter, 1);
            splitGrid.Children.Add(splitter);

            // Añadir el nuevo WebView2 al segundo panel
            Grid.SetColumn(webView2, 2);
            splitGrid.Children.Add(webView2);

            // Actualizar el contenido de la pestaña
            tabItem.Tab.Content = splitGrid;
            tabItem.RightWebView = webView2;
            tabItem.IsSplit = true;
        }

        /// <summary>
        /// Desactiva el modo de pantalla dividida para la pestaña dada.
        /// </summary>
        private void DisableSplitScreenForCurrentTab(BrowserTabItem tabItem)
        {
            // Detener lectura en voz alta si está activa
            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _isReadingAloud = false;
                ReadAloudButton.Content = "🔊";
            }

            // Asegurarse de que el LeftWebView no se deseche (es el principal)
            // Quitar el LeftWebView de su contenedor actual (el splitGrid)
            Grid currentGrid = tabItem.Tab.Content as Grid;
            if (currentGrid != null)
            {
                currentGrid.Children.Remove(tabItem.LeftWebView);
            }

            // Desechar el RightWebView
            if (tabItem.RightWebView != null)
            {
                tabItem.RightWebView.Dispose();
                tabItem.RightWebView = null;
            }

            // Restaurar el contenido de la pestaña a solo el LeftWebView
            Grid singleViewGrid = new Grid();
            singleViewGrid.Children.Add(tabItem.LeftWebView); // Vuelve a añadirlo a un Grid simple
            tabItem.Tab.Content = singleViewGrid;
            tabItem.IsSplit = false;
        }

        /// <summary>
        /// Maneja el clic en el botón "Modo Incógnito". Abre una nueva pestaña en modo incógnito.
        /// </summary>
        private void IncognitoButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(_defaultHomePage, isIncognito: true);
        }

        /// <summary>
        /// Cierra una pestaña cuando se hace clic en su botón "X".
        /// </summary>
        public void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            Button closeButton = sender as Button;
            TabItem tabToClose = closeButton?.Tag as TabItem; // Obtener el TabItem asociado al botón

            if (tabToClose != null)
            {
                var browserTabItem = GetBrowserTabItemFromTabItem(tabToClose);

                if (browserTabItem != null)
                {
                    browserTabItem.ParentGroup?.TabsInGroup.Remove(browserTabItem); // Eliminar del grupo
                    browserTabItem.LeftWebView?.Dispose();
                    browserTabItem.RightWebView?.Dispose();

                    // Si el grupo se queda vacío, eliminarlo (excepto el grupo por defecto si es el único)
                    if (!browserTabItem.ParentGroup.TabsInGroup.Any() && _tabGroupManager.TabGroups.Count > 1)
                    {
                        _tabGroupManager.RemoveGroup(browserTabItem.ParentGroup);
                    }
                }

                // Si no quedan pestañas en ningún grupo, abre una nueva por defecto para evitar una ventana vacía.
                if (!_tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Any())
                {
                    AddNewTab();
                }
                else
                {
                    // Asegurarse de que haya una pestaña seleccionada después de cerrar
                    if (SelectedTabItem == browserTabItem && _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Any())
                    {
                        SelectedTabItem = _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).First();
                        SelectedTabItem.Tab.IsSelected = true;
                    }
                }
            }
        }

        /// <summary>
        /// NUEVO: Se ejecuta cuando la selección de la pestaña en un TabControl dentro de un grupo cambia.
        /// </summary>
        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            TabControl currentTabControl = sender as TabControl;
            if (currentTabControl != null && currentTabControl.SelectedItem is BrowserTabItem selectedBrowserTab)
            {
                SelectedTabItem = selectedBrowserTab; // Actualizar la propiedad global
                UpdateUrlTextBoxFromCurrentTab();

                // Detener la lectura en voz alta si la pestaña activa cambia
                if (_isReadingAloud)
                {
                    _speechSynthesizer.SpeakAsyncCancelAll();
                    _isReadingAloud = false;
                    ReadAloudButton.Content = "🔊"; // Restaurar icono
                }

                // Actualizar el icono del botón de pantalla dividida
                SplitScreenButton.Content = selectedBrowserTab.IsSplit ? "➡️" : "↔️";

                // Si la pestaña seleccionada está suspendida, reactivarla
                if (selectedBrowserTab.LeftWebView == null) // Si el LeftWebView es nulo, la pestaña está suspendida
                {
                    if (_isTabSuspensionEnabled) // Solo reactivar si la suspensión está habilitada
                    {
                        // Recrear WebView2 y cargar la URL
                        string urlToReload = selectedBrowserTab.Tab.Tag?.ToString(); // Obtener la URL guardada

                        WebView2 newWebView = new WebView2();
                        newWebView.Source = new Uri(urlToReload ?? _defaultHomePage);
                        newWebView.Name = "WebView1_Tab" + (selectedBrowserTab.ParentGroup.TabsInGroup.IndexOf(selectedBrowserTab) + 1);
                        newWebView.HorizontalAlignment = HorizontalAlignment.Stretch;
                        newWebView.VerticalAlignment = VerticalAlignment.Stretch;

                        // Enlazar eventos (similar a AddNewTab, asegurando el entorno correcto)
                        newWebView.Loaded += WebView_Loaded;
                        CoreWebView2Environment envToUse = selectedBrowserTab.IsIncognito ? _incognitoEnvironment : _defaultEnvironment;
                        newWebView.CoreWebView2InitializationCompleted += (s, ev) => ConfigureCoreWebView2(newWebView, ev, envToUse);
                        newWebView.CoreWebView2.FindInPageCompleted += CoreWebView2_FindInPageCompleted;
                        newWebView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;
                        newWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                        newWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                        newWebView.CoreWebView2.FaviconChanged += CoreWebView2_FaviconChanged;
                        newWebView.CoreWebView2.IsAudioPlayingChanged += CoreWebView2_IsAudioPlayingChanged;
                        newWebView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;


                        // Reemplazar el contenido de la pestaña (volverá a ser una vista simple)
                        Grid tabContent = new Grid();
                        tabContent.Children.Add(newWebView);
                        selectedBrowserTab.Tab.Content = tabContent;

                        selectedBrowserTab.LeftWebView = newWebView; // Actualizar la referencia al nuevo WebView
                        selectedBrowserTab.RightWebView = null; // Asegurar que el derecho sea nulo
                        selectedBrowserTab.IsSplit = false; // No está en modo dividido al reactivar

                        // Restaurar el título original (quitar "(Suspendida)")
                        string originalHeaderText = selectedBrowserTab.HeaderTextBlock.Text;
                        if (originalHeaderText.StartsWith("(Suspendida) ")) // Evitar duplicar el prefijo
                        {
                            selectedBrowserTab.HeaderTextBlock.Text = originalHeaderText.Replace("(Suspendida) ", "");
                        }
                    }
                    else
                    {
                        // Si la suspensión no está activa pero la pestaña está suspendida (ej. se deshabilitó la opción)
                        // recargarla como una nueva pestaña normal.
                        string urlToReload = selectedBrowserTab.Tab.Tag?.ToString();
                        // Remover la pestaña suspendida y añadir una nueva
                        selectedBrowserTab.ParentGroup.TabsInGroup.Remove(selectedBrowserTab);
                        AddNewTab(urlToReload, selectedBrowserTab.IsIncognito, selectedBrowserTab.ParentGroup);
                    }
                }
            }
            // Al cambiar de pestaña, ocultar la barra de búsqueda y limpiar resultados anteriores
            _isFindBarVisible = false;
            FindBar.Visibility = Visibility.Collapsed;
            ClearFindResults();
        }

        /// <summary>
        /// Actualiza el texto de la barra de URL y el título de la ventana
        /// con la información de la pestaña actualmente seleccionada.
        /// </summary>
        private void UpdateUrlTextBoxFromCurrentTab()
        {
            WebView2 currentWebView = GetCurrentWebView(); // Esto ahora devuelve el LeftWebView por defecto
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                UrlTextBox.Text = currentWebView.CoreWebView2.Source;
                this.Title = currentWebView.CoreWebView2.DocumentTitle + " - Aurora Browser"; // Título cambiado a Aurora Browser
            }
            else
            {
                // Si no hay pestaña activa o el WebView principal no está listo, limpia la barra de URL y el título.
                UrlTextBox.Text = string.Empty;
                this.Title = "Aurora Browser"; // Título cambiado a Aurora Browser
            }
        }

        /// <summary>
        /// Obtiene la instancia de WebView2 principal (izquierda) de la pestaña actualmente seleccionada.
        /// </summary>
        /// <returns>El LeftWebView de la pestaña activa, o null si no hay una pestaña seleccionada o su contenido no es válido.</returns>
        public WebView2 GetCurrentWebView()
        {
            return SelectedTabItem?.LeftWebView; // Ahora usa SelectedTabItem
        }

        /// <summary>
        /// NUEVO: Obtiene el objeto BrowserTabItem a partir de un TabItem de la UI.
        /// </summary>
        private BrowserTabItem GetBrowserTabItemFromTabItem(TabItem tabItem)
        {
            return _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(bti => bti.Tab == tabItem);
        }

        /// <summary>
        /// NUEVO: Obtiene el objeto BrowserTabItem a partir de un WebView2.
        /// </summary>
        private BrowserTabItem GetBrowserTabItemFromWebView(WebView2 webView)
        {
            return _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(bti => bti.LeftWebView == webView || bti.RightWebView == webView);
        }


        /// <summary>
        /// Comprueba el número de pestañas abiertas y sugiere suspenderlas si hay demasiadas.
        /// </summary>
        private void CheckAndSuggestTabSuspension()
        {
            const int MaxTabsBeforeSuggestion = 15; // Límite para sugerir suspensión
            int activeTabs = _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Count(t => t.LeftWebView != null && !t.IsIncognito && !t.IsSplit); // Contar solo pestañas activas y no incógnito/divididas

            if (_isTabSuspensionEnabled && activeTabs > MaxTabsBeforeSuggestion)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Tienes {activeTabs} pestañas activas. Para mejorar el rendimiento, ¿te gustaría suspender las pestañas inactivas ahora?",
                    "Sugerencia de Suspensión de Pestañas",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    SettingsWindow_OnSuspendInactiveTabs(); // Llama a la lógica de suspensión
                }
            }
        }


        /// <summary>
        /// Maneja el clic en el botón "Opciones". Abre la ventana de configuración.
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Pasa la página de inicio actual, el estado del bloqueador, la URL del motor de búsqueda y el estado de suspensión
            SettingsWindow settingsWindow = new SettingsWindow(_defaultHomePage, AdBlocker.IsEnabled, _defaultSearchEngineUrl, _isTabSuspensionEnabled, _restoreSessionOnStartup, TrackerBlocker.IsEnabled, _isPdfViewerEnabled);

            // Suscribirse a los nuevos eventos de la ventana de configuración
            settingsWindow.OnClearBrowsingData += SettingsWindow_OnClearBrowsingData;
            settingsWindow.OnSuspendInactiveTabs += SettingsWindow_OnSuspendInactiveTabs;


            if (settingsWindow.ShowDialog() == true) // Muestra la ventana de configuración como un diálogo modal
            {
                // Si el usuario hizo clic en "Guardar" en la ventana de configuración
                _defaultHomePage = settingsWindow.HomePage; // Actualiza la página de inicio
                AdBlocker.IsEnabled = settingsWindow.IsAdBlockerEnabled; // Actualiza el estado del bloqueador
                _defaultSearchEngineUrl = settingsWindow.SearchEngineUrl; // Actualiza la URL del motor de búsqueda
                _isTabSuspensionEnabled = settingsWindow.IsTabSuspensionEnabled; // Actualiza el estado de suspensión
                _restoreSessionOnStartup = settingsWindow.RestoreSessionOnStartup; // Actualiza el estado de restaurar sesión
                TrackerBlocker.IsEnabled = settingsWindow.IsTrackerProtectionEnabled; // Actualiza el estado de TrackerBlocker
                _isPdfViewerEnabled = settingsWindow.IsPdfViewerEnabled; // Actualiza el estado del visor de PDF
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

            foreach (var browserTab in _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).ToList()) // Iterar sobre todos los BrowserTabItem
            {
                // No suspender la pestaña activa ni las pestañas incógnito (ya que su data no es persistente)
                // Tampoco suspender pestañas en modo dividido, para evitar complejidad adicional
                if (browserTab != SelectedTabItem && !browserTab.IsIncognito && !browserTab.IsSplit) // Comparar con SelectedTabItem
                {
                    // Un enfoque simple para "suspender": reemplazar el contenido con un mensaje y liberar el LeftWebView.
                    // Cuando el usuario vuelve a la pestaña, el WebView2 se recrea y se recarga la URL.
                    if (browserTab.LeftWebView != null && browserTab.LeftWebView.CoreWebView2 != null)
                    {
                        // Guardar la URL actual antes de desechar
                        string suspendedUrl = browserTab.LeftWebView.Source.OriginalString;

                        // Desechar el WebView2 para liberar recursos
                        browserTab.LeftWebView.Dispose();
                        browserTab.LeftWebView = null; // Marcar como nulo para indicar que está suspendido

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
                        browserTab.Tab.Content = suspendedMessage;
                        browserTab.Tab.Tag = suspendedUrl; // Guardar la URL en el Tag del TabItem para recargarla

                        // Cambiar el encabezado de la pestaña para indicar que está suspendida
                        string originalHeaderText = browserTab.HeaderTextBlock.Text;
                        if (!originalHeaderText.StartsWith("(Suspendida) ")) // Evitar duplicar el prefijo
                        {
                            browserTab.HeaderTextBlock.Text = "(Suspendida) " + originalHeaderText;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Carga las configuraciones de la aplicación (página de inicio, estado del bloqueador, motor de búsqueda, suspensión de pestañas, restaurar sesión, protección contra rastreadores, visor de PDF) desde App.config.
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

            // Cargar el estado de restaurar sesión al inicio
            string savedRestoreSessionState = ConfigurationManager.AppSettings[RestoreSessionSettingKey];
            if (bool.TryParse(savedRestoreSessionState, out bool restoreSession))
            {
                _restoreSessionOnStartup = restoreSession;
            }
            else
            {
                _restoreSessionOnStartup = true; // Por defecto habilitado
            }

            // Cargar el estado de la protección contra rastreadores
            string savedTrackerProtectionState = ConfigurationManager.AppSettings[TrackerProtectionSettingKey];
            if (bool.TryParse(savedTrackerProtectionState, out bool isTrackerProtectionEnabled))
            {
                TrackerBlocker.IsEnabled = isTrackerProtectionEnabled;
            }
            else
            {
                TrackerBlocker.IsEnabled = false; // Por defecto deshabilitado
            }

            // Cargar el estado del visor de PDF
            string savedPdfViewerState = ConfigurationManager.AppSettings[PdfViewerSettingKey];
            if (bool.TryParse(savedPdfViewerState, out bool isPdfViewerEnabled))
            {
                _isPdfViewerEnabled = isPdfViewerEnabled;
            }
            else
            {
                _isPdfViewerEnabled = true; // Por defecto habilitado
            }
        }

        /// <summary>
        /// Guarda las configuraciones actuales (página de inicio, estado del bloqueador, motor de búsqueda, suspensión de pestañas, restaurar sesión, protección contra rastreadores, visor de PDF) en App.config.
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

            // Guardar estado de restaurar sesión al inicio
            if (config.AppSettings.Settings[RestoreSessionSettingKey] == null)
                config.AppSettings.Settings.Add(RestoreSessionSettingKey, _restoreSessionOnStartup.ToString());
            else
                config.AppSettings.Settings[RestoreSessionSettingKey].Value = _restoreSessionOnStartup.ToString();

            // Guardar estado de la protección contra rastreadores
            if (config.AppSettings.Settings[TrackerProtectionSettingKey] == null)
                config.AppSettings.Settings.Add(TrackerProtectionSettingKey, TrackerBlocker.IsEnabled.ToString());
            else
                config.AppSettings.Settings[TrackerProtectionSettingKey].Value = TrackerBlocker.IsEnabled.ToString();

            // Guardar estado del visor de PDF
            if (config.AppSettings.Settings[PdfViewerSettingKey] == null)
                config.AppSettings.Settings.Add(PdfViewerSettingKey, _isPdfViewerEnabled.ToString());
            else
                config.AppSettings.Settings[PdfViewerSettingKey].Value = _isPdfViewerEnabled.ToString();


            // Guardar las URLs de la sesión actual si la restauración está habilitada
            if (_restoreSessionOnStartup)
            {
                List<string> currentUrls = new List<string>();
                // Recorrer todos los BrowserTabItem de todos los grupos
                foreach (var tab in _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup))
                {
                    // Solo guardar URLs de pestañas no incógnito que tengan un WebView2 cargado
                    if (!tab.IsIncognito && tab.LeftWebView != null && tab.LeftWebView.CoreWebView2 != null)
                    {
                        currentUrls.Add(tab.LeftWebView.Source.OriginalString);
                    }
                    else if (!tab.IsIncognito && tab.LeftWebView == null && tab.Tab.Tag is string suspendedUrl) // Pestañas suspendidas
                    {
                        currentUrls.Add(suspendedUrl);
                    }
                }
                string urlsJson = JsonSerializer.Serialize(currentUrls);
                if (config.AppSettings.Settings[LastSessionUrlsSettingKey] == null)
                    config.AppSettings.Settings.Add(LastSessionUrlsSettingKey, urlsJson);
                else
                    config.AppSettings.Settings[LastSessionUrlsSettingKey].Value = urlsJson;
            }
            else
            {
                // Si la restauración de sesión está deshabilitada, limpiar las URLs guardadas
                if (config.AppSettings.Settings[LastSessionUrlsSettingKey] != null)
                {
                    config.AppSettings.Settings.Remove(LastSessionUrlsSettingKey);
                }
            }


            config.Save(ConfigurationSaveMode.Modified); // Guarda los cambios en el archivo de configuración
            ConfigurationManager.RefreshSection("appSettings"); // Refresca la sección para que los nuevos valores estén disponibles inmediatamente
        }

        /// <summary>
        /// Se ejecuta cuando la ventana se está cerrando. Limpia los recursos del entorno incógnito y del sintetizador de voz.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings(); // Asegurarse de guardar la sesión antes de cerrar
            UpdateUncleanShutdownFlag(false); // Establecer el flag a false para indicar un cierre limpio

            // Detener y desechar el sintetizador de voz
            if (_speechSynthesizer != null)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _speechSynthesizer.Dispose();
                _speechSynthesizer = null;
            }

            // Desechar todos los WebViews de todas las pestañas
            foreach (var tab in _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup))
            {
                tab.LeftWebView?.Dispose();
                tab.RightWebView?.Dispose();
            }
            _tabGroupManager.TabGroups.Clear(); // Limpiar la lista de grupos

            // Limpiar los entornos de WebView2 (especialmente el de incógnito)
            if (_incognitoEnvironment != null)
            {
                string incognitoUserDataFolder = _incognitoEnvironment.UserDataFolder;
                _incognitoEnvironment = null; // Liberar la referencia
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
                }
            }
            if (_defaultEnvironment != null)
            {
                _defaultEnvironment = null; // Liberar la referencia
            }
        }
    }
}
