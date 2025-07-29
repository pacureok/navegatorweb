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

// Asegúrate de que estas directivas 'using' estén presentes para las clases auxiliares y de servicios
using NavegadorWeb.Classes; // Para TabItemData, TabGroup, TabGroupManager, CapturedPageData, RelayCommand, TabGroupState, ToolbarPosition, PasswordManager
using NavegadorWeb.Extensions; // Para CustomExtension, ExtensionManager
using NavegadorWeb.Services; // Para LanguageService (si se usa directamente aquí, aunque se usa en App.xaml.cs)


namespace NavegadorWeb
{
    // La clase partial MainWindow ya es generada por el compilador para incluir los elementos XAML.
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Claves de configuración para los ajustes de la aplicación
        private const string HomePageSettingKey = "DefaultHomePage";
        private const string AdBlockerSettingKey = "AdBlockerEnabled";
        private const string DefaultSearchEngineSettingKey = "DefaultSearchEngine";
        private const string TabSuspensionSettingKey = "TabSuspensionEnabled";
        private const string RestoreSessionSettingKey = "RestoreSessionOnStartup";
        private const string LastSessionUrlsSettingKey = "LastSessionUrls";
        private const string LastSessionTabGroupsSettingKey = "LastSessionTabGroups";
        private const string LastSelectedTabGroupSettingKey = "LastSelectedTabGroup";
        private const string WindowWidthSettingKey = "WindowWidth";
        private const string WindowHeightSettingKey = "WindowHeight";
        private const string WindowLeftSettingKey = "WindowLeft";
        private const string WindowTopSettingKey = "WindowTop";

        // Campos privados para el estado de la aplicación
        private string _defaultHomePage = "https://www.google.com";
        private bool _isAdBlockerEnabled;
        private bool _isGeminiModeActive;
        private bool _isFindBarVisible;
        private string _findSearchText = "";
        private string _findResultsText = "";
        private TabItemData? _selectedTabItem;
        private double _downloadProgress;
        private Visibility _downloadProgressBarVisibility = Visibility.Collapsed;
        private SpeechSynthesizer? _speechSynthesizer;
        private bool _isReadingAloud = false;
        private System.Timers.Timer _tabSuspensionTimer;
        private TimeSpan _tabSuspensionDelay = TimeSpan.FromMinutes(5); // Retraso predeterminado para la suspensión
        private bool _isTabSuspensionEnabled;

        // Propiedades públicas con notificación de cambio (INotifyPropertyChanged)
        public bool IsAdBlockerEnabled
        {
            get => _isAdBlockerEnabled;
            set
            {
                if (_isAdBlockerEnabled != value)
                {
                    _isAdBlockerEnabled = value;
                    OnPropertyChanged(nameof(IsAdBlockerEnabled));
                    ApplyAdBlockerSettings(); // Aplica la configuración del bloqueador de anuncios cuando cambia
                }
            }
        }

        public bool IsGeminiModeActive
        {
            get => _isGeminiModeActive;
            set
            {
                if (_isGeminiModeActive != value)
                {
                    _isGeminiModeActive = value;
                    OnPropertyChanged(nameof(IsGeminiModeActive));
                    ApplyTheme(); // Aplica los cambios de tema cuando se alterna el modo Gemini
                }
            }
        }

        public bool IsFindBarVisible
        {
            get => _isFindBarVisible;
            set
            {
                if (_isFindBarVisible != value)
                {
                    _isFindBarVisible = value;
                    OnPropertyChanged(nameof(IsFindBarVisible));
                }
            }
        }

        public string FindSearchText
        {
            get => _findSearchText;
            set
            {
                if (_findSearchText != value)
                {
                    _findSearchText = value;
                    OnPropertyChanged(nameof(FindSearchText));
                    FindInPage(_findSearchText); // Realiza la búsqueda cuando el texto cambia
                }
            }
        }

        public string FindResultsText
        {
            get => _findResultsText;
            set
            {
                if (_findResultsText != value)
                {
                    _findResultsText = value;
                    OnPropertyChanged(nameof(FindResultsText));
                }
            }
        }

        public TabItemData? SelectedTabItem
        {
            get => _selectedTabItem;
            set
            {
                if (_selectedTabItem != value)
                {
                    _selectedTabItem = value;
                    OnPropertyChanged(nameof(SelectedTabItem));
                    UpdateBrowserControls(); // Actualiza los controles de la interfaz de usuario según la pestaña seleccionada
                }
            }
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged(nameof(DownloadProgress));
            }
        }

        public Visibility DownloadProgressBarVisibility
        {
            get => _downloadProgressBarVisibility;
            set
            {
                _downloadProgressBarVisibility = value;
                OnPropertyChanged(nameof(DownloadProgressBarVisibility));
            }
        }

        public TabGroupManager TabGroupManager { get; private set; }
        public ExtensionManager ExtensionManager { get; private set; }

        public ObservableCollection<CapturedPageData> CapturedPagesForGemini { get; set; } = new ObservableCollection<CapturedPageData>();

        public bool IsTabSuspensionEnabled
        {
            get => _isTabSuspensionEnabled;
            set
            {
                if (_isTabSuspensionEnabled != value)
                {
                    _isTabSuspensionEnabled = value;
                    OnPropertyChanged(nameof(IsTabSuspensionEnabled));
                    if (IsTabSuspensionEnabled)
                    {
                        StartTabSuspensionTimer();
                    }
                    else
                    {
                        StopTabSuspensionTimer();
                    }
                }
            }
        }

        // Evento para la notificación de cambio de propiedad
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Constantes para el tamaño mínimo de la ventana
        private const double MIN_WIDTH = 800;
        private const double MIN_HEIGHT = 600;

        // DllImport para arrastrar la ventana (barra de título personalizada)
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public MainWindow()
        {
            InitializeComponent(); // Este método es generado automáticamente por WPF para inicializar los elementos XAML.
            this.DataContext = this; // Establece el DataContext para el enlace de datos

            TabGroupManager = new TabGroupManager();
            ExtensionManager = new ExtensionManager();

            LoadSettings(); // Carga la configuración de la aplicación
            ApplyAdBlockerSettings(); // Aplica la configuración del bloqueador de anuncios
            ApplyTheme(); // Aplica el tema actual

            // Inicializa el sintetizador de voz para la función de lectura en voz alta
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();

            // Configura el temporizador de suspensión de pestañas
            _tabSuspensionTimer = new System.Timers.Timer(_tabSuspensionDelay.TotalMilliseconds);
            _tabSuspensionTimer.Elapsed += TabSuspensionTimer_Elapsed;
            _tabSuspensionTimer.AutoReset = true; // El temporizador se reinicia automáticamente
            if (IsTabSuspensionEnabled)
            {
                StartTabSuspensionTimer();
            }

            // Registra los eventos de actividad del usuario para reiniciar el temporizador de suspensión de pestañas
            this.PreviewMouseMove += MainWindow_UserActivity;
            this.PreviewKeyDown += MainWindow_UserActivity;
        }

        // Inicia el temporizador de suspensión de pestañas
        private void StartTabSuspensionTimer()
        {
            _tabSuspensionTimer.Start();
        }

        // Detiene el temporizador de suspensión de pestañas
        private void StopTabSuspensionTimer()
        {
            _tabSuspensionTimer.Stop();
        }

        // Reinicia el temporizador de suspensión de pestañas con la actividad del usuario
        private void MainWindow_UserActivity(object sender, EventArgs e)
        {
            if (IsTabSuspensionEnabled)
            {
                _tabSuspensionTimer.Stop();
                _tabSuspensionTimer.Start();
            }
        }

        // Suspende las pestañas inactivas cuando el temporizador expira
        private async void TabSuspensionTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // Usa Dispatcher.InvokeAsync para actualizar la UI desde un hilo diferente
            await Dispatcher.InvokeAsync(async () =>
            {
                foreach (var group in TabGroupManager.TabGroups)
                {
                    foreach (var tab in group.TabsInGroup)
                    {
                        // Suspende la pestaña si no es la pestaña seleccionada actualmente y tiene una instancia de WebView2
                        if (tab != SelectedTabItem && tab.WebViewInstance != null && tab.WebViewInstance.CoreWebView2 != null)
                        {
                            tab.IsSuspended = true;
                            tab.LastSuspendedUrl = tab.WebViewInstance.Source.ToString(); // Guarda la URL actual
                            tab.WebViewInstance.CoreWebView2.Stop(); // Detiene la carga
                            tab.WebViewInstance.Source = new Uri("about:blank"); // Navega a una página en blanco
                            Console.WriteLine($"Pestaña suspendida: {tab.Title}");
                        }
                    }
                }
            });
        }

        // Activa una pestaña suspendida
        public async void ActivateTab(TabItemData tab)
        {
            if (tab.IsSuspended)
            {
                tab.IsSuspended = false;
                // Si la pestaña estaba suspendida y tiene una URL guardada, navega de nuevo a ella
                if (tab.WebViewInstance != null && tab.WebViewInstance.CoreWebView2 != null && !string.IsNullOrEmpty(tab.LastSuspendedUrl))
                {
                    tab.WebViewInstance.Source = new Uri(tab.LastSuspendedUrl);
                    Console.WriteLine($"Pestaña reactivada: {tab.Title}");
                }
            }
            SelectedTabItem = tab; // Establece esta pestaña como la seleccionada
        }

        // Carga la configuración de la aplicación desde ConfigurationManager
        private void LoadSettings()
        {
            _defaultHomePage = ConfigurationManager.AppSettings[HomePageSettingKey] ?? "https://www.google.com";
            IsAdBlockerEnabled = bool.Parse(ConfigurationManager.AppSettings[AdBlockerSettingKey] ?? "false");
            IsTabSuspensionEnabled = bool.Parse(ConfigurationManager.AppSettings[TabSuspensionSettingKey] ?? "false");

            // Restaura la última sesión si está habilitada, de lo contrario, abre una nueva pestaña con la página de inicio predeterminada
            if (bool.Parse(ConfigurationManager.AppSettings[RestoreSessionSettingKey] ?? "false"))
            {
                RestoreLastSession();
            }
            else
            {
                AddNewTab(_defaultHomePage);
            }

            // Carga el tamaño y la posición de la ventana
            if (double.TryParse(ConfigurationManager.AppSettings[WindowWidthSettingKey], out double width))
            {
                this.Width = Math.Max(width, MIN_WIDTH);
            }
            if (double.TryParse(ConfigurationManager.AppSettings[WindowHeightSettingKey], out double height))
            {
                this.Height = Math.Max(height, MIN_HEIGHT);
            }
            if (double.TryParse(ConfigurationManager.AppSettings[WindowLeftSettingKey], out double left))
            {
                this.Left = left;
            }
            if (double.TryParse(ConfigurationManager.AppSettings[WindowTopSettingKey], out double top))
            {
                this.Top = top;
            }
        }

        // Guarda la configuración de la aplicación en ConfigurationManager
        private void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Actualiza o añade las claves de configuración
            UpdateAppSetting(config, HomePageSettingKey, _defaultHomePage);
            UpdateAppSetting(config, AdBlockerSettingKey, IsAdBlockerEnabled.ToString());
            UpdateAppSetting(config, TabSuspensionSettingKey, IsTabSuspensionEnabled.ToString());
            UpdateAppSetting(config, RestoreSessionSettingKey, bool.Parse(ConfigurationManager.AppSettings[RestoreSessionSettingKey] ?? "false").ToString()); // Mantener el valor de la UI de SettingsWindow

            // Guarda el tamaño y la posición de la ventana antes de cerrar
            if (this.WindowState == WindowState.Maximized)
            {
                UpdateAppSetting(config, WindowWidthSettingKey, this.RestoreBounds.Width.ToString());
                UpdateAppSetting(config, WindowHeightSettingKey, this.RestoreBounds.Height.ToString());
                UpdateAppSetting(config, WindowLeftSettingKey, this.RestoreBounds.Left.ToString());
                UpdateAppSetting(config, WindowTopSettingKey, this.RestoreBounds.Top.ToString());
            }
            else
            {
                UpdateAppSetting(config, WindowWidthSettingKey, this.Width.ToString());
                UpdateAppSetting(config, WindowHeightSettingKey, this.Height.ToString());
                UpdateAppSetting(config, WindowLeftSettingKey, this.Left.ToString());
                UpdateAppSetting(config, WindowTopSettingKey, this.Top.ToString());
            }

            // Guarda la sesión actual si la restauración al inicio está habilitada, de lo contrario, borra los datos de la sesión
            if (bool.Parse(ConfigurationManager.AppSettings[RestoreSessionSettingKey] ?? "false"))
            {
                SaveCurrentSession(config);
            }
            else
            {
                UpdateAppSetting(config, LastSessionUrlsSettingKey, "");
                UpdateAppSetting(config, LastSessionTabGroupsSettingKey, "");
                UpdateAppSetting(config, LastSelectedTabGroupSettingKey, "");
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        // Método auxiliar para actualizar o añadir una clave en AppSettings
        private void UpdateAppSetting(Configuration config, string key, string value)
        {
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }
        }

        // Guarda las URLs de todas las pestañas abiertas y los grupos de pestañas en la configuración
        private void SaveCurrentSession(Configuration config)
        {
            // Serializa todas las URLs de las pestañas
            var allUrls = TabGroupManager.TabGroups
                                        .SelectMany(g => g.TabsInGroup)
                                        .Select(tab => tab.WebViewInstance?.Source?.ToString())
                                        .Where(url => !string.IsNullOrEmpty(url) && url != "about:blank")
                                        .ToList();
            UpdateAppSetting(config, LastSessionUrlsSettingKey, JsonSerializer.Serialize(allUrls));

            // Serializa los estados de los grupos de pestañas (ID del grupo, nombre, URLs de las pestañas, URL de la pestaña seleccionada)
            var groupStates = TabGroupManager.TabGroups.Select(g => new TabGroupState
            {
                GroupId = g.GroupId,
                GroupName = g.GroupName,
                TabUrls = g.TabsInGroup.Select(t => t.WebViewInstance?.Source?.ToString()).Where(url => !string.IsNullOrEmpty(url) && url != "about:blank").ToList(),
                SelectedTabUrl = g.SelectedTabItem?.WebViewInstance?.Source?.ToString()
            }).ToList();
            UpdateAppSetting(config, LastSessionTabGroupsSettingKey, JsonSerializer.Serialize(groupStates));

            // Guarda el ID del grupo de pestañas seleccionado actualmente
            UpdateAppSetting(config, LastSelectedTabGroupSettingKey, TabGroupManager.SelectedTabGroup?.GroupId ?? "");
        }

        // Restaura la última sesión de navegación desde la configuración
        private async void RestoreLastSession()
        {
            var savedTabGroupsJson = ConfigurationManager.AppSettings[LastSessionTabGroupsSettingKey];
            if (!string.IsNullOrEmpty(savedTabGroupsJson))
            {
                try
                {
                    var groupStates = JsonSerializer.Deserialize<List<TabGroupState>>(savedTabGroupsJson);
                    if (groupStates != null && groupStates.Any())
                    {
                        TabGroupManager.TabGroups.Clear(); // Borra la pestaña predeterminada existente

                        foreach (var groupState in groupStates)
                        {
                            var newGroup = new TabGroup(groupState.GroupId, groupState.GroupName);
                            TabGroupManager.AddGroup(newGroup);

                            foreach (var url in groupState.TabUrls)
                            {
                                if (!string.IsNullOrEmpty(url))
                                {
                                    var newTab = CreateNewTabItem(url);
                                    newGroup.AddTab(newTab);
                                    // Asegúrate de que CoreWebView2 esté inicializado para cada pestaña restaurada
                                    await newTab.WebViewInstance.EnsureCoreWebView2Async(null);
                                }
                            }

                            if (!string.IsNullOrEmpty(groupState.SelectedTabUrl))
                            {
                                var selectedTab = newGroup.TabsInGroup.FirstOrDefault(t => t.WebViewInstance?.Source?.ToString() == groupState.SelectedTabUrl);
                                if (selectedTab != null)
                                {
                                    newGroup.SelectedTabItem = selectedTab;
                                }
                            }
                        }

                        // Restaura el último grupo de pestañas seleccionado
                        var lastSelectedGroupId = ConfigurationManager.AppSettings[LastSelectedTabGroupSettingKey];
                        var restoredSelectedGroup = TabGroupManager.TabGroups.FirstOrDefault(g => g.GroupId == lastSelectedGroupId);
                        if (restoredSelectedGroup != null)
                        {
                            TabGroupManager.SelectedTabGroup = restoredSelectedGroup;
                            BrowserTabs.ItemsSource = restoredSelectedGroup.TabsInGroup;
                            SelectedTabItem = restoredSelectedGroup.SelectedTabItem;
                        }
                        else
                        {
                            // Vuelve al grupo predeterminado si no se encuentra el último grupo seleccionado
                            TabGroupManager.SelectedTabGroup = TabGroupManager.GetDefaultGroup();
                            BrowserTabs.ItemsSource = TabGroupManager.GetDefaultGroup().TabsInGroup;
                            SelectedTabItem = TabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault();
                        }
                    }
                    else
                    {
                        AddNewTab(_defaultHomePage); // Si no hay sesión guardada, abre la página de inicio predeterminada
                    }
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Error al restaurar la sesión: {ex.Message}", "Error de Restauración", MessageBoxButton.OK, MessageBoxImage.Error);
                    AddNewTab(_defaultHomePage); // En caso de error, abre la página de inicio predeterminada
                }
            }
            else
            {
                AddNewTab(_defaultHomePage); // Si no hay datos de sesión guardados, abre la página de inicio predeterminada
            }
        }

        // Aplica la configuración del bloqueador de anuncios a todas las instancias de WebView2
        private void ApplyAdBlockerSettings()
        {
            foreach (var group in TabGroupManager.TabGroups)
            {
                foreach (var tab in group.TabsInGroup)
                {
                    if (tab.WebViewInstance != null && tab.WebViewInstance.CoreWebView2 != null)
                    {
                        // Nota: WebView2 no tiene una función directa de "bloqueador de anuncios".
                        // Esto es un ejemplo de cómo podrías intentar bloquear recursos.
                        // Un bloqueador de anuncios real requeriría una lógica más compleja (listas de filtros, etc.).
                        if (IsAdBlockerEnabled)
                        {
                            // Esto es un ejemplo muy básico y NO es un bloqueador de anuncios completo.
                            // Para un bloqueador real, necesitarías interceptar solicitudes y aplicar reglas.
                            // Aquí, simplemente se evita que WebView2 cargue ciertos tipos de recursos.
                            // Esto puede romper la funcionalidad de algunos sitios web.
                            tab.WebViewInstance.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                            tab.WebViewInstance.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                        }
                        else
                        {
                            // Si el bloqueador está deshabilitado, elimina el filtro de solicitudes.
                            tab.WebViewInstance.CoreWebView2.RemoveWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                            tab.WebViewInstance.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
                        }
                    }
                }
            }
        }

        // Manejador de eventos para CoreWebView2.WebResourceRequested (para el bloqueador de anuncios)
        private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            // Este es un ejemplo simplificado. Un bloqueador de anuncios real es mucho más complejo.
            // Aquí, simplemente cancelamos las solicitudes a dominios conocidos de anuncios (ejemplo).
            string url = e.Request.Uri;
            if (IsAdBlockerEnabled && IsAdDomain(url))
            {
                e.Response = SelectedTabItem?.WebViewInstance?.CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Forbidden", "Content-Type: text/plain");
            }
        }

        // Función de ejemplo para verificar si una URL pertenece a un dominio de anuncios
        private bool IsAdDomain(string url)
        {
            // Lista muy simplificada de dominios de anuncios.
            // En un caso real, esto sería una lista mucho más grande y dinámica.
            string[] adDomains = { "doubleclick.net", "googlesyndication.com", "adservice.google.com" };
            foreach (var domain in adDomains)
            {
                if (url.Contains(domain))
                {
                    return true;
                }
            }
            return false;
        }

        // Aplica el tema actual (predeterminado o modo Gemini)
        private void ApplyTheme()
        {
            // Elimina los recursos dinámicos existentes
            Resources.Remove("BrowserBackgroundColor");
            Resources.Remove("BrowserBackgroundBrush");
            Resources.Remove("BrowserForegroundColor");
            Resources.Remove("BrowserForegroundBrush");

            // Agrega nuevos recursos dinámicos basados en el estado del modo Gemini
            if (IsGeminiModeActive)
            {
                Resources.Add("BrowserBackgroundColor", (Color)FindResource("GeminiBackgroundColor"));
                Resources.Add("BrowserBackgroundBrush", (SolidColorBrush)FindResource("GeminiBackgroundBrush"));
                Resources.Add("BrowserForegroundColor", (Color)FindResource("GeminiForegroundColor"));
                Resources.Add("BrowserForegroundBrush", (SolidColorBrush)FindResource("GeminiForegroundBrush"));
            }
            else
            {
                Resources.Add("BrowserBackgroundColor", (Color)FindResource("DefaultBrowserBackgroundColor"));
                Resources.Add("BrowserBackgroundBrush", (SolidColorBrush)FindResource("DefaultBrowserBackgroundBrush"));
                Resources.Add("BrowserForegroundColor", (Color)FindResource("DefaultBrowserForegroundColor"));
                Resources.Add("BrowserForegroundBrush", (SolidColorBrush)FindResource("DefaultBrowserForegroundBrush"));
            }
            // Actualiza el fondo de la ventana principal
            MainBorder.Background = (SolidColorBrush)Resources["BrowserBackgroundBrush"];
        }

        // Controlador de eventos para el evento de carga de la ventana
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // El tamaño y la posición de la ventana ya se cargan en LoadSettings()
            // Aquí solo aseguramos que el estado maximizado/normal se refleje correctamente
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeRestoreButton.Content = "🗖"; // Cambia el icono si está maximizado
            }
            else
            {
                MaximizeRestoreButton.Content = "🗖"; // Icono de restaurar si no está maximizado
            }

            // Establece el ItemsSource y el elemento seleccionado del TabControl
            // Esto se hace después de restaurar la sesión en LoadSettings
            if (TabGroupManager.SelectedTabGroup == null && TabGroupManager.TabGroups.Any())
            {
                TabGroupManager.SelectedTabGroup = TabGroupManager.GetDefaultGroup();
            }
            BrowserTabs.ItemsSource = TabGroupManager.SelectedTabGroup?.TabsInGroup;
            SelectedTabItem = TabGroupManager.SelectedTabGroup?.TabsInGroup.FirstOrDefault();

            // Aplica TabHeaderTemplate y TabContentTemplate
            BrowserTabs.ItemTemplate = (DataTemplate)this.Resources["TabHeaderTemplate"];
            BrowserTabs.ContentTemplate = (DataTemplate)this.Resources["TabContentTemplate"];
            BrowserTabs.ItemContainerStyle = (Style)this.Resources["TabItemStyle"];


            // Registra los eventos de WebView2 para todas las pestañas existentes
            foreach (var group in TabGroupManager.TabGroups)
            {
                foreach (var tab in group.TabsInGroup)
                {
                    // Asegúrate de que WebViewInstance no sea nulo antes de registrar eventos
                    if (tab.WebViewInstance != null)
                    {
                        tab.WebViewInstance.SourceChanged += WebView_SourceChanged;
                        tab.WebViewInstance.NavigationCompleted += WebView_NavigationCompleted;
                        tab.WebViewInstance.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
                        tab.WebViewInstance.WebMessageReceived += WebView_WebMessageReceived;
                        tab.WebViewInstance.DownloadStarting += WebView_DownloadStarting;
                        tab.WebViewInstance.ContextMenuOpening += WebView_ContextMenuOpening;
                    }
                }
            }

            // Carga y muestra las extensiones
            ExtensionManager.LoadExtensions();
            ExtensionsMenuItem.DataContext = ExtensionManager; // Enlaza el menú de extensiones
        }

        // Controlador de eventos para el evento de cierre de la ventana
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings(); // Guarda la configuración de la aplicación y la sesión

            _speechSynthesizer?.Dispose(); // Libera el sintetizador de voz
            _tabSuspensionTimer?.Stop(); // Detiene el temporizador de suspensión de pestañas
            _tabSuspensionTimer?.Dispose(); // Libera el temporizador de suspensión de pestañas

            // Libera todas las instancias de WebView2 para liberar recursos
            foreach (var group in TabGroupManager.TabGroups)
            {
                foreach (var tab in group.TabsInGroup)
                {
                    tab.WebViewInstance?.Dispose();
                }
            }
        }

        // Controlador de eventos para el cambio de estado de la ventana (por ejemplo, maximizada/normal)
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            // Ajusta el grosor del borde y el radio de la esquina cuando cambia el estado de la ventana
            if (WindowState == WindowState.Maximized)
            {
                MainBorder.BorderThickness = new Thickness(0);
                MainBorder.CornerRadius = new CornerRadius(0);
                MaximizeRestoreButton.Content = "🗗"; // Icono de restaurar
            }
            else
            {
                MainBorder.BorderThickness = new Thickness(1);
                MainBorder.CornerRadius = new CornerRadius(10);
                MaximizeRestoreButton.Content = "🗖"; // Icono de maximizar
            }
        }

        // Controlador de eventos para arrastrar la barra de título personalizada
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove(); // Permite arrastrar la ventana
            }
        }

        // Controlador de eventos para el clic del botón minimizar
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Controlador de eventos para el clic del botón maximizar/restaurar
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

        // Controlador de eventos para el clic del botón cerrar
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Crea una nueva instancia de TabItemData con un WebView2
        private TabItemData CreateNewTabItem(string url)
        {
            var webView = new WebView2();
            var newTabItem = new TabItemData(webView)
            {
                Title = "Cargando...",
                Url = url,
                CapturedData = new CapturedPageData { Url = url }
            };

            webView.Source = new Uri(url);
            // Registra los eventos de WebView2 para la nueva pestaña
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.SourceChanged += WebView_SourceChanged;
            webView.WebMessageReceived += WebView_WebMessageReceived;
            webView.DownloadStarting += WebView_DownloadStarting;
            webView.ContextMenuOpening += WebView_ContextMenuOpening;

            // Aplica el estilo de barra de desplazamiento personalizado a WebView2
            webView.Loaded += (s, e) =>
            {
                var border = VisualTreeHelper.GetChild(webView, 0) as Border;
                if (border != null)
                {
                    var scrollViewer = border.Child as ScrollViewer;
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollBarStyle = (Style)FindResource("CustomScrollBarStyle");
                    }
                }
            };

            return newTabItem;
        }

        // Agrega una nueva pestaña al grupo de pestañas predeterminado
        private void AddNewTab(string url = "about:blank")
        {
            TabItemData newTabItem = CreateNewTabItem(url);
            TabGroupManager.GetDefaultGroup().AddTab(newTabItem);
            SelectedTabItem = newTabItem; // Selecciona la pestaña recién agregada
        }

        // Controlador de eventos para agregar una nueva pestaña mediante clic de botón
        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(_defaultHomePage);
        }

        // Controlador de eventos para cerrar una pestaña mediante clic de botón
        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TabItemData tabToClose)
            {
                var currentGroup = TabGroupManager.GetGroupByTab(tabToClose);
                if (currentGroup != null)
                {
                    currentGroup.RemoveTab(tabToClose); // Elimina la pestaña de su grupo
                    tabToClose.WebViewInstance?.Dispose(); // Libera la instancia de WebView2

                    // Si el grupo queda vacío y no es el último grupo, elimina el grupo
                    if (currentGroup.TabsInGroup.Count == 0 && TabGroupManager.TabGroups.Count > 1)
                    {
                        TabGroupManager.RemoveGroup(currentGroup);
                    }

                    // Si todos los grupos de pestañas están vacíos, abre una nueva pestaña predeterminada
                    if (TabGroupManager.TabGroups.All(g => g.TabsInGroup.Count == 0))
                    {
                        AddNewTab(_defaultHomePage);
                    }
                    // Si la pestaña cerrada estaba seleccionada, selecciona otra pestaña en el mismo grupo o la primera pestaña del primer grupo
                    else if (SelectedTabItem == tabToClose && currentGroup.TabsInGroup.Any())
                    {
                        SelectedTabItem = currentGroup.TabsInGroup.FirstOrDefault();
                    }
                    else if (SelectedTabItem == tabToClose && TabGroupManager.TabGroups.Any())
                    {
                        SelectedTabItem = TabGroupManager.TabGroups.First().TabsInGroup.FirstOrDefault();
                        BrowserTabs.ItemsSource = TabGroupManager.SelectedTabGroup?.TabsInGroup;
                    }
                }
            }
        }

        // Controlador de eventos para el clic del botón Atrás
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabItem?.WebViewInstance?.GoBack();
            UpdateNavigationButtons();
        }

        // Controlador de eventos para el clic del botón Adelante
        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabItem?.WebViewInstance?.GoForward();
            UpdateNavigationButtons();
        }

        // Controlador de eventos para el clic del botón Recargar
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabItem?.WebViewInstance?.Reload();
        }

        // Controlador de eventos para el clic del botón Inicio
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl(_defaultHomePage);
        }

        // Controlador de eventos KeyDown para AddressBar (maneja la tecla Enter para la navegación)
        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToUrl(AddressBar.Text);
                Keyboard.ClearFocus(); // Quita el foco de la barra de direcciones
            }
        }

        // Controlador de eventos GotFocus para AddressBar (selecciona todo el texto al obtener el foco)
        private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
        {
            AddressBar.SelectAll();
        }

        // Controlador de eventos LostFocus para AddressBar (no hace nada por ahora)
        private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
        {
            // Puedes añadir lógica aquí si necesitas hacer algo cuando la barra de direcciones pierde el foco
        }

        // Navega la pestaña seleccionada a la URL especificada
        private void NavigateToUrl(string url)
        {
            if (SelectedTabItem != null && SelectedTabItem.WebViewInstance != null)
            {
                string fullUrl = url;
                // Si la URL no contiene un esquema, intenta anteponer "https://" o usa el motor de búsqueda
                if (!url.Contains("://") && !url.StartsWith("file://") && !url.StartsWith("about:"))
                {
                    if (url.Contains(".")) // Probablemente un dominio
                    {
                        fullUrl = "https://" + url;
                    }
                    else // Probablemente una consulta de búsqueda
                    {
                        string searchEngineUrl = ConfigurationManager.AppSettings[DefaultSearchEngineSettingKey] ?? "https://www.google.com/search?q=";
                        fullUrl = searchEngineUrl + Uri.EscapeDataString(url);
                    }
                }

                try
                {
                    SelectedTabItem.WebViewInstance.Source = new Uri(fullUrl);
                }
                catch (UriFormatException)
                {
                    MessageBox.Show("URL o término de búsqueda inválido.", "Error de Navegación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Controlador de eventos para el evento NavigationCompleted de WebView2
        private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView != null)
            {
                var tab = TabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(t => t.WebViewInstance == webView);
                if (tab != null)
                {
                    tab.Url = webView.Source.ToString(); // Actualiza la URL de la pestaña
                    if (SelectedTabItem == tab) // Solo actualiza la barra de direcciones si es la pestaña activa
                    {
                        AddressBar.Text = tab.Url;
                    }

                    await UpdateTabTitleAndFavicon(tab, webView); // Actualiza el título y el favicon de la pestaña

                    if (tab.IsReaderMode)
                    {
                        tab.IsReaderMode = false; // Sale del modo lector si está activo después de la navegación
                    }

                    // Ejecuta todas las extensiones habilitadas
                    foreach (var extension in ExtensionManager.Extensions)
                    {
                        if (extension.IsEnabled)
                        {
                            string scriptContent = extension.LoadScriptContent();
                            if (!string.IsNullOrEmpty(scriptContent))
                            {
                                await webView.CoreWebView2.ExecuteScriptAsync(scriptContent);
                            }
                        }
                    }
                    UpdateNavigationButtons(); // Actualiza los estados de los botones de navegación
                }
            }
        }

        // Actualiza el título y el favicon de la pestaña
        private async Task UpdateTabTitleAndFavicon(TabItemData tab, WebView2 webView)
        {
            if (webView.CoreWebView2 != null)
            {
                try
                {
                    // Obtiene el título de la página
                    string title = await webView.CoreWebView2.ExecuteScriptAsync("document.title");
                    tab.Title = title.Replace("\"", ""); // Elimina las comillas del título

                    // Obtiene la URL del favicon usando JavaScript
                    string getFaviconScript = @"
                        (function() {
                            var faviconLink = document.querySelector('link[rel~=""icon""]');
                            if (faviconLink) {
                                return faviconLink.href;
                            }
                            return null;
                        })();
                    ";
                    string faviconUrlJson = await webView.CoreWebView2.ExecuteScriptAsync(getFaviconScript);
                    string faviconUrl = JsonSerializer.Deserialize<string>(faviconUrlJson) ?? "";

                    if (!string.IsNullOrEmpty(faviconUrl))
                    {
                        // Resuelve las URLs de favicon relativas
                        if (!faviconUrl.Contains("://"))
                        {
                            Uri baseUri = new Uri(webView.Source.ToString());
                            Uri absoluteUri = new Uri(baseUri, faviconUrl);
                            faviconUrl = absoluteUri.ToString();
                        }

                        try
                        {
                            // Descarga el favicon y lo convierte a BitmapImage
                            using (var httpClient = new System.Net.Http.HttpClient())
                            {
                                byte[] faviconBytes = await httpClient.GetByteArrayAsync(faviconUrl);
                                tab.Favicon = new BitmapImage();
                                tab.Favicon.BeginInit();
                                tab.Favicon.StreamSource = new MemoryStream(faviconBytes);
                                tab.Favicon.EndInit();

                                tab.CapturedData.FaviconBase64 = Convert.ToBase64String(faviconBytes); // Guarda como Base64 para Gemini
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al descargar o procesar el favicon: {ex.Message}");
                            tab.Favicon = null;
                            tab.CapturedData.FaviconBase64 = string.Empty;
                        }
                    }
                    else
                    {
                        tab.Favicon = null;
                        tab.CapturedData.FaviconBase64 = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al obtener el título o el favicon: {ex.Message}");
                    tab.Title = "Error";
                    tab.Favicon = null;
                    tab.CapturedData.FaviconBase64 = string.Empty;
                }
            }
        }

        // Controlador de eventos para el evento SourceChanged de WebView2
        private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView != null)
            {
                var tab = TabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(t => t.WebViewInstance == webView);
                if (tab != null && SelectedTabItem == tab)
                {
                    AddressBar.Text = webView.Source.ToString(); // Actualiza la barra de direcciones si es la pestaña seleccionada
                }
            }
        }

        // Controlador de eventos para el evento CoreWebView2InitializationCompleted de WebView2
        private async void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView != null && webView.CoreWebView2 != null)
            {
                // Configura los ajustes de WebView2
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false; // Deshabilitar herramientas de desarrollo por defecto
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                // Aplica el filtro del bloqueador de anuncios si está habilitado
                if (IsAdBlockerEnabled)
                {
                    // Registra el manejador de eventos para interceptar solicitudes
                    webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                    // Añade un filtro general para todas las solicitudes (luego se filtra en el manejador)
                    webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                }

                // Agrega scripts para ejecutar en la creación del documento para las extensiones habilitadas
                foreach (var extension in ExtensionManager.Extensions)
                {
                    if (extension.IsEnabled)
                    {
                        string scriptContent = extension.LoadScriptContent();
                        if (!string.IsNullOrEmpty(scriptContent))
                        {
                            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(scriptContent);
                        }
                    }
                }
            }
        }

        // Controlador de eventos para el evento SelectionChanged de TabControl (para pestañas agrupadas)
        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            if (BrowserTabs.SelectedItem is TabItemData selectedTab)
            {
                SelectedTabItem = selectedTab; // Actualiza SelectedTabItem
                AddressBar.Text = selectedTab.Url; // Actualiza la barra de direcciones
                UpdateNavigationButtons(); // Actualiza los estados de los botones de navegación
                ActivateTab(selectedTab); // Asegura que la pestaña se active si estaba suspendida
            }
            else
            {
                AddressBar.Text = "";
                UpdateNavigationButtons();
            }
        }

        // Actualiza la visibilidad y el estado de los controles del navegador
        private void UpdateBrowserControls()
        {
            IsFindBarVisible = false; // Oculta la barra de búsqueda
            FindTextBox.Text = ""; // Borra el texto de búsqueda
            FindResultsTextBlock.Text = ""; // Borra los resultados de búsqueda
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.StopFindInPage(); // Detiene cualquier operación de búsqueda activa

            UpdateNavigationButtons(); // Actualiza los botones de navegación
        }

        // Actualiza el estado habilitado/deshabilitado de los botones de retroceso y avance
        private void UpdateNavigationButtons()
        {
            GoBackButton.IsEnabled = SelectedTabItem?.WebViewInstance?.CanGoBack ?? false;
            GoForwardButton.IsEnabled = SelectedTabItem?.WebViewInstance?.CanGoForward ?? false;
        }

        // Controlador de eventos para el clic del botón Buscar (alterna la visibilidad de la barra de búsqueda)
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            IsFindBarVisible = !IsFindBarVisible; // Alterna la visibilidad
            if (IsFindBarVisible)
            {
                FindTextBox.Focus(); // Enfoca el cuadro de texto de búsqueda
                FindTextBox.SelectAll(); // Selecciona todo el texto existente
            }
            else
            {
                SelectedTabItem?.WebViewInstance?.CoreWebView2?.StopFindInPage(); // Detiene la búsqueda
                FindResultsTextBlock.Text = ""; // Borra los resultados
            }
        }

        // Controlador de eventos para el clic del botón Cerrar barra de búsqueda
        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            IsFindBarVisible = false;
            FindTextBox.Text = "";
            FindResultsTextBlock.Text = "";
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.StopFindInPage(); // Detiene la operación de búsqueda
        }

        // Controlador de eventos para el evento TextChanged de FindTextBox
        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FindInPage(FindTextBox.Text); // Realiza la búsqueda a medida que cambia el texto
        }

        // Realiza la operación de búsqueda en la página
        private void FindInPage(string searchText)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                if (!string.IsNullOrEmpty(searchText))
                {
                    SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(
                        searchText,
                        CoreWebView2FindInPageKind.None, // Inicia una nueva búsqueda
                        false, // No sensible a mayúsculas y minúsculas
                        (sender, args) =>
                        {
                            FindResultsTextBlock.Text = $"{args.ActiveMatch}/{args.Matches}"; // Actualiza el texto de los resultados
                        }
                    );
                }
                else
                {
                    SelectedTabItem.WebViewInstance.CoreWebView2.StopFindInPage(); // Detiene la búsqueda si el texto de búsqueda está vacío
                    FindResultsTextBlock.Text = "";
                }
            }
        }

        // Controlador de eventos para el clic del botón Siguiente búsqueda
        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null && !string.IsNullOrEmpty(FindTextBox.Text))
            {
                SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(
                    FindTextBox.Text,
                    CoreWebView2FindInPageKind.Next, // Busca la siguiente ocurrencia
                    false,
                    (sender, args) =>
                    {
                        FindResultsTextBlock.Text = $"{args.ActiveMatch}/{args.Matches}";
                    }
                );
            }
        }

        // Controlador de eventos para el clic del botón Búsqueda anterior
        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null && !string.IsNullOrEmpty(FindTextBox.Text))
            {
                SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(
                    FindTextBox.Text,
                    CoreWebView2FindInPageKind.Previous, // Busca la ocurrencia anterior
                    false,
                    (sender, args) =>
                    {
                        FindResultsTextBlock.Text = $"{args.ActiveMatch}/{args.Matches}";
                    }
                );
            }
        }

        // Controlador de eventos para el clic del botón Historial (abre HistoryWindow)
        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que HistoryWindow exista en tu proyecto y sea accesible
            var historyWindow = new HistoryWindow();
            historyWindow.ShowDialog(); // ShowDialog bloquea la ventana principal hasta que se cierra
        }

        // Controlador de eventos para el clic del botón Marcadores (abre BookmarksWindow)
        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que BookmarksWindow exista en tu proyecto y sea accesible
            var bookmarksWindow = new BookmarksWindow();
            bookmarksWindow.ShowDialog();
        }

        // Controlador de eventos para el clic del botón Administrador de contraseñas (abre PasswordManagerWindow)
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que PasswordManagerWindow exista en tu proyecto y sea accesible
            var passwordManagerWindow = new PasswordManagerWindow();
            passwordManagerWindow.ShowDialog();
        }

        // Controlador de eventos para el clic del botón Extracción de datos (abre DataExtractionWindow)
        private async void DataExtractionButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                // Asegúrate de que DataExtractionWindow exista en tu proyecto y sea accesible
                var dataExtractionWindow = new DataExtractionWindow(SelectedTabItem.WebViewInstance.CoreWebView2);
                dataExtractionWindow.Show();
            }
            else
            {
                MessageBox.Show("No hay una pestaña activa para extraer datos.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Controlador de eventos para el clic del botón Configuración (abre SettingsWindow)
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que SettingsWindow exista en tu proyecto y sea accesible
            var settingsWindow = new SettingsWindow();
            // Pasa las configuraciones actuales a la ventana de configuración
            settingsWindow.HomePage = _defaultHomePage;
            settingsWindow.IsAdBlockerEnabled = IsAdBlockerEnabled;
            settingsWindow.IsTabSuspensionEnabled = IsTabSuspensionEnabled;
            settingsWindow.DefaultSearchEngine = ConfigurationManager.AppSettings[DefaultSearchEngineSettingKey] ?? "https://www.google.com/search?q=";
            settingsWindow.RestoreSessionOnStartup = bool.Parse(ConfigurationManager.AppSettings[RestoreSessionSettingKey] ?? "false");

            if (settingsWindow.ShowDialog() == true) // Muestra la ventana de diálogo y espera el resultado
            {
                // Si el usuario guardó los cambios, actualiza las propiedades de MainWindow
                _defaultHomePage = settingsWindow.HomePage;
                IsAdBlockerEnabled = settingsWindow.IsAdBlockerEnabled;
                IsTabSuspensionEnabled = settingsWindow.IsTabSuspensionEnabled;
                // Actualiza el motor de búsqueda por defecto
                ConfigurationManager.AppSettings[DefaultSearchEngineSettingKey] = settingsWindow.DefaultSearchEngine;
                // Actualiza la configuración de restauración de sesión
                ConfigurationManager.AppSettings[RestoreSessionSettingKey] = settingsWindow.RestoreSessionOnStartup.ToString();

                SaveSettings(); // Guarda la configuración después de cerrar el diálogo
                ApplyAdBlockerSettings(); // Vuelve a aplicar la configuración del bloqueador de anuncios
                ApplyTheme(); // Vuelve a aplicar el tema si es necesario
            }
        }

        // Controlador de eventos para el clic del botón Picture-in-Picture (PIP)
        private async void PipButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null && !string.IsNullOrEmpty(SelectedTabItem.Url))
            {
                string videoUrl = SelectedTabItem.Url;

                try
                {
                    // Asegúrate de que PipWindow exista en tu proyecto y sea accesible
                    // Necesitas pasar el entorno de CoreWebView2 para que PipWindow pueda crear su propio WebView2
                    var pipWindow = new PipWindow(videoUrl, SelectedTabItem.WebViewInstance.CoreWebView2.Environment);
                    pipWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al abrir la ventana PIP: {ex.Message}", "Error PIP", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No hay una URL válida para abrir en PIP.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Controlador de eventos para el evento ContextMenuOpening de WebView2 (puede personalizar el menú contextual)
        private void WebView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Puedes agregar elementos personalizados al menú contextual aquí si es necesario
            // Ejemplo: e.Handled = true; para evitar el menú contextual predeterminado de WebView2
        }

        // Controlador de eventos para el clic del botón Leer en voz alta (alterna la conversión de texto a voz)
        private async void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                if (_isReadingAloud)
                {
                    _speechSynthesizer?.SpeakAsyncCancelAll(); // Detiene la voz actual
                    _isReadingAloud = false;
                    ReadAloudButton.ToolTip = "Leer en voz alta"; // Cambia la información sobre herramientas
                }
                else
                {
                    try
                    {
                        // JavaScript para extraer todo el texto visible de la página
                        string extractTextScript = @"
                            (function() {
                                var bodyText = document.body.innerText;
                                return bodyText;
                            })();
                        ";
                        string pageTextJson = await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync(extractTextScript);
                        string pageText = JsonSerializer.Deserialize<string>(pageTextJson) ?? "";

                        if (!string.IsNullOrEmpty(pageText))
                        {
                            _speechSynthesizer?.SpeakAsync(pageText); // Inicia la lectura
                            _isReadingAloud = true;
                            ReadAloudButton.ToolTip = "Detener lectura"; // Cambia la información sobre herramientas
                        }
                        else
                        {
                            MessageBox.Show("No se pudo extraer texto de la página para leer en voz alta.", "Leer en Voz Alta", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al leer en voz alta: {ex.Message}", "Error de Lectura", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Controlador de eventos para el clic del botón Modo lector (alterna la vista simplificada)
        private async void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                try
                {
                    if (SelectedTabItem.IsReaderMode)
                    {
                        // Sale del modo lector: navega de nuevo a la URL original
                        if (!string.IsNullOrEmpty(SelectedTabItem.Url))
                        {
                            SelectedTabItem.WebViewInstance.Source = new Uri(SelectedTabItem.Url);
                        }
                        SelectedTabItem.IsReaderMode = false;
                        ReaderModeButton.ToolTip = "Modo lector";
                    }
                    else
                    {
                        // CSS para el estilo del modo lector
                        string readerModeCss = @"
                            body {
                                font-family: 'Georgia', serif;
                                line-height: 1.6;
                                max-width: 800px;
                                margin: 0 auto;
                                padding: 20px;
                                background-color: #f9f9f9;
                                color: #333;
                            }
                            p {
                                margin-bottom: 1em;
                            }
                            img {
                                max-width: 100%;
                                height: auto;
                                display: block;
                                margin: 1em auto;
                            }
                            h1, h2, h3, h4, h5, h6 {
                                font-family: 'Helvetica Neue', sans-serif;
                                color: #1a1a1a;
                                margin-top: 1.5em;
                                margin-bottom: 0.5em;
                            }
                            a {
                                color: #007bff;
                                text-decoration: none;
                            }
                            a:hover {
                                text-decoration: underline;
                            }
                            header, footer, nav, aside, .sidebar, .comments, .ads, .related-posts {
                                display: none !important;
                            }
                        ";

                        // JavaScript para inyectar CSS y eliminar elementos irrelevantes para el modo lector
                        string readerModeScript = @"
                            (function() {
                                var style = document.createElement('style');
                                style.textContent = `" + readerModeCss.Replace("`", "\\`") + @"`;
                                document.head.appendChild(style);

                                var elementsToRemove = 'header, footer, nav, aside, .sidebar, .comments, .ads, .related-posts, script, style';
                                document.querySelectorAll(elementsToRemove).forEach(function(el) {
                                    el.parentNode.removeChild(el);
                                });

                                document.body.style.margin = '0 auto';
                                document.body.style.maxWidth = '800px';
                                document.body.style.padding = '20px';
                            })();
                        ";
                        await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync(readerModeScript);
                        SelectedTabItem.IsReaderMode = true;
                        ReaderModeButton.ToolTip = "Desactivar modo lector";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cambiar el modo lector: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Controlador de eventos para el clic del botón Incógnito (muestra un mensaje de información)
        private void IncognitoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("El modo incógnito abre una nueva ventana donde la actividad de navegación no se guarda en el historial ni en las cookies después de cerrar la ventana.", "Modo Incógnito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Controlador de eventos para el clic del elemento de menú Extensión (alterna el estado de la extensión)
        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is CustomExtension extension)
            {
                extension.IsEnabled = !extension.IsEnabled;
                ApplyExtension(extension); // Aplica los cambios de la extensión
            }
        }

        // Aplica o desaplica el script de una extensión al WebView2 actual
        private async void ApplyExtension(CustomExtension extension)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                string scriptContent = extension.LoadScriptContent();
                if (!string.IsNullOrEmpty(scriptContent))
                {
                    if (extension.IsEnabled)
                    {
                        await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync(scriptContent);
                    }
                    else
                    {
                        // Si la extensión se desactiva, es posible que necesites recargar la página
                        // o ejecutar un script para revertir los cambios de la extensión.
                        MessageBox.Show($"La extensión '{extension.Name}' ha sido desactivada. Puede que necesite recargar la página para ver los cambios.", "Extensiones", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        // Controlador de eventos para el clic del botón Administrar extensiones (abre ExtensionsWindow)
        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que ExtensionsWindow exista en tu proyecto y sea accesible
            var extensionsWindow = new ExtensionsWindow(ExtensionManager);
            extensionsWindow.ShowDialog();
        }

        // Controlador de eventos para el evento WebMessageReceived de WebView2 (maneja mensajes de scripts inyectados)
        private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView == null) return;

            string message = e.WebMessageAsJson; // Obtiene el mensaje como JSON

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(message))
                {
                    if (doc.RootElement.TryGetProperty("type", out JsonElement typeElement))
                    {
                        string messageType = typeElement.GetString() ?? "";

                        switch (messageType)
                        {
                            case "extractedText":
                                if (doc.RootElement.TryGetProperty("text", out JsonElement textElement))
                                {
                                    string extractedText = textElement.GetString() ?? "";
                                    Console.WriteLine($"Texto Extraído: {extractedText.Substring(0, Math.Min(extractedText.Length, 200))}...");

                                    var tab = TabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(t => t.WebViewInstance == webView);
                                    if (tab != null)
                                    {
                                        tab.CapturedData.ExtractedText = extractedText; // Almacena el texto extraído en los datos de la pestaña
                                    }
                                }
                                break;
                            case "passwordDetected":
                                if (doc.RootElement.TryGetProperty("url", out JsonElement urlElement) &&
                                    doc.RootElement.TryGetProperty("username", out JsonElement usernameElement) &&
                                    doc.RootElement.TryGetProperty("password", out JsonElement passwordElement))
                                {
                                    string url = urlElement.GetString() ?? "";
                                    string username = usernameElement.GetString() ?? "";
                                    string password = passwordElement.GetString() ?? "";

                                    var result = MessageBox.Show($"¿Deseas guardar la contraseña para {username} en {url}?", "Guardar Contraseña", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        PasswordManager.SavePassword(url, username, password); // Guarda la contraseña usando PasswordManager
                                        MessageBox.Show("Contraseña guardada exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                                    }
                                }
                                break;
                            case "languageDetected":
                                if (doc.RootElement.TryGetProperty("language", out JsonElement langElement))
                                {
                                    string detectedLanguage = langElement.GetString() ?? "es";
                                    Console.WriteLine($"Idioma detectado por script: {detectedLanguage}");
                                    // Aquí podrías usar el idioma detectado, por ejemplo, para ofrecer traducción.
                                }
                                break;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error al analizar el mensaje de WebView2 como JSON: {ex.Message}");
                Console.WriteLine($"Mensaje recibido de WebView2: {message}");
            }
        }

        // Controlador de eventos para el evento DownloadStarting de WebView2 (maneja las descargas de archivos)
        private void WebView_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            e.Cancel = true; // Cancela el comportamiento de descarga predeterminado

            string suggestedFileName = Path.GetFileName(e.ResultFilePath); // Obtiene el nombre de archivo sugerido

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.FileName = suggestedFileName;
            saveFileDialog.Title = "Guardar archivo";

            if (saveFileDialog.ShowDialog() == true)
            {
                e.ResultFilePath = saveFileDialog.FileName; // Establece la ruta de descarga personalizada
                e.Cancel = false; // Permite que la descarga continúe

                DownloadProgressBarVisibility = Visibility.Visible; // Muestra la barra de progreso
                DownloadProgress = 0; // Reinicia el progreso

                // Actualiza la barra de progreso a medida que se reciben los bytes
                e.DownloadOperation.BytesReceivedChanged += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (e.DownloadOperation.TotalBytesToReceive > 0)
                        {
                            DownloadProgress = (double)e.DownloadOperation.BytesReceived * 100 / e.DownloadOperation.TotalBytesToReceive;
                        }
                    });
                };

                // Maneja los cambios de estado de la descarga (completada, interrumpida)
                e.DownloadOperation.StateChanged += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        switch (e.DownloadOperation.State)
                        {
                            case CoreWebView2DownloadState.Completed:
                                MessageBox.Show($"Descarga completada: {e.ResultFilePath}", "Descarga", MessageBoxButton.OK, MessageBoxImage.Information);
                                DownloadProgressBarVisibility = Visibility.Collapsed; // Oculta la barra de progreso
                                break;
                            case CoreWebView2DownloadState.Interrupted:
                                MessageBox.Show($"Descarga interrumpida: {e.DownloadOperation.InterruptReason}", "Descarga Fallida", MessageBoxButton.OK, MessageBoxImage.Error);
                                DownloadProgressBarVisibility = Visibility.Collapsed; // Oculta la barra de progreso
                                break;
                        }
                    });
                };
            }
            else
            {
                e.Cancel = true; // Cancela la descarga si el usuario no selecciona una ruta
            }
        }

        // Comando para agregar un nuevo grupo de pestañas
        public ICommand AddTabGroupCommand => new RelayCommand(_ => AddNewTabGroup());

        // Agrega un nuevo grupo de pestañas
        private void AddNewTabGroup()
        {
            var newGroup = new TabGroup($"Grupo {TabGroupManager.TabGroups.Count + 1}");
            TabGroupManager.AddGroup(newGroup); // Agrega un nuevo grupo al administrador
            TabGroupManager.SelectedTabGroup = newGroup; // Selecciona el nuevo grupo
            AddNewTab(_defaultHomePage); // Agrega una nueva pestaña al nuevo grupo
            BrowserTabs.ItemsSource = newGroup.TabsInGroup; // Actualiza el ItemsSource del TabControl
        }

        // Comando para seleccionar un grupo de pestañas
        public ICommand SelectTabGroupCommand => new RelayCommand(parameter =>
        {
            if (parameter is TabGroup selectedGroup)
            {
                TabGroupManager.SelectedTabGroup = selectedGroup; // Establece el grupo seleccionado
                BrowserTabs.ItemsSource = selectedGroup.TabsInGroup; // Actualiza el ItemsSource del TabControl
                SelectedTabItem = selectedGroup.TabsInGroup.FirstOrDefault(); // Selecciona la primera pestaña del grupo
            }
        });

        // Controlador de eventos para el evento SourceInitialized de MainWindow (para arrastrar la ventana personalizada)
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            HwndSource? source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                source.AddHook(WndProc); // Agrega un hook para el procedimiento de ventana personalizado
            }
        }

        // Procedimiento de ventana personalizado para manejar mensajes del área no cliente (por ejemplo, arrastrar)
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCLBUTTONDOWN:
                    if (wParam.ToInt32() == HT_CAPTION)
                    {
                        handled = true; // Maneja el clic en el área del título para arrastrar
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        // Controlador de eventos para el clic del botón Gemini (captura datos de la página y abre GeminiDataViewerWindow)
        private async void GeminiButton_Click(object sender, RoutedEventArgs e)
        {
            var capturedDataList = new ObservableCollection<CapturedPageData>();

            if (SelectedTabItem != null)
            {
                // Asegúrate de que CoreWebView2 esté inicializado
                if (SelectedTabItem.WebViewInstance.CoreWebView2 == null)
                {
                    await SelectedTabItem.WebViewInstance.EnsureCoreWebView2Async(null);
                }

                try
                {
                    // Extrae el contenido de texto de la página
                    string extractedText = await GetPageText(SelectedTabItem.WebViewInstance.CoreWebView2);
                    SelectedTabItem.CapturedData.ExtractedText = extractedText;

                    // Captura una captura de pantalla de la página
                    string screenshotBase64 = await CaptureScreenshotAsync(SelectedTabItem.WebViewInstance);
                    SelectedTabItem.CapturedData.ScreenshotBase64 = screenshotBase64;

                    // Convierte el favicon a Base64 si está disponible
                    if (string.IsNullOrEmpty(SelectedTabItem.CapturedData.FaviconBase64) && SelectedTabItem.Favicon != null)
                    {
                        SelectedTabItem.CapturedData.FaviconBase64 = ConvertBitmapImageToBase64(SelectedTabItem.Favicon);
                    }

                    // Rellena los datos capturados con la URL y el título
                    SelectedTabItem.CapturedData.Url = SelectedTabItem.Url;
                    SelectedTabItem.CapturedData.Title = SelectedTabItem.Title;

                    capturedDataList.Add(SelectedTabItem.CapturedData); // Agrega los datos capturados a la lista
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al capturar datos de la página para Gemini: {ex.Message}", "Error de Captura", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("No hay una pestaña web activa para enviar a Gemini.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Abre GeminiDataViewerWindow para mostrar los datos capturados y obtener la pregunta del usuario
            var geminiViewerWindow = new GeminiDataViewerWindow(capturedDataList);
            geminiViewerWindow.Owner = this; // Establece el propietario en la ventana principal

            if (geminiViewerWindow.ShowDialog() == true)
            {
                string userQuestion = geminiViewerWindow.UserQuestion; // Obtiene la pregunta del usuario

                MessageBox.Show($"Datos enviados a Gemini con la pregunta: '{userQuestion}'", "Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
                IsGeminiModeActive = true; // Activa el modo Gemini
            }
            else
            {
                MessageBox.Show("Envío a Gemini cancelado.", "Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
                IsGeminiModeActive = false; // Desactiva el modo Gemini
            }
        }

        // Extrae el contenido de texto de una página de WebView2
        private async Task<string> GetPageText(CoreWebView2 webView)
        {
            if (webView == null) return string.Empty;

            try
            {
                // JavaScript para extraer texto de elementos de contenido comunes o del cuerpo
                string script = @"
                    (function() {
                        var selectors = ['article', 'main', 'body'];
                        for (var i = 0; i < selectors.length; i++) {
                            var element = document.querySelector(selectors[i]);
                            if (element && element.innerText) {
                                return element.innerText;
                            }
                        }
                        return document.body.innerText;
                    })();
                ";
                string pageTextJson = await webView.ExecuteScriptAsync(script);
                return JsonSerializer.Deserialize<string>(pageTextJson) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al extraer texto de la página: {ex.Message}");
                return string.Empty;
            }
        }

        // Captura una captura de pantalla del contenido de WebView2
        private async Task<string> CaptureScreenshotAsync(WebView2 webView)
        {
            if (webView.CoreWebView2 == null)
            {
                await webView.EnsureCoreWebView2Async(null);
            }

            try
            {
                // Obtiene el tamaño del contenido usando JavaScript
                var contentSizeJson = await webView.CoreWebView2.ExecuteScriptAsync(
                    "(function() { " +
                    "  var body = document.body, html = document.documentElement;" +
                    "  var height = Math.max( body.scrollHeight, body.offsetHeight, html.clientHeight, html.scrollHeight, html.offsetHeight );" +
                    "  var width = Math.max( body.scrollWidth, body.offsetWidth, html.clientWidth, html.scrollWidth, html.offsetWidth );" +
                    "  return { width: width, height: height };" +
                    "})()"
                );

                using (JsonDocument doc = JsonDocument.Parse(contentSizeJson))
                {
                    int contentWidth = doc.RootElement.GetProperty("width").GetInt32();
                    int contentHeight = doc.RootElement.GetProperty("height").GetInt32();

                    // Limita las dimensiones para evitar un uso excesivo de memoria
                    const int MAX_DIMENSION = 8000; // Define un límite razonable
                    contentWidth = Math.Min(contentWidth, MAX_DIMENSION);
                    contentHeight = Math.Min(contentHeight, MAX_DIMENSION);

                    // Almacena las dimensiones originales de WebView
                    double originalWidth = webView.Width;
                    double originalHeight = webView.Height;

                    // Redimensiona temporalmente WebView al tamaño del contenido para una captura de pantalla completa
                    // Usamos Dispatcher.Invoke para asegurar que la operación se realice en el hilo de la UI
                    await Dispatcher.InvokeAsync(() =>
                    {
                        webView.Width = contentWidth;
                        webView.Height = contentHeight;
                        webView.Measure(new Size(contentWidth, contentHeight));
                        webView.Arrange(new Rect(0, 0, contentWidth, contentHeight));
                    });

                    await Task.Delay(50); // Pequeño retraso para permitir la renderización

                    using (MemoryStream stream = new MemoryStream())
                    {
                        await webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream); // Captura la vista previa

                        // Restaura las dimensiones originales de WebView
                        await Dispatcher.InvokeAsync(() =>
                        {
                            webView.Width = originalWidth;
                            webView.Height = originalHeight;
                            webView.Measure(new Size(originalWidth, originalHeight));
                            webView.Arrange(new Rect(0, 0, originalWidth, originalHeight));
                        });

                        byte[] imageBytes = stream.ToArray();
                        return "data:image/png;base64," + Convert.ToBase64String(imageBytes); // Devuelve la cadena Base64
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al capturar la captura de pantalla: {ex.Message}");
                return string.Empty;
            }
        }

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
