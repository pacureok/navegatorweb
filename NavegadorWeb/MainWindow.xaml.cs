using Microsoft.Web.WebView2.Core; // ¡Esta línea es CRUCIAL y debe estar al inicio!
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
using System.Speech.Synthesis;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics; // Necesario para Process.Start
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Timers;

namespace NavegadorWeb
{
    // La clase partial MainWindow ya es generada por el compilador para incluir los elementos XAML.
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _defaultHomePage = "https://www.google.com";
        private const string HomePageSettingKey = "DefaultHomePage";
        private const string AdBlockerSettingKey = "AdBlockerEnabled";
        private const string DefaultSearchEngineSettingKey = "DefaultSearchEngine";
        private const string TabSuspensionSettingKey = "TabSuspensionEnabled";
        private const string RestoreSessionSettingKey = "RestoreSessionOnStartup";
        private const string LastSessionUrlsSettingKey = "LastSessionUrls";
        private const string LastSessionTabGroupsSettingKey = "LastSessionTabGroups"; // Nueva clave
        private const string LastSelectedTabGroupSettingKey = "LastSelectedTabGroup"; // Nueva clave

        private bool _isAdBlockerEnabled;
        public bool IsAdBlockerEnabled
        {
            get => _isAdBlockerEnabled;
            set
            {
                if (_isAdBlockerEnabled != value)
                {
                    _isAdBlockerEnabled = value;
                    OnPropertyChanged(nameof(IsAdBlockerEnabled));
                    ApplyAdBlockerSettings();
                }
            }
        }

        private bool _isGeminiModeActive;
        public bool IsGeminiModeActive
        {
            get => _isGeminiModeActive;
            set
            {
                if (_isGeminiModeActive != value)
                {
                    _isGeminiModeActive = value;
                    OnPropertyChanged(nameof(IsGeminiModeActive));
                    ApplyTheme(); // Cambia el tema al activar/desactivar el modo Gemini
                }
            }
        }

        private bool _isFindBarVisible;
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

        private string _findSearchText = "";
        public string FindSearchText
        {
            get => _findSearchText;
            set
            {
                if (_findSearchText != value)
                {
                    _findSearchText = value;
                    OnPropertyChanged(nameof(FindSearchText));
                    FindInPage(_findSearchText);
                }
            }
        }

        private string _findResultsText = "";
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

        private TabItemData? _selectedTabItem;
        public TabItemData? SelectedTabItem
        {
            get => _selectedTabItem;
            set
            {
                if (_selectedTabItem != value)
                {
                    _selectedTabItem = value;
                    OnPropertyChanged(nameof(SelectedTabItem));
                    UpdateBrowserControls();
                }
            }
        }

        // Propiedades para la barra de progreso
        private double _downloadProgress;
        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged(nameof(DownloadProgress));
            }
        }

        private Visibility _downloadProgressBarVisibility = Visibility.Collapsed;
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
        public ExtensionManager ExtensionManager { get; private set; } // Añadir ExtensionManager

        // Speech Synthesizer para Read Aloud
        private SpeechSynthesizer? _speechSynthesizer;
        private bool _isReadingAloud = false;

        // Propiedad para almacenar los datos capturados para Gemini
        public ObservableCollection<CapturedPageData> CapturedPagesForGemini { get; set; } = new ObservableCollection<CapturedPageData>();


        // Implementación explícita del evento PropertyChanged para INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Constantes para el tamaño mínimo de la ventana
        private const double MIN_WIDTH = 800;
        private const double MIN_HEIGHT = 600;

        // Timer para la suspensión de pestañas
        private System.Timers.Timer _tabSuspensionTimer;
        private TimeSpan _tabSuspensionDelay = TimeSpan.FromMinutes(5); // 5 minutos de inactividad
        private bool _isTabSuspensionEnabled;
        public bool IsTabSuspensionEnabled
        {
            get => _isTabSuspensionEnabled;
            set
            {
                if (_isTabSuspensionEnabled != value)
                {
                    _isTabSuspensionEnabled = value;
                    OnPropertyChanged(nameof(IsTabSuspensionEnabled));
                    if (_isTabSuspensionEnabled)
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


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this; // Establecer el DataContext a la propia ventana

            TabGroupManager = new TabGroupManager();
            ExtensionManager = new ExtensionManager(); // Inicializar ExtensionManager

            LoadSettings(); // Cargar configuraciones al inicio
            ApplyAdBlockerSettings(); // Aplicar el estado inicial del AdBlocker
            ApplyTheme(); // Aplicar el tema inicial

            // Inicializar el sintetizador de voz
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();

            // Configurar el temporizador de suspensión de pestañas
            _tabSuspensionTimer = new System.Timers.Timer(_tabSuspensionDelay.TotalMilliseconds);
            _tabSuspensionTimer.Elapsed += TabSuspensionTimer_Elapsed;
            _tabSuspensionTimer.AutoReset = true; // Para que se repita
            if (IsTabSuspensionEnabled)
            {
                StartTabSuspensionTimer();
            }

            // Vincular el evento PreviewMouseMove para reiniciar el temporizador
            this.PreviewMouseMove += MainWindow_UserActivity;
            this.PreviewKeyDown += MainWindow_UserActivity;
        }


        // Métodos para la suspensión de pestañas
        private void StartTabSuspensionTimer()
        {
            _tabSuspensionTimer.Start();
        }

        private void StopTabSuspensionTimer()
        {
            _tabSuspensionTimer.Stop();
        }

        private void MainWindow_UserActivity(object sender, EventArgs e)
        {
            // Reiniciar el temporizador de inactividad en cada actividad del usuario
            if (IsTabSuspensionEnabled)
            {
                _tabSuspensionTimer.Stop();
                _tabSuspensionTimer.Start();
            }
        }

        private async void TabSuspensionTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // Este método se ejecuta en un hilo diferente, así que necesitamos usar el Dispatcher
            await Dispatcher.InvokeAsync(async () =>
            {
                foreach (var group in TabGroupManager.TabGroups)
                {
                    foreach (var tab in group.TabsInGroup)
                    {
                        if (tab != SelectedTabItem && tab.WebViewInstance != null && tab.WebViewInstance.CoreWebView2 != null)
                        {
                            // Suspender la pestaña
                            tab.IsSuspended = true;
                            tab.LastSuspendedUrl = tab.WebViewInstance.Source.ToString(); // Guardar la URL actual
                            tab.WebViewInstance.CoreWebView2.Stop(); // Detener la carga
                            tab.WebViewInstance.Source = new Uri("about:blank"); // Cargar una página en blanco
                            Console.WriteLine($"Pestaña suspendida: {tab.Title}");
                        }
                    }
                }
            });
        }

        public async void ActivateTab(TabItemData tab)
        {
            if (tab.IsSuspended)
            {
                tab.IsSuspended = false;
                if (tab.WebViewInstance != null && tab.WebViewInstance.CoreWebView2 != null && !string.IsNullOrEmpty(tab.LastSuspendedUrl))
                {
                    tab.WebViewInstance.Source = new Uri(tab.LastSuspendedUrl);
                    Console.WriteLine($"Pestaña reactivada: {tab.Title}");
                }
            }
            SelectedTabItem = tab;
        }


        private void LoadSettings()
        {
            _defaultHomePage = ConfigurationManager.AppSettings[HomePageSettingKey] ?? "https://www.google.com";
            IsAdBlockerEnabled = bool.Parse(ConfigurationManager.AppSettings[AdBlockerSettingKey] ?? "false");
            IsTabSuspensionEnabled = bool.Parse(ConfigurationManager.AppSettings[TabSuspensionSettingKey] ?? "false");

            if (bool.Parse(ConfigurationManager.AppSettings[RestoreSessionSettingKey] ?? "false"))
            {
                RestoreLastSession();
            }
            else
            {
                AddNewTab(_defaultHomePage); // Abrir una nueva pestaña si no se restaura la sesión
            }
        }

        private void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[HomePageSettingKey].Value = _defaultHomePage;
            config.AppSettings.Settings[AdBlockerSettingKey].Value = IsAdBlockerEnabled.ToString();
            config.AppSettings.Settings[TabSuspensionSettingKey].Value = IsTabSuspensionEnabled.ToString();

            // Guardar URLs de la sesión actual si la restauración está activada
            if (bool.Parse(ConfigurationManager.AppSettings[RestoreSessionSettingKey] ?? "false"))
            {
                SaveCurrentSession();
            }
            else
            {
                // Limpiar la configuración de la sesión si la restauración está desactivada
                config.AppSettings.Settings[LastSessionUrlsSettingKey].Value = "";
                config.AppSettings.Settings[LastSessionTabGroupsSettingKey].Value = "";
                config.AppSettings.Settings[LastSelectedTabGroupSettingKey].Value = "";
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void SaveCurrentSession()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Guardar URLs de todas las pestañas abiertas
            var allUrls = TabGroupManager.TabGroups
                                        .SelectMany(g => g.TabsInGroup)
                                        .Select(tab => tab.WebViewInstance?.Source?.ToString())
                                        .Where(url => !string.IsNullOrEmpty(url) && url != "about:blank")
                                        .ToList();
            config.AppSettings.Settings[LastSessionUrlsSettingKey].Value = JsonSerializer.Serialize(allUrls);

            // Guardar el estado de los grupos de pestañas
            var groupStates = TabGroupManager.TabGroups.Select(g => new TabGroupState
            {
                GroupId = g.GroupId,
                GroupName = g.GroupName,
                TabUrls = g.TabsInGroup.Select(t => t.WebViewInstance?.Source?.ToString()).Where(url => !string.IsNullOrEmpty(url) && url != "about:blank").ToList(),
                SelectedTabUrl = g.SelectedTabItem?.WebViewInstance?.Source?.ToString()
            }).ToList();
            config.AppSettings.Settings[LastSessionTabGroupsSettingKey].Value = JsonSerializer.Serialize(groupStates);

            // Guardar el ID del grupo de pestañas seleccionado
            config.AppSettings.Settings[LastSelectedTabGroupSettingKey].Value = TabGroupManager.SelectedTabGroup?.GroupId ?? "";

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }


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
                        TabGroupManager.TabGroups.Clear(); // Limpiar grupos por defecto

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
                                    await newTab.WebViewInstance.EnsureCoreWebView2Async(null);
                                }
                            }

                            // Seleccionar la pestaña que estaba seleccionada en este grupo
                            if (!string.IsNullOrEmpty(groupState.SelectedTabUrl))
                            {
                                var selectedTab = newGroup.TabsInGroup.FirstOrDefault(t => t.WebViewInstance?.Source?.ToString() == groupState.SelectedTabUrl);
                                if (selectedTab != null)
                                {
                                    newGroup.SelectedTabItem = selectedTab;
                                }
                            }
                        }

                        // Restaurar el grupo de pestañas seleccionado
                        var lastSelectedGroupId = ConfigurationManager.AppSettings[LastSelectedTabGroupSettingKey];
                        var restoredSelectedGroup = TabGroupManager.TabGroups.FirstOrDefault(g => g.GroupId == lastSelectedGroupId);
                        if (restoredSelectedGroup != null)
                        {
                            TabGroupManager.SelectedTabGroup = restoredSelectedGroup;
                            BrowserTabs.ItemsSource = restoredSelectedGroup.TabsInGroup; // Actualizar el ItemsSource
                            SelectedTabItem = restoredSelectedGroup.SelectedTabItem; // Asegurar que la pestaña seleccionada se actualice
                        }
                        else
                        {
                            TabGroupManager.SelectedTabGroup = TabGroupManager.GetDefaultGroup();
                            BrowserTabs.ItemsSource = TabGroupManager.GetDefaultGroup().TabsInGroup;
                            SelectedTabItem = TabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault();
                        }
                    }
                    else
                    {
                        AddNewTab(_defaultHomePage); // Si no hay datos de sesión, abre una nueva pestaña
                    }
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Error al restaurar la sesión: {ex.Message}", "Error de Restauración", MessageBoxButton.OK, MessageBoxImage.Error);
                    AddNewTab(_defaultHomePage);
                }
            }
            else
            {
                AddNewTab(_defaultHomePage); // Si no hay datos de sesión, abre una nueva pestaña
            }
        }


        private void ApplyAdBlockerSettings()
        {
            foreach (var group in TabGroupManager.TabGroups)
            {
                foreach (var tab in group.TabsInGroup)
                {
                    if (tab.WebViewInstance != null && tab.WebViewInstance.CoreWebView2 != null)
                    {
                        if (IsAdBlockerEnabled)
                        {
                            tab.WebViewInstance.CoreWebView2.SetWebResourceContextFilter(
                                CoreWebView2WebResourceContext.Image, CoreWebView2WebResourceContext.Script,
                                CoreWebView2WebResourceContext.Stylesheet, CoreWebView2WebResourceContext.Media,
                                CoreWebView2WebResourceContext.Font);
                        }
                        else
                        {
                            tab.WebViewInstance.CoreWebView2.ClearWebResourceContextFilter();
                        }
                    }
                }
            }
        }


        private void ApplyTheme()
        {
            Resources.Clear();
            if (IsGeminiModeActive)
            {
                Resources.Add("BrowserBackgroundColor", FindResource("GeminiBackgroundColor"));
                Resources.Add("BrowserBackgroundBrush", FindResource("GeminiBackgroundBrush"));
                Resources.Add("BrowserForegroundColor", FindResource("GeminiForegroundColor"));
                Resources.Add("BrowserForegroundBrush", FindResource("GeminiForegroundBrush"));
            }
            else
            {
                Resources.Add("BrowserBackgroundColor", FindResource("DefaultBrowserBackgroundColor"));
                Resources.Add("BrowserBackgroundBrush", FindResource("DefaultBrowserBackgroundBrush"));
                Resources.Add("BrowserForegroundColor", FindResource("DefaultBrowserForegroundColor"));
                Resources.Add("BrowserForegroundBrush", FindResource("DefaultBrowserForegroundBrush"));
            }
            // Asegurarse de que el color de fondo de la ventana principal se actualice
            MainBorder.Background = (SolidColorBrush)Resources["BrowserBackgroundBrush"];
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Maximizar la ventana si estaba maximizada en la última sesión
            if (Application.Current.MainWindow != null &&
                (Application.Current.MainWindow.WindowState == WindowState.Maximized ||
                 (Application.Current.MainWindow.ActualWidth == SystemParameters.WorkArea.Width &&
                  Application.Current.MainWindow.ActualHeight == SystemParameters.WorkArea.Height)))
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                // Restaurar el tamaño y la posición si se guardaron
                if (double.TryParse(ConfigurationManager.AppSettings["WindowWidth"], out double width))
                {
                    this.Width = Math.Max(width, MIN_WIDTH);
                }
                if (double.TryParse(ConfigurationManager.AppSettings["WindowHeight"], out double height))
                {
                    this.Height = Math.Max(height, MIN_HEIGHT);
                }
                if (double.TryParse(ConfigurationManager.AppSettings["WindowLeft"], out double left))
                {
                    this.Left = left;
                }
                if (double.TryParse(ConfigurationManager.AppSettings["WindowTop"], out double top))
                {
                    this.Top = top;
                }
            }

            // Establecer el TabControl.ItemsSource al grupo por defecto inicialmente
            BrowserTabs.ItemsSource = TabGroupManager.GetDefaultGroup().TabsInGroup;
            SelectedTabItem = TabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault();

            // Configurar el estilo de las cabeceras de las pestañas dinámicamente
            BrowserTabs.ItemTemplate = (DataTemplate)this.Resources["TabHeaderTemplate"];

            // Añadir un listener al evento SourceChanged de cada WebView2 existente
            foreach (var group in TabGroupManager.TabGroups)
            {
                foreach (var tab in group.TabsInGroup)
                {
                    tab.WebViewInstance.SourceChanged += WebView_SourceChanged;
                    tab.WebViewInstance.NavigationCompleted += WebView_NavigationCompleted;
                    tab.WebViewInstance.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
                    tab.WebViewInstance.WebMessageReceived += WebView_WebMessageReceived; // Para comunicación con JS
                    tab.WebViewInstance.DownloadStarting += WebView_DownloadStarting;
                }
            }

            // Cargar extensiones (si las hay)
            ExtensionManager.LoadExtensions();
            // Asegurarse de que el DataContext del menú de extensiones esté configurado
            ExtensionsMenuItem.DataContext = ExtensionManager;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Guardar el estado de la ventana al cerrar
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (this.WindowState == WindowState.Maximized)
            {
                config.AppSettings.Settings["WindowWidth"].Value = this.RestoreBounds.Width.ToString();
                config.AppSettings.Settings["WindowHeight"].Value = this.RestoreBounds.Height.ToString();
                config.AppSettings.Settings["WindowLeft"].Value = this.RestoreBounds.Left.ToString();
                config.AppSettings.Settings["WindowTop"].Value = this.RestoreBounds.Top.ToString();
            }
            else
            {
                config.AppSettings.Settings["WindowWidth"].Value = this.Width.ToString();
                config.AppSettings.Settings["WindowHeight"].Value = this.Height.ToString();
                config.AppSettings.Settings["WindowLeft"].Value = this.Left.ToString();
                config.AppSettings.Settings["WindowTop"].Value = this.Top.ToString();
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            SaveSettings(); // Guardar todas las configuraciones al cerrar

            // Dispose del sintetizador de voz
            _speechSynthesizer?.Dispose();

            // Detener y liberar el temporizador
            _tabSuspensionTimer?.Stop();
            _tabSuspensionTimer?.Dispose();

            // Limpiar los WebView2 al cerrar para liberar recursos
            foreach (var group in TabGroupManager.TabGroups)
            {
                foreach (var tab in group.TabsInGroup)
                {
                    tab.WebViewInstance?.Dispose();
                }
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            // Ajustar el borde para ventanas maximizadas
            if (WindowState == WindowState.Maximized)
            {
                MainBorder.BorderThickness = new Thickness(0);
                MainBorder.CornerRadius = new CornerRadius(0);
            }
            else
            {
                MainBorder.BorderThickness = new Thickness(1);
                MainBorder.CornerRadius = new CornerRadius(10);
            }
        }

        // Para permitir el arrastre de la ventana cuando WindowStyle="None"
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // Métodos de control de la ventana
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

        private TabItemData CreateNewTabItem(string url)
        {
            var webView = new WebView2();
            var newTabItem = new TabItemData(webView)
            {
                Title = "Cargando...",
                Url = url,
                CapturedData = new CapturedPageData { Url = url } // Inicializar CapturedPageData
            };

            webView.Source = new Uri(url);
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.SourceChanged += WebView_SourceChanged;
            webView.WebMessageReceived += WebView_WebMessageReceived; // Para comunicación con JS
            webView.DownloadStarting += WebView_DownloadStarting;
            webView.ContextMenuOpening += WebView_ContextMenuOpening;

            // Establecer el estilo de la barra de desplazamiento
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


        private void AddNewTab(string url = "about:blank")
        {
            TabItemData newTabItem = CreateNewTabItem(url);
            TabGroupManager.GetDefaultGroup().AddTab(newTabItem);
            SelectedTabItem = newTabItem;
        }

        private void AddNewTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(_defaultHomePage); // Abrir nueva pestaña con la página de inicio
        }

        private void CloseTabCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = BrowserTabs.Items.Count > 1; // Solo se puede cerrar si hay más de una pestaña
        }

        private void CloseTabCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedTabItem != null)
            {
                var currentGroup = TabGroupManager.GetGroupByTab(SelectedTabItem);
                if (currentGroup != null)
                {
                    currentGroup.RemoveTab(SelectedTabItem);
                    SelectedTabItem.WebViewInstance?.Dispose(); // Liberar recursos del WebView2

                    if (currentGroup.TabsInGroup.Count == 0 && TabGroupManager.TabGroups.Count > 1)
                    {
                        TabGroupManager.RemoveGroup(currentGroup);
                    }

                    // Asegurarse de que siempre haya al menos una pestaña
                    if (TabGroupManager.TabGroups.All(g => g.TabsInGroup.Count == 0))
                    {
                        AddNewTab(_defaultHomePage);
                    }
                    else if (SelectedTabItem == null && currentGroup.TabsInGroup.Any())
                    {
                        SelectedTabItem = currentGroup.TabsInGroup.FirstOrDefault();
                    }
                    else if (SelectedTabItem == null && TabGroupManager.TabGroups.Any())
                    {
                        // Si el grupo actual se vació, seleccionar el primer tab del primer grupo disponible
                        SelectedTabItem = TabGroupManager.TabGroups.First().TabsInGroup.FirstOrDefault();
                        BrowserTabs.ItemsSource = TabGroupManager.SelectedTabGroup?.TabsInGroup; // Actualizar el ItemsSource
                    }
                }
            }
        }

        private void GoBackCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTabItem != null && SelectedTabItem.WebViewInstance.CanGoBack;
        }

        private void GoBackCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SelectedTabItem?.WebViewInstance?.GoBack();
        }

        private void GoForwardCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTabItem != null && SelectedTabItem.WebViewInstance.CanGoForward;
        }

        private void GoForwardCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SelectedTabItem?.WebViewInstance?.GoForward();
        }

        private void RefreshCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedTabItem != null;
        }

        private void RefreshCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SelectedTabItem?.WebViewInstance?.Reload();
        }

        private void HomeCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true; // Siempre se puede ir a la página de inicio
        }

        private void HomeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NavigateToUrl(_defaultHomePage);
        }

        private void SearchCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrWhiteSpace(AddressBar.Text);
        }

        private void SearchCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            NavigateToUrl(AddressBar.Text);
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToUrl(AddressBar.Text);
            }
        }

        private void NavigateButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl(AddressBar.Text);
        }

        private void NavigateToUrl(string url)
        {
            if (SelectedTabItem != null && SelectedTabItem.WebViewInstance != null)
            {
                string fullUrl = url;
                if (!url.Contains("://") && !url.StartsWith("file://") && !url.StartsWith("about:"))
                {
                    // Si no tiene esquema, intenta prefijar con "https://" o buscar en Google
                    if (url.Contains(".")) // Probablemente un dominio
                    {
                        fullUrl = "https://" + url;
                    }
                    else // Probablemente un término de búsqueda
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

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView != null)
            {
                var tab = TabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(t => t.WebViewInstance == webView);
                if (tab != null)
                {
                    tab.Url = webView.Source.ToString();
                    AddressBar.Text = tab.Url; // Actualizar la barra de direcciones

                    // Obtener el título y el favicon de la página
                    UpdateTabTitleAndFavicon(tab, webView);

                    // Desactivar el modo de lectura si no es una página de lectura
                    if (tab.IsReaderMode)
                    {
                        // Verificar si la página sigue siendo apta para el modo de lectura
                        // Esto es complejo y podría requerir volver a analizar el DOM
                        // Por simplicidad, aquí asumimos que al navegar, el modo de lectura se desactiva
                        tab.IsReaderMode = false;
                    }

                    // Inyectar el script para la extracción de texto si la extensión está habilitada
                    var textExtractionExtension = ExtensionManager.Extensions.FirstOrDefault(ext => ext.Id == "text_extraction_extension");
                    if (textExtractionExtension != null && textExtractionExtension.IsEnabled)
                    {
                        string scriptContent = textExtractionExtension.LoadScriptContent();
                        if (!string.IsNullOrEmpty(scriptContent))
                        {
                            webView.CoreWebView2.ExecuteScriptAsync(scriptContent);
                        }
                    }

                    // Inyectar scripts de las extensiones habilitadas
                    foreach (var extension in ExtensionManager.Extensions)
                    {
                        if (extension.IsEnabled)
                        {
                            string scriptContent = extension.LoadScriptContent();
                            if (!string.IsNullOrEmpty(scriptContent))
                            {
                                webView.CoreWebView2.ExecuteScriptAsync(scriptContent);
                            }
                        }
                    }

                    // Después de la navegación, obtener el texto de la página y la captura de pantalla para Gemini
                    // No lo hacemos aquí automáticamente para cada navegación, sino cuando el usuario lo pida explícitamente.
                    // Esto se hará a través del botón "Capture Data for Gemini".
                }
            }
        }

        private async void UpdateTabTitleAndFavicon(TabItemData tab, WebView2 webView)
        {
            if (webView.CoreWebView2 != null)
            {
                try
                {
                    // Obtener el título
                    string title = await webView.CoreWebView2.ExecuteScriptAsync("document.title");
                    tab.Title = title.Replace("\"", ""); // Eliminar comillas dobles

                    // Obtener el favicon
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
                        // Si la URL del favicon es relativa, hacerla absoluta
                        if (!faviconUrl.Contains("://"))
                        {
                            Uri baseUri = new Uri(webView.Source.ToString());
                            Uri absoluteUri = new Uri(baseUri, faviconUrl);
                            faviconUrl = absoluteUri.ToString();
                        }

                        // Descargar el favicon y convertirlo a Base64
                        try
                        {
                            using (var httpClient = new System.Net.Http.HttpClient())
                            {
                                byte[] faviconBytes = await httpClient.GetByteArrayAsync(faviconUrl);
                                tab.Favicon = new BitmapImage();
                                tab.Favicon.BeginInit();
                                tab.Favicon.StreamSource = new MemoryStream(faviconBytes);
                                tab.Favicon.EndInit();

                                // Guardar el favicon en Base64 para GeminiDataViewerWindow
                                tab.CapturedData.FaviconBase64 = Convert.ToBase64String(faviconBytes);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al descargar o procesar el favicon: {ex.Message}");
                            tab.Favicon = null; // O establecer un favicon predeterminado
                            tab.CapturedData.FaviconBase64 = string.Empty;
                        }
                    }
                    else
                    {
                        tab.Favicon = null; // No hay favicon
                        tab.CapturedData.FaviconBase64 = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al obtener título o favicon: {ex.Message}");
                    tab.Title = "Error";
                    tab.Favicon = null;
                    tab.CapturedData.FaviconBase64 = string.Empty;
                }
            }
        }


        private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView != null)
            {
                var tab = TabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(t => t.WebViewInstance == webView);
                if (tab != null && SelectedTabItem == tab)
                {
                    AddressBar.Text = webView.Source.ToString();
                }
            }
        }

        private async void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView != null && webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false; // Deshabilitar DevTools
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false; // Ocultar barra de estado

                // Aplicar el filtro de ad-blocker si está habilitado
                if (IsAdBlockerEnabled)
                {
                    webView.CoreWebView2.SetWebResourceContextFilter(
                        CoreWebView2WebResourceContext.Image, CoreWebView2WebResourceContext.Script,
                        CoreWebView2WebResourceContext.Stylesheet, CoreWebView2WebResourceContext.Media,
                        CoreWebView2WebResourceContext.Font);
                }

                // Inyectar el script de ad-blocker y otros scripts de extensión aquí si son de "RunOnDocumentReady"
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

        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            if (BrowserTabs.SelectedItem is TabItemData selectedTab)
            {
                SelectedTabItem = selectedTab;
                AddressBar.Text = selectedTab.Url;
                UpdateNavigationButtons(); // Actualizar el estado de los botones de navegación
            }
            else
            {
                // Manejar el caso donde no hay pestaña seleccionada (por ejemplo, al cerrar la última)
                AddressBar.Text = "";
                UpdateNavigationButtons();
            }
        }

        private void UpdateBrowserControls()
        {
            // Ocultar la barra de búsqueda si se cambia de pestaña
            IsFindBarVisible = false;
            FindTextBox.Text = "";
            FindResultsTextBlock.Text = "";
            SelectedTabItem?.WebViewInstance?.CoreWebView2.StopFindInPage();

            // Actualizar botones de navegación
            UpdateNavigationButtons();
        }

        private void UpdateNavigationButtons()
        {
            GoBackButton.IsEnabled = SelectedTabItem?.WebViewInstance?.CanGoBack ?? false;
            GoForwardButton.IsEnabled = SelectedTabItem?.WebViewInstance?.CanGoForward ?? false;
        }


        // Funcionalidad de Búsqueda en Página (Find in Page)
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            IsFindBarVisible = true;
            FindTextBox.Focus();
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            IsFindBarVisible = false;
            FindTextBox.Text = "";
            FindResultsTextBlock.Text = "";
            SelectedTabItem?.WebViewInstance?.CoreWebView2.StopFindInPage();
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FindInPage(FindTextBox.Text);
        }

        private void FindInPage(string searchText)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                if (!string.IsNullOrEmpty(searchText))
                {
                    SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(
                        searchText,
                        CoreWebView2FindInPageKind.None,
                        false, // No resaltar todos los resultados
                        (sender, args) =>
                        {
                            if (args.Is  /* Corrected property name */ != null)
                            {
                                FindResultsTextBlock.Text = $"{args.Matches}/{args.TotalMatches}";
                            }
                            else
                            {
                                FindResultsTextBlock.Text = "";
                            }
                        }
                    );
                }
                else
                {
                    SelectedTabItem.WebViewInstance.CoreWebView2.StopFindInPage();
                    FindResultsTextBlock.Text = "";
                }
            }
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null && !string.IsNullOrEmpty(FindTextBox.Text))
            {
                SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(
                    FindTextBox.Text,
                    CoreWebView2FindInPageKind.Next,
                    false, // No resaltar todos los resultados
                    (sender, args) =>
                    {
                        if (args.Is /* Corrected property name */ != null)
                        {
                            FindResultsTextBlock.Text = $"{args.Matches}/{args.TotalMatches}";
                        }
                        else
                        {
                            FindResultsTextBlock.Text = "";
                        }
                    }
                );
            }
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null && !string.IsNullOrEmpty(FindTextBox.Text))
            {
                SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(
                    FindTextBox.Text,
                    CoreWebView2FindInPageKind.Previous,
                    false, // No resaltar todos los resultados
                    (sender, args) =>
                    {
                        if (args.Is /* Corrected property name */ != null)
                        {
                            FindResultsTextBlock.Text = $"{args.Matches}/{args.TotalMatches}";
                        }
                        else
                        {
                            FindResultsTextBlock.Text = "";
                        }
                    }
                );
            }
        }

        // Historial de Navegación
        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Implementación de la ventana de historial
            var historyWindow = new HistoryWindow();
            historyWindow.ShowDialog();
        }

        // Favoritos
        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            // Implementación de la ventana de favoritos
            var bookmarksWindow = new BookmarksWindow();
            bookmarksWindow.ShowDialog();
        }

        // Gestor de Contraseñas
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            var passwordManagerWindow = new PasswordManagerWindow();
            passwordManagerWindow.ShowDialog();
        }

        // Extractor de Datos
        private void DataExtractionButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                var dataExtractionWindow = new DataExtractionWindow(SelectedTabItem.WebViewInstance.CoreWebView2);
                dataExtractionWindow.Show();
            }
            else
            {
                MessageBox.Show("No hay una pestaña activa para extraer datos.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Botón de Configuración
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.HomePage = _defaultHomePage;
            settingsWindow.IsAdBlockerEnabled = IsAdBlockerEnabled;
            settingsWindow.IsTabSuspensionEnabled = IsTabSuspensionEnabled; // Pasar el valor actual

            if (settingsWindow.ShowDialog() == true)
            {
                _defaultHomePage = settingsWindow.HomePage;
                IsAdBlockerEnabled = settingsWindow.IsAdBlockerEnabled;
                IsTabSuspensionEnabled = settingsWindow.IsTabSuspensionEnabled; // Actualizar con el valor de la ventana de settings
                SaveSettings(); // Guardar las configuraciones actualizadas
            }
        }

        // Botón PIP
        private void PipButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null && !string.IsNullOrEmpty(SelectedTabItem.Url))
            {
                // En un escenario real, necesitarías extraer la URL del video de la página.
                // Aquí, para la demostración, asumiremos que la URL de la pestaña es la URL del video.
                // O podrías buscar un elemento <video> en el DOM y obtener su src.
                string videoUrl = SelectedTabItem.Url; // O implementa lógica para encontrar el video real

                try
                {
                    // Puedes pasar el mismo entorno de WebView2 para que compartan datos, cookies, etc.
                    // O crear uno nuevo para aislamiento. Aquí, para simplificar, se crea uno nuevo si no existe.
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


        // Menú Contextual del WebView2
        private void WebView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Puedes agregar elementos personalizados al menú contextual aquí si es necesario
            // Por ejemplo, un elemento para "Abrir en PIP" si se hace clic derecho en un video
        }


        // Funcionalidad de "Leer en voz alta"
        private async void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                if (_isReadingAloud)
                {
                    _speechSynthesizer?.SpeakAsyncCancelAll();
                    _isReadingAloud = false;
                    ReadAloudButton.ToolTip = "Leer en voz alta";
                    // Cambiar ícono a "pausar" o "detener" si lo tienes
                }
                else
                {
                    try
                    {
                        // Extraer texto de la página (puedes usar el mismo script que para Gemini)
                        string extractTextScript = @"
                            (function() {
                                var bodyText = document.body.innerText;
                                // Puedes refinar esto para excluir elementos de navegación, etc.
                                return bodyText;
                            })();
                        ";
                        string pageTextJson = await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync(extractTextScript);
                        string pageText = JsonSerializer.Deserialize<string>(pageTextJson) ?? "";

                        if (!string.IsNullOrEmpty(pageText))
                        {
                            _speechSynthesizer?.SpeakAsync(pageText);
                            _isReadingAloud = true;
                            ReadAloudButton.ToolTip = "Detener lectura";
                            // Cambiar ícono a "reproducir"
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

        // Modo Lector
        private async void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                try
                {
                    if (SelectedTabItem.IsReaderMode)
                    {
                        // Desactivar el modo lector: recargar la URL original
                        if (!string.IsNullOrEmpty(SelectedTabItem.Url))
                        {
                            SelectedTabItem.WebViewInstance.Source = new Uri(SelectedTabItem.Url);
                        }
                        SelectedTabItem.IsReaderMode = false;
                        ReaderModeButton.ToolTip = "Modo lector";
                    }
                    else
                    {
                        // Activar el modo lector: inyectar CSS y JavaScript para reformatear la página
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
                            /* Ocultar elementos irrelevantes */
                            header, footer, nav, aside, .sidebar, .comments, .ads, .related-posts {
                                display: none !important;
                            }
                        ";

                        string readerModeScript = @"
                            (function() {
                                var style = document.createElement('style');
                                style.textContent = `" + readerModeCss.Replace("`", "\\`") + @"`; // Escapar backticks
                                document.head.appendChild(style);

                                // Opcional: Simplificar el DOM para eliminar ruido adicional
                                var elementsToRemove = 'header, footer, nav, aside, .sidebar, .comments, .ads, .related-posts, script, style';
                                document.querySelectorAll(elementsToRemove).forEach(function(el) {
                                    el.parentNode.removeChild(el);
                                });

                                // Centrar el contenido
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


        // Modo Incógnito (ejemplo - requiere más lógica para ser completo)
        private void IncognitoButton_Click(object sender, RoutedEventArgs e)
        {
            // Para un modo incógnito real, necesitarías:
            // 1. Crear un nuevo CoreWebView2Environment con un UserDataFolder temporal.
            // 2. Abrir una nueva ventana de navegador o una nueva pestaña con este entorno.
            // 3. Asegurarse de que no se guarden historial, cookies, etc. para este entorno.

            // Por ahora, solo abrimos una nueva pestaña con un mensaje.
            // (La implementación completa de modo incógnito es compleja y va más allá del alcance de este ejemplo).
            MessageBox.Show("El modo incógnito abre una nueva ventana donde la actividad de navegación no se guarda en el historial ni en las cookies después de cerrar la ventana.", "Modo Incógnito", MessageBoxButton.OK, MessageBoxImage.Information);

            // Ejemplo de cómo podrías abrir una nueva ventana con un entorno diferente:
            // var incognitoEnv = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Path.GetTempPath(), "AuroraIncognito", Guid.NewGuid().ToString()));
            // var incognitoWindow = new MainWindow(incognitoEnv); // Necesitarías un constructor que acepte el entorno
            // incognitoWindow.Show();
        }

        // Menú de Extensiones (manejo de clics)
        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is CustomExtension extension)
            {
                extension.IsEnabled = !extension.IsEnabled; // Alternar el estado
                                                            // Aplicar/desaplicar extensión si es necesario (ej: inyectar/eliminar script)
                ApplyExtension(extension);
            }
        }

        private async void ApplyExtension(CustomExtension extension)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                string scriptContent = extension.LoadScriptContent();
                if (!string.IsNullOrEmpty(scriptContent))
                {
                    if (extension.IsEnabled)
                    {
                        // Inyectar el script en la pestaña actual
                        await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync(scriptContent);
                        // También podrías considerar inyectarlo en todas las pestañas existentes o futuras
                    }
                    else
                    {
                        // Para "desaplicar" una extensión, es más complejo.
                        // A menudo implica recargar la página o ejecutar un script de "limpieza".
                        // Para este ejemplo, solo avisamos.
                        MessageBox.Show($"La extensión '{extension.Name}' ha sido {(extension.IsEnabled ? "activada" : "desactivada")}. Puede que necesite recargar la página para ver los cambios.", "Extensiones", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            var extensionsWindow = new ExtensionsWindow(ExtensionManager);
            extensionsWindow.ShowDialog();
            // Después de cerrar la ventana de extensiones, recargar/aplicar configuraciones si es necesario
            // Por ejemplo, si se activaron o desactivaron extensiones.
        }

        // Manejo de mensajes desde JavaScript (para extensiones, extracción de datos, etc.)
        private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView == null) return;

            string message = e.WebMessageAsJson;

            // Procesar mensajes JSON específicos
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
                                    Console.WriteLine($"Texto Extraído: {extractedText.Substring(0, Math.Min(extractedText.Length, 200))}..."); // Log para depuración

                                    var tab = TabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(t => t.WebViewInstance == webView);
                                    if (tab != null)
                                    {
                                        tab.CapturedData.ExtractedText = extractedText;
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

                                    // Preguntar al usuario si desea guardar la contraseña
                                    var result = MessageBox.Show($"¿Deseas guardar la contraseña para {username} en {url}?", "Guardar Contraseña", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        PasswordManager.SavePassword(url, username, password);
                                        MessageBox.Show("Contraseña guardada exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                                    }
                                }
                                break;
                                // Otros tipos de mensajes si se implementan más extensiones
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error al parsear mensaje de WebView2 como JSON: {ex.Message}");
                // Si no es JSON, podría ser un mensaje de depuración simple
                Console.WriteLine($"Mensaje recibido de WebView2: {message}");
            }
        }


        // Manejo de descargas
        private void WebView_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            // Prevenir la descarga automática y mostrar una barra de progreso
            e.Cancel = true;

            // Obtener el nombre de archivo sugerido
            string suggestedFileName = Path.GetFileName(e.ResultFilePath);

            // Abrir un SaveFileDialog para que el usuario elija dónde guardar
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.FileName = suggestedFileName;
            saveFileDialog.Title = "Guardar archivo";

            if (saveFileDialog.ShowDialog() == true)
            {
                e.ResultFilePath = saveFileDialog.FileName;
                e.Cancel = false; // Permitir que la descarga continúe al path elegido

                DownloadProgressBarVisibility = Visibility.Visible;
                DownloadProgress = 0;

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

                e.DownloadOperation.StateChanged += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        switch (e.DownloadOperation.State)
                        {
                            case CoreWebView2DownloadState.Completed:
                                MessageBox.Show($"Descarga completada: {e.ResultFilePath}", "Descarga", MessageBoxButton.OK, MessageBoxImage.Information);
                                DownloadProgressBarVisibility = Visibility.Collapsed;
                                break;
                            case CoreWebView2DownloadState.Interrupted:
                                MessageBox.Show($"Descarga interrumpida: {e.DownloadOperation.InterruptReason}", "Descarga Fallida", MessageBoxButton.OK, MessageBoxImage.Error);
                                DownloadProgressBarVisibility = Visibility.Collapsed;
                                break;
                        }
                    });
                };
            }
            else
            {
                // El usuario canceló la descarga
                e.Cancel = true;
            }
        }

        // Comando para añadir un nuevo grupo de pestañas
        public ICommand AddTabGroupCommand => new RelayCommand(_ => AddNewTabGroup());

        private void AddNewTabGroup()
        {
            var newGroup = new TabGroup($"Grupo {TabGroupManager.TabGroups.Count + 1}");
            TabGroupManager.AddGroup(newGroup);
            TabGroupManager.SelectedTabGroup = newGroup;
            AddNewTab(_defaultHomePage); // Añadir una pestaña por defecto al nuevo grupo
            BrowserTabs.ItemsSource = newGroup.TabsInGroup; // Actualizar el ItemsSource del TabControl
        }

        // Comando para seleccionar un grupo de pestañas (desde el menú de grupos)
        public ICommand SelectTabGroupCommand => new RelayCommand(parameter =>
        {
            if (parameter is TabGroup selectedGroup)
            {
                TabGroupManager.SelectedTabGroup = selectedGroup;
                BrowserTabs.ItemsSource = selectedGroup.TabsInGroup; // Actualizar el ItemsSource del TabControl
                SelectedTabItem = selectedGroup.TabsInGroup.FirstOrDefault(); // Seleccionar la primera pestaña del grupo
            }
        });

        // Evento para arrastrar la ventana sin bordes
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            HwndSource? source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                source.AddHook(WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCLBUTTONDOWN:
                    if (wParam.ToInt32() == HT_CAPTION)
                    {
                        // Permitir arrastrar la ventana incluso si el mouse está sobre controles
                        // No necesitas llamar a DragMove() aquí si usas SendMessage
                        handled = true; // Marca el evento como manejado para evitar el procesamiento por defecto
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        // Funcionalidad de IA/Gemini
        private async void GeminiButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Recolectar datos de las pestañas activas (o seleccionadas)
            // Por simplicidad, tomaremos los datos de la pestaña actualmente seleccionada.
            // Para múltiples pestañas, necesitarías un mecanismo de selección (ej. checkboxes en cada tab).
            var capturedDataList = new ObservableCollection<CapturedPageData>();

            if (SelectedTabItem != null)
            {
                // Asegurarse de que el CoreWebView2 esté inicializado
                if (SelectedTabItem.WebViewInstance.CoreWebView2 == null)
                {
                    await SelectedTabItem.WebViewInstance.EnsureCoreWebView2Async(null);
                }

                // Intentar capturar la información
                try
                {
                    // Obtener texto de la página
                    string extractedText = await GetPageText(SelectedTabItem.WebViewInstance.CoreWebView2);
                    SelectedTabItem.CapturedData.ExtractedText = extractedText;

                    // Capturar captura de pantalla
                    string screenshotBase64 = await CaptureScreenshotAsync(SelectedTabItem.WebViewInstance);
                    SelectedTabItem.CapturedData.ScreenshotBase64 = screenshotBase64;

                    // El favicon ya se debería haber capturado en WebView_NavigationCompleted
                    // Si no, puedes intentar obtenerlo aquí también
                    if (string.IsNullOrEmpty(SelectedTabItem.CapturedData.FaviconBase64) && SelectedTabItem.Favicon != null)
                    {
                        SelectedTabItem.CapturedData.FaviconBase64 = ConvertBitmapImageToBase64(SelectedTabItem.Favicon);
                    }


                    // Asignar los valores a la instancia de CapturedPageData de la pestaña
                    SelectedTabItem.CapturedData.Url = SelectedTabItem.Url;
                    SelectedTabItem.CapturedData.Title = SelectedTabItem.Title;

                    // Agregar la pestaña actual a la lista de datos capturados
                    capturedDataList.Add(SelectedTabItem.CapturedData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al capturar datos de la página para Gemini: {ex.Message}", "Error de Captura", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                MessageBox.Show("No hay una pestaña activa para enviar a Gemini.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Mostrar la ventana GeminiDataViewerWindow con los datos capturados
            var geminiViewerWindow = new GeminiDataViewerWindow(capturedDataList);
            // Establecer el propietario para centrarla respecto a MainWindow
            geminiViewerWindow.Owner = this;

            if (geminiViewerWindow.ShowDialog() == true)
            {
                // El usuario hizo clic en "Enviar a Gemini"
                string userQuestion = geminiViewerWindow.UserQuestion;

                // Aquí es donde se haría la llamada a la API de Gemini
                // Por ejemplo: await CallGeminiAPI(userQuestion, capturedDataList);
                MessageBox.Show($"Datos enviados a Gemini con la pregunta: '{userQuestion}'", "Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
                IsGeminiModeActive = true; // Activar el modo Gemini al enviar la pregunta
            }
            else
            {
                // El usuario hizo clic en "Cancelar"
                MessageBox.Show("Envío a Gemini cancelado.", "Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
                IsGeminiModeActive = false; // Desactivar el modo Gemini
            }
        }


        private async Task<string> GetPageText(CoreWebView2 webView)
        {
            if (webView == null) return string.Empty;

            try
            {
                // Este script intenta obtener el texto legible principal de la página.
                // Puede ser necesario ajustarlo para diferentes estructuras de sitios web.
                string script = @"
                    (function() {
                        // Prioriza elementos de contenido común
                        var selectors = ['article', 'main', 'body'];
                        for (var i = 0; i < selectors.length; i++) {
                            var element = document.querySelector(selectors[i]);
                            if (element && element.innerText) {
                                return element.innerText;
                            }
                        }
                        return document.body.innerText; // Fallback al texto completo del body
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

        private async Task<string> CaptureScreenshotAsync(WebView2 webView)
        {
            if (webView.CoreWebView2 == null)
            {
                await webView.EnsureCoreWebView2Async(null); // Asegúrate de que CoreWebView2 esté inicializado
            }

            try
            {
                // Get the scrollable dimensions of the page
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

                    // Clamp to reasonable maximums to prevent out-of-memory issues for very large pages
                    const int MAX_DIMENSION = 8000; // Example limit, adjust as needed
                    contentWidth = Math.Min(contentWidth, MAX_DIMENSION);
                    contentHeight = Math.Min(contentHeight, MAX_DIMENSION);

                    // Save original WebView2 size
                    double originalWidth = webView.Width;
                    double originalHeight = webView.Height;

                    // Temporarily resize WebView2 to capture full content
                    // Ensure the WebView2 is part of the visual tree and laid out
                    // This might require running on the UI thread and ensuring layout passes
                    Dispatcher.Invoke(() =>
                    {
                        webView.Width = contentWidth;
                        webView.Height = contentHeight;
                        webView.Measure(new Size(contentWidth, contentHeight));
                        webView.Arrange(new Rect(0, 0, contentWidth, contentHeight));
                    });

                    // Wait for layout to update
                    await Task.Delay(50); // Small delay to allow layout to settle

                    using (MemoryStream stream = new MemoryStream())
                    {
                        // Capture the screenshot
                        await webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);

                        // Restore original WebView2 size
                        Dispatcher.Invoke(() =>
                        {
                            webView.Width = originalWidth;
                            webView.Height = originalHeight;
                            webView.Measure(new Size(originalWidth, originalHeight));
                            webView.Arrange(new Rect(0, 0, originalWidth, originalHeight));
                        });

                        byte[] imageBytes = stream.ToArray();
                        return Convert.ToBase64String(imageBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al capturar captura de pantalla: {ex.Message}");
                // Return empty string or a placeholder Base64 image
                return string.Empty;
            }
        }


        // Helper para convertir BitmapImage a Base64 (útil para favicon si no viene directamente como bytes)
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

        private void SaveBase64ImageAsPng(string base64String, string filePath)
        {
            if (string.IsNullOrEmpty(base64String)) return;

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String);
                File.WriteAllBytes(filePath, imageBytes);
                MessageBox.Show($"Captura de pantalla guardada en: {filePath}", "Captura Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la captura de pantalla: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // ... El resto de tu código igual ...
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }

    public enum ToolbarPosition
    {
        Top,
        Bottom,
        Left,
        Right
    }

    // Clase auxiliar para los datos capturados para Gemini
    public class CapturedPageData : INotifyPropertyChanged
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public string ScreenshotBase64 { get; set; } = string.Empty; // Base64 de la imagen
        public string FaviconBase64 { get; set; } = string.Empty;   // Base64 del favicon

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Clases para la serialización del estado de la sesión
    public class TabGroupState
    {
        public string GroupId { get; set; } = Guid.NewGuid().ToString();
        public string GroupName { get; set; } = "Default Group";
        public List<string?> TabUrls { get; set; } = new List<string?>();
        public string? SelectedTabUrl { get; set; }
    }
}
