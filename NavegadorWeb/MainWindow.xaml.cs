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
using System.Speech.Synthesis;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.ComponentModel; // Para INotifyPropertyChanged
using System.Windows.Interop; // Para el redimensionamiento de ventana
using System.Runtime.InteropServices; // Para el redimensionamiento de ventana

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged // Implementar INotifyPropertyChanged
    {
        // ... (Tu código existente para campos y propiedades) ...
        private string _defaultHomePage = "https://www.google.com";
        private const string HomePageSettingKey = "DefaultHomePage";
        private const string AdBlockerSettingKey = "AdBlockerEnabled";
        private const string DefaultSearchEngineSettingKey = "DefaultSearchEngine";
        private const string TabSuspensionSettingKey = "TabSuspensionEnabled";
        private const string RestoreSessionSettingKey = "RestoreSessionOnStartup";
        private const string LastSessionUrlsSettingKey = "LastSessionUrls";
        private const string TrackerProtectionSettingKey = "TrackerProtectionEnabled";
        private const string PdfViewerSettingKey = "PdfViewerEnabled";
        private const string UncleanShutdownFlagKey = "UncleanShutdown";
        private const string BrowserBackgroundColorKey = "BrowserBackgroundColor";
        private const string BrowserForegroundColorKey = "BrowserForegroundColor";
        private const string ToolbarOrientationKey = "ToolbarOrientation"; // NUEVO: Clave para la orientación de la barra

        private string _defaultSearchEngineUrl = "https://www.google.com/search?q=";
        private bool _isTabSuspensionEnabled = false;
        private bool _restoreSessionOnStartup = true;
        private bool _isPdfViewerEnabled = true;
        private ToolbarPosition _currentToolbarPosition = ToolbarPosition.Top; // NUEVO: Posición inicial de la barra de herramientas

        // Propiedades para el tema del navegador
        private Color _browserBackgroundColor;
        public Color BrowserBackgroundColor
        {
            get { return _browserBackgroundColor; }
            set
            {
                if (_browserBackgroundColor != value)
                {
                    _browserBackgroundColor = value;
                    OnPropertyChanged(nameof(BrowserBackgroundColor));
                    Application.Current.Resources["BrowserBackgroundColor"] = value;
                    Application.Current.Resources["BrowserBackgroundBrush"] = new SolidColorBrush(value);
                    // Asegurar que la barra de título también refleje el cambio
                    if (MainToolbarContainer != null) // Usamos el mismo color para la barra de título personalizada
                        ((Border)this.Content).BorderBrush = new SolidColorBrush(value);
                }
            }
        }

        private Color _browserForegroundColor;
        public Color BrowserForegroundColor
        {
            get { return _browserForegroundColor; }
            set
            {
                if (_browserForegroundColor != value)
                {
                    _browserForegroundColor = value;
                    OnPropertyChanged(nameof(BrowserForegroundColor));
                    Application.Current.Resources["BrowserForegroundColor"] = value;
                    Application.Current.Resources["BrowserForegroundBrush"] = new SolidColorBrush(value);
                    // Actualizar el color de los botones de control de la ventana
                    ApplyForegroundToWindowControls(); // NUEVO: Función para actualizar los colores de los botones de control
                }
            }
        }


        private TabGroupManager _tabGroupManager;
        private ExtensionManager _extensionManager;

        public BrowserTabItem SelectedTabItem
        {
            get { return (BrowserTabItem)GetValue(SelectedTabItemProperty); }
            set { SetValue(SelectedTabItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedTabItemProperty =
            DependencyProperty.Register("SelectedTabItem", typeof(BrowserTabItem), typeof(MainWindow), new PropertyMetadata(null));


        // Entornos de WebView2
        private CoreWebView2Environment _defaultEnvironment;
        private CoreWebView2Environment _incognitoEnvironment;

        private string _readerModeScript = string.Empty;
        private string _darkModeScript = string.Empty;
        private string _pageColorExtractionScript = string.Empty; // NUEVO: Script para extracción de color de página

        private SpeechSynthesizer _speechSynthesizer;
        private bool _isReadingAloud = false;

        private bool _isFindBarVisible = false;
        private CoreWebView2FindInPage _findInPage;

        // Comandos para los atajos de teclado
        public ICommand ReloadCommand { get; private set; }
        public ICommand ToggleFullscreenCommand { get; private set; }
        public ICommand OpenDevToolsCommand { get; private set; }
        public ICommand ScreenshotCommand { get; private set; }
        public ICommand NewTabCommand { get; private set; }
        public ICommand CloseTabCommand { get; private set; }
        public ICommand FocusUrlBarCommand { get; private set; }
        public ICommand OpenHistoryCommand { get; private set; }
        public ICommand OpenBookmarksCommand { get; private set; }
        public ICommand OpenDownloadsCommand { get; private set; }
        public ICommand ToggleFindBarCommand { get; private set; }
        public ICommand CloseFindBarCommand { get; private set; }

        // NUEVO: Para el redimensionamiento de ventana
        private const int WM_NCHITTEST = 0x0084;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCAPTION = 2;

        public MainWindow()
        {
            InitializeComponent();
            _tabGroupManager = new TabGroupManager();
            _extensionManager = new ExtensionManager();
            this.DataContext = this;

            TabGroupContainer.ItemsSource = _tabGroupManager.TabGroups;

            LoadSettings(); // Cargar configuraciones al iniciar la aplicación (incluyendo colores y orientación)
            InitializeEnvironments();
            LoadReaderModeScript();
            LoadDarkModeScript();
            LoadPageColorExtractionScript(); // NUEVO: Cargar script de extracción de color

            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();
            _speechSynthesizer.SpeakCompleted += SpeechSynthesizer_SpeakCompleted;

            InitializeCommands();

            // NUEVO: Configuración de la ventana para permitir el redimensionamiento personalizado
            this.SourceInitialized += MainWindow_SourceInitialized;
            this.StateChanged += MainWindow_StateChanged; // Para cambiar el icono de maximizar/restaurar
            ApplyForegroundToWindowControls(); // Aplicar colores iniciales a los botones de control de ventana
            ApplyToolbarPosition(_currentToolbarPosition); // Aplicar la orientación de la barra de herramientas al inicio
        }

        // NUEVO: Método para aplicar el color de primer plano a los botones de control de la ventana
        private void ApplyForegroundToWindowControls()
        {
            if (MaximizeRestoreButton != null)
            {
                MaximizeRestoreButton.Foreground = BrowserForegroundColor != null ? new SolidColorBrush(BrowserForegroundColor) : Brushes.Black;
                MinimizeButton.Foreground = BrowserForegroundColor != null ? new SolidColorBrush(BrowserForegroundColor) : Brushes.Black;
                CloseButton.Foreground = BrowserForegroundColor != null ? new SolidColorBrush(BrowserForegroundColor) : Brushes.Black;
                AIButton_TitleBar.Foreground = BrowserForegroundColor != null ? new SolidColorBrush(BrowserForegroundColor) : Brushes.Black;
            }
            if (WindowTitleText != null)
            {
                WindowTitleText.Foreground = BrowserForegroundColor != null ? new SolidColorBrush(BrowserForegroundColor) : Brushes.Black;
            }
        }

        private void InitializeCommands()
        {
            ReloadCommand = new RelayCommand(ReloadButton_Click);
            ToggleFullscreenCommand = new RelayCommand(ToggleFullscreen);
            OpenDevToolsCommand = new RelayCommand(OpenDevTools);
            ScreenshotCommand = new RelayCommand(ScreenshotButton_Click);
            NewTabCommand = new RelayCommand(NewTabButton_Click);
            CloseTabCommand = new RelayCommand(CloseCurrentTab);
            FocusUrlBarCommand = new RelayCommand(FocusUrlTextBox);
            OpenHistoryCommand = new RelayCommand(HistoryButton_Click);
            OpenBookmarksCommand = new RelayCommand(BookmarksButton_Click);
            OpenDownloadsCommand = new RelayCommand(DownloadsButton_Click);
            ToggleFindBarCommand = new RelayCommand(FindButton_Click);
            CloseFindBarCommand = new RelayCommand(CloseFindBarButton_Click);
        }

        private void ToggleFullscreen(object parameter)
        {
            if (this.WindowState == WindowState.Maximized && this.WindowStyle == WindowStyle.None)
            {
                this.WindowStyle = WindowStyle.None; // Mantener None para el custom chrome
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowStyle = WindowStyle.None; // Mantener None para el custom chrome
                this.WindowState = WindowState.Maximized;
            }
            // Actualizar el ícono del botón de maximizar
            UpdateMaximizeRestoreButtonContent();
        }

        private void OpenDevTools(object parameter)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.OpenDevToolsWindow();
            }
        }

        private void CloseCurrentTab(object parameter)
        {
            if (SelectedTabItem != null)
            {
                CloseBrowserTab(SelectedTabItem.Tab);
            }
        }

        private void FocusUrlTextBox(object parameter)
        {
            UrlTextBox.Focus();
            UrlTextBox.SelectAll();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SpeechSynthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            _isReadingAloud = false;
            Dispatcher.Invoke(() => ReadAloudButton.Content = "🔊");
        }

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
                    MessageBox.Show("Advertencia: El archivo 'DarkMode.js' no se encontró. El modo oscuro global no funcionará.", "Archivo Faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el script de modo oscuro: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// NUEVO: Carga el script para extraer el color dominante de la página.
        /// </summary>
        private void LoadPageColorExtractionScript()
        {
            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PageColorExtractor.js");
                if (File.Exists(scriptPath))
                {
                    _pageColorExtractionScript = File.ReadAllText(scriptPath);
                }
                else
                {
                    MessageBox.Show("Advertencia: El archivo 'PageColorExtractor.js' no se encontró. La aclimatación de color de página no funcionará.", "Archivo Faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el script de extracción de color de página: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void InitializeEnvironments()
        {
            try
            {
                string defaultUserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AuroraBrowser", "UserData");
                _defaultEnvironment = await CoreWebView2Environment.CreateAsync(null, defaultUserDataFolder);

                string incognitoUserDataFolder = Path.Combine(Path.GetTempPath(), "AuroraBrowserIncognito", Guid.NewGuid().ToString());
                _incognitoEnvironment = await CoreWebView2Environment.CreateAsync(null, incognitoUserDataFolder, new CoreWebView2EnvironmentOptions {
                    IsCustomCrashReportingEnabled = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar los entornos del navegador: {ex.Message}\nPor favor, asegúrate de tener WebView2 Runtime instalado.", "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string blockedDomainsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "blocked_domains.txt");
            AdBlocker.LoadBlockedDomainsFromFile(blockedDomainsFilePath);

            string trackerDomainsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tracker_domains.txt");
            TrackerBlocker.LoadBlockedTrackerDomainsFromFile(trackerDomainsFilePath);

            bool uncleanShutdown = false;
            if (ConfigurationManager.AppSettings[UncleanShutdownFlagKey] != null && bool.TryParse(ConfigurationManager.AppSettings[UncleanShutdownFlagKey], out bool flag))
            {
                uncleanShutdown = flag;
            }

            UpdateUncleanShutdownFlag(true);

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
                            CrashRecoveryWindow recoveryWindow = new CrashRecoveryWindow();
                            recoveryWindow.ShowDialog();

                            if (recoveryWindow.ShouldRestoreSession)
                            {
                                foreach (var group in _tabGroupManager.TabGroups.ToList())
                                {
                                    foreach (var tabItem in group.TabsInGroup.ToList())
                                    {
                                        CloseBrowserTab(tabItem.Tab);
                                    }
                                    if (!group.TabsInGroup.Any() && _tabGroupManager.TabGroups.Count > 1)
                                    {
                                        _tabGroupManager.RemoveGroup(group);
                                    }
                                }
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
                                AddNewTab(_defaultHomePage);
                            }
                        }
                        else
                        {
                            AddNewTab(_defaultHomePage);
                        }
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"Error al leer la sesión guardada: {ex.Message}. Se iniciará con la página de inicio.", "Error de Sesión", MessageBoxButton.OK, MessageBoxImage.Error);
                        AddNewTab(_defaultHomePage);
                    }
                }
                else
                {
                    AddNewTab(_defaultHomePage);
                }
            }
            else
            {
                AddNewTab(_defaultHomePage);
            }
        }

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


        private async void AddNewTab(string url = null, bool isIncognito = false, TabGroup targetGroup = null)
        {
            if (_defaultEnvironment == null || _incognitoEnvironment == null)
            {
                await Task.Delay(100);
                if (_defaultEnvironment == null || _incognitoEnvironment == null)
                {
                    MessageBox.Show("El navegador no está listo. Por favor, reinicia la aplicación.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            TabGroup groupToAdd = targetGroup ?? _tabGroupManager.GetDefaultGroup();

            TabItem newTabItem = new TabItem();
            newTabItem.Name = "Tab" + (groupToAdd.TabsInGroup.Count + 1);

            BrowserTabItem browserTab = new BrowserTabItem
            {
                Tab = newTabItem,
                IsIncognito = isIncognito,
                IsSplit = false,
                ParentGroup = groupToAdd
            };

            DockPanel tabHeaderPanel = new DockPanel();
            browserTab.FaviconImage = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            browserTab.AudioIconImage = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            browserTab.ExtensionIconImage = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            browserTab.BlockedIconImage = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            browserTab.HeaderTextBlock = new TextBlock { Text = "Cargando...", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };

            browserTab.FaviconImage.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("FaviconSource") { Source = browserTab });
            browserTab.AudioIconImage.SetBinding(Image.VisibilityProperty, new System.Windows.Data.Binding("IsAudioPlaying") { Source = browserTab, Converter = (System.Windows.Data.IValueConverter)this.FindResource("BooleanToVisibilityConverter") });
            browserTab.AudioIconImage.MouseLeftButtonUp += AudioIcon_MouseLeftButtonUp;
            browserTab.ExtensionIconImage.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("ExtensionActiveIcon") { Source = browserTab });
            browserTab.ExtensionIconImage.SetBinding(Image.VisibilityProperty, new System.Windows.Data.Binding("IsExtensionActive") { Source = browserTab, Converter = (System.Windows.Data.IValueConverter)this.FindResource("BooleanToVisibilityConverter") });
            browserTab.BlockedIconImage.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("SiteBlockedIcon") { Source = browserTab });
            browserTab.BlockedIconImage.SetBinding(Image.VisibilityProperty, new System.Windows.Data.Binding("IsSiteBlocked") { Source = browserTab, Converter = (System.Windows.Data.IValueConverter)this.FindResource("BooleanToVisibilityConverter") });


            if (isIncognito)
            {
                browserTab.HeaderTextBlock.Text = "(Incógnito) Cargando...";
            }

            Button closeButton = new Button
            {
                Content = "✖",
                Width = 20,
                Height = 20,
                Margin = new Thickness(5, 0, 0, 0),
                ToolTip = "Cerrar Pestaña"
            };
            closeButton.Click += CloseTabButton_Click;
            closeButton.Tag = newTabItem;

            DockPanel.SetDock(browserTab.FaviconImage, Dock.Left);
            DockPanel.SetDock(browserTab.AudioIconImage, Dock.Left);
            DockPanel.SetDock(browserTab.ExtensionIconImage, Dock.Left);
            DockPanel.SetDock(browserTab.BlockedIconImage, Dock.Left);
            DockPanel.SetDock(browserTab.HeaderTextBlock, Dock.Left);
            DockPanel.SetDock(closeButton, Dock.Right);

            tabHeaderPanel.Children.Add(browserTab.FaviconImage);
            tabHeaderPanel.Children.Add(browserTab.AudioIconImage);
            tabHeaderPanel.Children.Add(browserTab.ExtensionIconImage);
            tabHeaderPanel.Children.Add(browserTab.BlockedIconImage);
            tabHeaderPanel.Children.Add(browserTab.HeaderTextBlock);
            tabHeaderPanel.Children.Add(closeButton);
            newTabItem.Header = tabHeaderPanel;

            WebView2 webView1 = new WebView2();
            webView1.Source = new Uri(url ?? _defaultHomePage);
            webView1.Name = "WebView1_Tab" + (groupToAdd.TabsInGroup.Count + 1);
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
            webView1.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;


            Grid tabContent = new Grid();
            tabContent.Children.Add(webView1);
            newTabItem.Content = tabContent;

            groupToAdd.TabsInGroup.Add(browserTab);
            browserTab.LeftWebView = webView1;

            newTabItem.IsSelected = true;
            SelectedTabItem = browserTab;

            UpdateUrlTextBoxFromCurrentTab();

            CheckAndSuggestTabSuspension();
        }

        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null)
            {
                await currentWebView.EnsureCoreWebView2Async(null);
            }
        }

        private void ConfigureCoreWebView2(WebView2 currentWebView, CoreWebView2InitializationCompletedEventArgs e, CoreWebView2Environment environment)
        {
            if (currentWebView != null && e.IsSuccess)
            {
                currentWebView.CoreWebView2.Environment.SetCustomFileExtensions(new[] { ".pdf", ".docx", ".xlsx" });

                // Desuscribirse para evitar duplicados si se llama varias veces (ej. al cambiar de entorno o al recargar)
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
                currentWebView.CoreWebView2.WebResourceResponseReceived -= CoreWebView2_WebResourceResponseReceived;


                // Suscribirse de nuevo
                currentWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

                currentWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                currentWebView.CoreWebView2.Settings.IsPinchZoomEnabled = true;
                currentWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;


                currentWebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

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
                currentWebView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            }
        }


        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (AdBlocker.IsEnabled && AdBlocker.IsBlocked(e.Request.Uri))
            {
                e.Response = ((WebView2)sender).CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Forbidden", "Content-Type: text/plain\nAccess-Control-Allow-Origin: *"
                );
                var browserTab = GetBrowserTabItemFromWebView(sender as WebView2);
                if (browserTab != null) browserTab.IsSiteBlocked = true;
                return;
            }

            if (TrackerBlocker.IsEnabled && TrackerBlocker.IsBlocked(e.Request.Uri))
            {
                e.Response = ((WebView2)sender).CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Forbidden", "Content-Type: text/plain\nAccess-Control-Allow-Origin: *"
                );
                var browserTab = GetBrowserTabItemFromWebView(sender as WebView2);
                if (browserTab != null) browserTab.IsSiteBlocked = true;
                return;
            }
        }

        private void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            // No se necesita lógica adicional aquí para IsSiteBlocked, ya se maneja en WebResourceRequested.
        }


        private async void CoreWebView2_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            if (_isPdfViewerEnabled && e.DownloadOperation.Uri.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                e.Handled = true;
                PdfViewerWindow pdfViewer = new PdfViewerWindow(e.DownloadOperation.Uri, _defaultEnvironment);
                pdfViewer.Show();
                return;
            }

            e.Handled = true;

            DownloadEntry newDownload = new DownloadEntry
            {
                FileName = e.ResultFilePath.Split('\\').Last(),
                Url = e.DownloadOperation.Uri,
                TotalBytes = e.DownloadOperation.TotalBytesToReceive,
                TargetPath = e.ResultFilePath,
                State = CoreWebView2DownloadState.InProgress,
                Progress = 0
            };

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = newDownload.FileName,
                Filter = "Todos los archivos (*.*)|*.*",
                Title = "Guardar descarga como..."
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                newDownload.TargetPath = saveFileDialog.FileName;
                e.ResultFilePath = saveFileDialog.FileName;

                DownloadManager.AddOrUpdateDownload(newDownload);

                e.DownloadOperation.BytesReceivedChanged += (s, args) =>
                {
                    newDownload.ReceivedBytes = e.DownloadOperation.BytesReceived;
                    if (newDownload.TotalBytes > 0)
                    {
                        newDownload.Progress = (int)((double)newDownload.ReceivedBytes / newDownload.TotalBytes * 100);
                    }
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
                    DownloadManager.AddOrUpdateDownload(newDownload);
                };
            }
            else
            {
                e.Cancel = true;
                MessageBox.Show("Descarga cancelada por el usuario.", "Descarga Cancelada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CoreWebView2_PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                $"El sitio web '{e.Uri}' solicita permiso para usar: {e.PermissionKind}.\n¿Deseas permitirlo?",
                "Solicitud de Permiso",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                e.State = CoreWebView2PermissionState.Allow;
            }
            else
            {
                e.State = CoreWebView2PermissionState.Deny;
            }
        }

        private void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            var browserTab = GetBrowserTabItemFromWebView(currentWebView);

            if (browserTab != null && SelectedTabItem == browserTab)
            {
                UrlTextBox.Text = currentWebView.CoreWebView2.Source;
            }
            if (browserTab != null) browserTab.IsSiteBlocked = false;
        }


        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            var browserTab = GetBrowserTabItemFromWebView(currentWebView);

            if (browserTab != null && SelectedTabItem == browserTab)
            {
                if (!e.IsSuccess)
                {
                    if (e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationAborted)
                    {
                        string errorPagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomErrorPage.html");
                        if (File.Exists(errorPagePath))
                        {
                            currentWebView.CoreWebView2.Navigate($"file:///{errorPagePath.Replace("\\", "/")}");
                        }
                        else
                        {
                            MessageBox.Show($"La navegación a {currentWebView.CoreWebView2.Source} falló con el código de error {e.WebErrorStatus}", "Error de Navegación", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    if (!browserTab.IsIncognito && browserTab.LeftWebView == currentWebView)
                    {
                        HistoryManager.AddHistoryEntry(currentWebView.CoreWebView2.Source, currentWebView.CoreWebView2.DocumentTitle);
                    }

                    await InjectEnabledExtensions(currentWebView, browserTab);

                    // NUEVO: Intentar extraer el color de la página
                    if (!string.IsNullOrEmpty(_pageColorExtractionScript))
                    {
                        try
                        {
                            string resultJson = await currentWebView.CoreWebView2.ExecuteScriptAsync(_pageColorExtractionScript);
                            if (resultJson != null && resultJson != "null")
                            {
                                var colorData = JsonSerializer.Deserialize<Dictionary<string, string>>(resultJson);
                                if (colorData != null && colorData.ContainsKey("dominantColor"))
                                {
                                    string dominantColorHex = colorData["dominantColor"];
                                    try
                                    {
                                        Color pageColor = (Color)ColorConverter.ConvertFromString(dominantColorHex);
                                        // Aquí puedes usar 'pageColor' para tu barra superior
                                        // Por simplicidad, la aplicaremos al DockPanel de la barra de herramientas
                                        MainToolbarContainer.Background = new SolidColorBrush(pageColor);
                                        // Opcionalmente, podrías cambiar el color del texto si el contraste es bajo
                                        // (Más avanzado: detectar contraste y cambiar foreground)
                                    }
                                    catch (FormatException)
                                    {
                                        Debug.WriteLine($"Color hexadecimal inválido de la página: {dominantColorHex}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error al ejecutar el script de extracción de color: {ex.Message}");
                        }
                    }
                }
            }
            LoadingProgressBar.Visibility = Visibility.Collapsed;
        }

        private async Task InjectEnabledExtensions(WebView2 webView, BrowserTabItem browserTab)
        {
            bool anyExtensionInjected = false;
            foreach (var extension in _extensionManager.GetEnabledExtensions())
            {
                try
                {
                    string scriptContent = extension.LoadScriptContent();
                    if (!string.IsNullOrEmpty(scriptContent))
                    {
                        await webView.CoreWebView2.ExecuteScriptAsync(scriptContent);
                        anyExtensionInjected = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al inyectar extensión '{extension.Name}': {ex.Message}");
                }
            }
            browserTab.IsExtensionActive = anyExtensionInjected;
        }


        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            LoadingProgressBar.Visibility = Visibility.Visible;
            // Restaurar el color de fondo de la barra de herramientas al color del tema al inicio de la navegación
            MainToolbarContainer.Background = new SolidColorBrush(BrowserBackgroundColor);

            if (_isPdfViewerEnabled && e.Uri.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                PdfViewerWindow pdfViewer = new PdfViewerWindow(e.Uri, _defaultEnvironment);
                pdfViewer.Show();
                return;
            }
        }

        private void WebView_DocumentTitleChanged(object sender, object e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView != null)
            {
                var browserTab = GetBrowserTabItemFromWebView(currentWebView);
                if (browserTab != null)
                {
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

                if (SelectedTabItem == browserTab && browserTab.LeftWebView == currentWebView)
                {
                    // Actualizar el texto en la barra de título personalizada
                    WindowTitleText.Text = currentWebView.CoreWebView2.DocumentTitle + " - Aurora Browser";
                }
            }
        }

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
                        bitmap.Freeze();
                        browserTab.FaviconSource = bitmap;
                    }
                    else
                    {
                        browserTab.FaviconSource = browserTab.GetDefaultGlobeIcon();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener favicon: {ex.Message}");
                browserTab.FaviconSource = browserTab.GetDefaultGlobeIcon();
            }
        }

        private void CoreWebView2_IsAudioPlayingChanged(object sender, object e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            var browserTab = GetBrowserTabItemFromWebView(currentWebView);
            if (browserTab == null) return;

            browserTab.IsAudioPlaying = currentWebView.CoreWebView2.IsAudioPlaying;
        }

        private void AudioIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image audioIcon = sender as Image;
            if (audioIcon == null) return;

            BrowserTabItem browserTab = audioIcon.DataContext as BrowserTabItem;
            if (browserTab == null || browserTab.LeftWebView == null || browserTab.LeftWebView.CoreWebView2 == null) return;

            browserTab.LeftWebView.CoreWebView2.IsMuted = !browserTab.LeftWebView.CoreWebView2.IsMuted;
            audioIcon.ToolTip = browserTab.LeftWebView.CoreWebView2.IsMuted ? "Audio silenciado (clic para reactivar)" : "Reproduciendo audio (clic para silenciar/reactivar)";
        }


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
                string errorPagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomErrorPage.html");
                if (File.Exists(errorPagePath))
                {
                    failedWebView.CoreWebView2.Navigate($"file:///{errorPagePath.Replace("\\", "/")}");
                }
            }
        }


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

            string input = UrlTextBox.Text.Trim();
            string urlToNavigate = input;

            if (!Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                urlToNavigate = _defaultSearchEngineUrl + Uri.EscapeDataString(input);
            }
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

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
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

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow historyWindow = new HistoryWindow();
            if (historyWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(historyWindow.SelectedUrl))
                {
                    UrlTextBox.Text = historyWindow.SelectedUrl;
                    NavigateToUrlInCurrentTab();
                }
            }
        }

        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            BookmarksWindow bookmarksWindow = new BookmarksWindow();
            if (bookmarksWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(bookmarksWindow.SelectedUrl))
                {
                    UrlTextBox.Text = bookmarksWindow.SelectedUrl;
                    NavigateToUrlInCurrentTab();
                }
            }
        }

        private void AddBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                var browserTab = SelectedTabItem;
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

        private void DownloadsButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadsWindow downloadsWindow = new DownloadsWindow();
            downloadsWindow.Show();
        }

        private async void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para aplicar el modo lectura.", "Error de Modo Lectura", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrEmpty(_readerModeScript))
            {
                try
                {
                    await currentWebView.CoreWebView2.ExecuteScriptAsync(_readerModeScript);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al aplicar el modo lectura: {ex.Message}", "Error de Modo Lectura", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Advertencia: El archivo 'ReaderMode.js' no se encontró. El modo lectura no funcionará.", "Archivo Faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _isReadingAloud = false;
                ReadAloudButton.Content = "🔊";
                return;
            }

            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para leer en voz alta.", "Leer en Voz Alta", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string script = @"
                        (function() {
                            let text = '';
                            let mainContent = document.querySelector('article, main, .post-content, .entry-content, #content, #main');

                            if (mainContent) {
                                text = mainContent.innerText || mainContent.textContent;
                            } else {
                                text = document.body.innerText || document.body.textContent;
                            }

                            text = text.replace(/(\r\n|\n|\r)/gm, ' ').replace(/\s+/g, ' ').trim();

                            return text;
                        })();
                    ";
                string pageText = await currentWebView.CoreWebView2.ExecuteScriptAsync(script);

                pageText = JsonSerializer.Deserialize<string>(pageText);

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    _speechSynthesizer.SpeakAsync(pageText);
                    _isReadingAloud = true;
                    ReadAloudButton.Content = "⏸️";
                }
                else
                {
                    MessageBox.Show("No se encontró texto legible en la página actual.", "Leer en Voz Alta", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al leer en voz alta: {ex.Message}", "Error de Lectura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SplitScreenButton_Click(object sender, RoutedEventArgs e)
        {
            var currentTab = SelectedTabItem;
            if (currentTab == null || currentTab.LeftWebView == null || currentTab.LeftWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una pestaña activa o el navegador no está listo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (currentTab.IsSplit)
            {
                DisableSplitScreenForCurrentTab(currentTab);
                SplitScreenButton.Content = "↔️";
            }
            else
            {
                await EnableSplitScreenForCurrentTab(currentTab, _defaultHomePage);
                SplitScreenButton.Content = "➡️";
            }
        }

        private async void AIButton_Click(object sender, RoutedEventArgs e)
        {
            var currentTab = SelectedTabItem;
            if (currentTab == null || currentTab.LeftWebView == null || currentTab.LeftWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una pestaña activa o el navegador no está listo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!currentTab.IsSplit)
            {
                await EnableSplitScreenForCurrentTab(currentTab, "https://gemini.google.com/");
                SplitScreenButton.Content = "➡️";
            }
            else
            {
                if (currentTab.RightWebView != null && currentTab.RightWebView.CoreWebView2 != null)
                {
                    currentTab.RightWebView.CoreWebView2.Navigate("https://gemini.google.com/");
                }
                else
                {
                    await EnableSplitScreenForCurrentTab(currentTab, "https://gemini.google.com/");
                }
            }
        }

        private async void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para capturar.", "Error de Captura", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
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

                    using (MemoryStream stream = new MemoryStream())
                    {
                        await currentWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);

                        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            stream.WriteTo(fileStream);
                        }
                    }
                    MessageBox.Show($"Captura de pantalla guardada en:\n{filePath}", "Captura Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al realizar la captura de pantalla: {ex.Message}", "Error de Captura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TabManagerButton_Click(object sender, RoutedEventArgs e)
        {
            TabManagerWindow tabManagerWindow = new TabManagerWindow(() => _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).ToList(), CloseBrowserTab, GetCurrentBrowserTabItemInternal);
            tabManagerWindow.Show();
        }

        private void DataExtractionButton_Click(object sender, RoutedEventArgs e)
        {
            DataExtractionWindow dataExtractionWindow = new DataExtractionWindow(GetCurrentWebView);
            dataExtractionWindow.Show();
        }

        private async void DarkModeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para aplicar el modo oscuro.", "Error de Modo Oscuro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrEmpty(_darkModeScript))
            {
                try
                {
                    await currentWebView.CoreWebView2.ExecuteScriptAsync(_darkModeScript);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al aplicar el modo oscuro: {ex.Message}", "Error de Modo Oscuro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Advertencia: El script de modo oscuro no está cargado. Asegúrate de que 'DarkMode.js' exista.", "Error de Modo Oscuro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PerformanceMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            PerformanceMonitorWindow monitorWindow = new PerformanceMonitorWindow(() => _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).ToList());
            monitorWindow.Show();
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            _isFindBarVisible = !_isFindBarVisible;
            FindBar.Visibility = _isFindBarVisible ? Visibility.Visible : Visibility.Collapsed;

            if (_isFindBarVisible)
            {
                FindTextBox.Focus();
                FindResultsTextBlock.Text = "0/0";
                ClearFindResults();
            }
            else
            {
                ClearFindResults();
            }
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformFindInPage(FindTextBox.Text);
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformFindInPage(FindTextBox.Text, CoreWebView2FindInPageKind.Next);
            }
        }

        private async void PerformFindInPage(string searchText, CoreWebView2FindInPageKind findKind = CoreWebView2FindInPageKind.None)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null || string.IsNullOrWhiteSpace(searchText))
            {
                FindResultsTextBlock.Text = "0/0";
                ClearFindResults();
                return;
            }

            if (_findInPage == null || _findInPage.SearchText != searchText || findKind == CoreWebView2FindInPageKind.None)
            {
                _findInPage = currentWebView.CoreWebView2.FindInPage(searchText, CoreWebView2FindInPageKind.None);
            }
            else
            {
                _findInPage = currentWebView.CoreWebView2.FindInPage(searchText, findKind);
            }
        }

        private void CoreWebView2_FindInPageCompleted(object sender, CoreWebView2FindInPageCompletedEventArgs e)
        {
            FindResultsTextBlock.Text = $"{e.ActiveMatchIndex + 1}/{e.Matches}";
            if (e.Matches == 0)
            {
                FindResultsTextBlock.Text = "0/0";
            }
        }

        private void ClearFindResults()
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.FindInPage(string.Empty, CoreWebView2FindInPageKind.None);
                FindResultsTextBlock.Text = "0/0";
            }
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFindInPage(FindTextBox.Text, CoreWebView2FindInPageKind.Next);
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            PerformFindInPage(FindTextBox.Text, CoreWebView2FindInPageKind.Previous);
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            _isFindBarVisible = false;
            FindBar.Visibility = Visibility.Collapsed;
            ClearFindResults();
        }

        private void PermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            PermissionsManagerWindow permissionsWindow = new PermissionsManagerWindow(GetDefaultEnvironment);
            permissionsWindow.Show();
        }

        private async void PipButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para extraer un video.", "Error de PIP", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string script = @"
                    (function() {
                        let video = document.querySelector('video');
                        if (video && video.src) {
                            video.pause();
                            return video.src;
                        }
                        let youtubeIframe = document.querySelector('iframe[src*=""youtube.com/embed""]');
                        if (youtubeIframe && youtubeIframe.src) {
                            return youtubeIframe.src;
                        }
                        let youtubeWatchIframe = document.querySelector('iframe[src*=""youtube.com/watch""]');
                        if (youtubeWatchIframe && youtubeWatchIframe.src) {
                            return youtubeWatchIframe.src;
                        }
                        return null;
                    })();
                ";
                string videoUrlJson = await currentWebView.CoreWebView2.ExecuteScriptAsync(script);
                string videoUrl = JsonSerializer.Deserialize<string>(videoUrlJson);

                if (!string.IsNullOrEmpty(videoUrl))
                {
                    PipWindow pipWindow = new PipWindow(videoUrl, currentWebView);
                    pipWindow.Show();
                }
                else
                {
                    MessageBox.Show("No se encontró ningún video reproducible en la página actual.", "Video no Encontrado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al intentar el modo Picture-in-Picture: {ex.Message}", "Error de PIP", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            PasswordManagerWindow passwordWindow = new PasswordManagerWindow();
            passwordWindow.ShowDialog();
        }

        private void ExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            ExtensionsWindow extensionsWindow = new ExtensionsWindow(_extensionManager);
            extensionsWindow.ShowDialog();
        }


        private async void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            var browserTab = GetBrowserTabItemFromWebView(currentWebView);
            if (browserTab != null && browserTab.IsIncognito) return;

            string currentUrl = currentWebView.CoreWebView2.Source;
            string username = null;
            string password = null;

            var allPasswords = PasswordManager.GetAllPasswords();
            var matchingEntry = allPasswords.FirstOrDefault(p =>
                new Uri(p.Url).Host.Equals(new Uri(currentUrl).Host, StringComparison.OrdinalIgnoreCase));

            if (matchingEntry != null)
            {
                username = matchingEntry.Username;
                password = PasswordManager.DecryptPassword(matchingEntry.EncryptedPassword);

                string autofillScript = $@"
                    (function() {{
                        let usernameFields = document.querySelectorAll('input[type=""text""], input[type=""email""]');
                        let passwordFields = document.querySelectorAll('input[type=""password""]');

                        if (usernameFields.length > 0 && passwordFields.length > 0) {{
                            usernameFields[0].value = '{username}';
                            passwordFields[0].value = '{password}';
                        }}
                    }})();
                ";
                await currentWebView.CoreWebView2.ExecuteScriptAsync(autofillScript);
            }

            string scriptToInject = @"
                (function() {
                    document.querySelectorAll('form').forEach(form => {
                        form.addEventListener('submit', (event) => {
                            let usernameInput = form.querySelector('input[type=""text""], input[type=""email""]');
                            let passwordInput = form.querySelector('input[type=""password""]');

                            if (usernameInput && passwordInput && usernameInput.value && passwordInput.value) {
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

        private async void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            var browserTab = GetBrowserTabItemFromWebView(currentWebView);
            if (browserTab != null && browserTab.IsIncognito) return;

            string message = e.WebMessageAsJson;
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(message))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("type", out JsonElement typeElement) && typeElement.GetString() == "loginSubmit")
                    {
                        string url = root.GetProperty("url").GetString();
                        string username = root.GetProperty("username").GetString();
                        string password = root.GetProperty("password").GetString();

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
                Debug.WriteLine($"Error al procesar mensaje web: {ex.Message}");
            }
        }


        public CoreWebView2Environment GetDefaultEnvironment()
        {
            return _defaultEnvironment;
        }


        public List<BrowserTabItem> GetBrowserTabItems()
        {
            return _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).ToList();
        }

        public void CloseBrowserTab(TabItem tabToClose)
        {
            Button closeButton = null;
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
                var browserTabItem = GetBrowserTabItemFromTabItem(tabToClose);
                if (browserTabItem != null)
                {
                    browserTabItem.LeftWebView?.Dispose();
                    browserTabItem.RightWebView?.Dispose();
                    browserTabItem.ParentGroup?.TabsInGroup.Remove(browserTabItem);

                    if (!browserTabItem.ParentGroup.TabsInGroup.Any() && _tabGroupManager.TabGroups.Count > 1)
                    {
                        _tabGroupManager.RemoveGroup(browserTabItem.ParentGroup);
                    }
                }

                if (!_tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Any())
                {
                    AddNewTab();
                }
                else
                {
                    if (SelectedTabItem == browserTabItem && _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Any())
                    {
                        SelectedTabItem = _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).First();
                        SelectedTabItem.Tab.IsSelected = true;
                    }
                }
            }
        }

        private TabItem GetCurrentBrowserTabItemInternal()
        {
            return SelectedTabItem?.Tab;
        }


        private async Task EnableSplitScreenForCurrentTab(BrowserTabItem tabItem, string rightPanelUrl)
        {
            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _isReadingAloud = false;
                ReadAloudButton.Content = "🔊";
            }

            WebView2 webView2 = new WebView2();
            webView2.Source = new Uri(rightPanelUrl);
            webView2.Name = "WebView2_Tab" + tabItem.ParentGroup.TabsInGroup.IndexOf(tabItem);
            webView2.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView2.VerticalAlignment = VerticalAlignment.Stretch;

            CoreWebView2Environment envToUse = tabItem.IsIncognito ? _incognitoEnvironment : _defaultEnvironment;
            webView2.CoreWebView2InitializationCompleted += (s, ev) => ConfigureCoreWebView2(webView2, ev, envToUse);

            await webView2.EnsureCoreWebView2Async(null);

            Grid splitGrid = new Grid();
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(tabItem.LeftWebView, 0);
            splitGrid.Children.Add(tabItem.LeftWebView);

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

            Grid.SetColumn(webView2, 2);
            splitGrid.Children.Add(webView2);

            tabItem.Tab.Content = splitGrid;
            tabItem.RightWebView = webView2;
            tabItem.IsSplit = true;
        }

        private void DisableSplitScreenForCurrentTab(BrowserTabItem tabItem)
        {
            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _isReadingAloud = false;
                ReadAloudButton.Content = "🔊";
            }

            Grid currentGrid = tabItem.Tab.Content as Grid;
            if (currentGrid != null)
            {
                currentGrid.Children.Remove(tabItem.LeftWebView);
            }

            if (tabItem.RightWebView != null)
            {
                tabItem.RightWebView.Dispose();
                tabItem.RightWebView = null;
            }

            Grid singleViewGrid = new Grid();
            singleViewGrid.Children.Add(tabItem.LeftWebView);
            tabItem.Tab.Content = singleViewGrid;
            tabItem.IsSplit = false;
        }

        private void IncognitoButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(_defaultHomePage, isIncognito: true);
        }

        public void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            Button closeButton = sender as Button;
            TabItem tabToClose = closeButton?.Tag as TabItem;

            if (tabToClose != null)
            {
                var browserTabItem = GetBrowserTabItemFromTabItem(tabToClose);

                if (browserTabItem != null)
                {
                    browserTabItem.ParentGroup?.TabsInGroup.Remove(browserTabItem);
                    browserTabItem.LeftWebView?.Dispose();
                    browserTabItem.RightWebView?.Dispose();

                    if (!browserTabItem.ParentGroup.TabsInGroup.Any() && _tabGroupManager.TabGroups.Count > 1)
                    {
                        _tabGroupManager.RemoveGroup(browserTabItem.ParentGroup);
                    }
                }

                if (!_tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Any())
                {
                    AddNewTab();
                }
                else
                {
                    if (SelectedTabItem == browserTabItem && _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Any())
                    {
                        SelectedTabItem = _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).First();
                        SelectedTabItem.Tab.IsSelected = true;
                    }
                }
            }
        }

        private void UrlTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // No se necesita código aquí si el ContextMenu está definido directamente en XAML.
        }

        private void OpenInNewTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(UrlTextBox.Text);
        }

        private void OpenInNewIncognitoTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(UrlTextBox.Text, isIncognito: true);
        }


        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            TabControl currentTabControl = sender as TabControl;
            if (currentTabControl != null && currentTabControl.SelectedItem is BrowserTabItem selectedBrowserTab)
            {
                SelectedTabItem = selectedBrowserTab;
                UpdateUrlTextBoxFromCurrentTab();

                if (_isReadingAloud)
                {
                    _speechSynthesizer.SpeakAsyncCancelAll();
                    _isReadingAloud = false;
                    ReadAloudButton.Content = "🔊";
                }

                SplitScreenButton.Content = selectedBrowserTab.IsSplit ? "➡️" : "↔️";

                if (selectedBrowserTab.LeftWebView == null)
                {
                    if (_isTabSuspensionEnabled)
                    {
                        string urlToReload = selectedBrowserTab.Tab.Tag?.ToString();

                        WebView2 newWebView = new WebView2();
                        newWebView.Source = new Uri(urlToReload ?? _defaultHomePage);
                        newWebView.Name = "WebView1_Tab" + (selectedBrowserTab.ParentGroup.TabsInGroup.IndexOf(selectedBrowserTab) + 1);
                        newWebView.HorizontalAlignment = HorizontalAlignment.Stretch;
                        newWebView.VerticalAlignment = VerticalAlignment.Stretch;

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
                        newWebView.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;


                        Grid tabContent = new Grid();
                        tabContent.Children.Add(newWebView);
                        selectedBrowserTab.Tab.Content = tabContent;

                        selectedBrowserTab.LeftWebView = newWebView;
                        selectedBrowserTab.RightWebView = null;
                        selectedBrowserTab.IsSplit = false;

                        string originalHeaderText = selectedBrowserTab.HeaderTextBlock.Text;
                        if (originalHeaderText.StartsWith("(Suspendida) "))
                        {
                            selectedBrowserTab.HeaderTextBlock.Text = originalHeaderText.Replace("(Suspendida) ", "");
                        }
                    }
                    else
                    {
                        string urlToReload = selectedBrowserTab.Tab.Tag?.ToString();
                        selectedBrowserTab.ParentGroup.TabsInGroup.Remove(selectedBrowserTab);
                        AddNewTab(urlToReload, selectedBrowserTab.IsIncognito, selectedBrowserTab.ParentGroup);
                    }
                }
            }
            _isFindBarVisible = false;
            FindBar.Visibility = Visibility.Collapsed;
            ClearFindResults();
        }

        private void UpdateUrlTextBoxFromCurrentTab()
        {
            WebView2 currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                UrlTextBox.Text = currentWebView.CoreWebView2.Source;
                this.Title = currentWebView.CoreWebView2.DocumentTitle + " - Aurora Browser";
                // Actualizar el texto en la barra de título personalizada
                WindowTitleText.Text = this.Title;
            }
            else
            {
                UrlTextBox.Text = string.Empty;
                this.Title = "Aurora Browser";
                WindowTitleText.Text = this.Title;
            }
        }

        public WebView2 GetCurrentWebView()
        {
            return SelectedTabItem?.LeftWebView;
        }

        private BrowserTabItem GetBrowserTabItemFromTabItem(TabItem tabItem)
        {
            return _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(bti => bti.Tab == tabItem);
        }

        private BrowserTabItem GetBrowserTabItemFromWebView(WebView2 webView)
        {
            return _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(bti => bti.LeftWebView == webView || bti.RightWebView == webView);
        }


        private void CheckAndSuggestTabSuspension()
        {
            const int MaxTabsBeforeSuggestion = 15;
            int activeTabs = _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Count(t => t.LeftWebView != null && !t.IsIncognito && !t.IsSplit);

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
                    SettingsWindow_OnSuspendInactiveTabs();
                }
            }
        }


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Pasar los colores actuales y la orientación de la barra al SettingsWindow
            SettingsWindow settingsWindow = new SettingsWindow(
                _defaultHomePage, AdBlocker.IsEnabled, _defaultSearchEngineUrl,
                _isTabSuspensionEnabled, _restoreSessionOnStartup, TrackerBlocker.IsEnabled,
                _isPdfViewerEnabled, BrowserBackgroundColor, BrowserForegroundColor, _currentToolbarPosition); // NUEVO: Pasar orientación

            settingsWindow.OnClearBrowseData += SettingsWindow_OnClearBrowseData;
            settingsWindow.OnSuspendInactiveTabs += SettingsWindow_OnSuspendInactiveTabs;
            settingsWindow.OnColorsChanged += SettingsWindow_OnColorsChanged;
            settingsWindow.OnToolbarPositionChanged += SettingsWindow_OnToolbarPositionChanged; // NUEVO: Suscribirse al evento de cambio de posición

            if (settingsWindow.ShowDialog() == true)
            {
                _defaultHomePage = settingsWindow.HomePage;
                AdBlocker.IsEnabled = settingsWindow.IsAdBlockerEnabled;
                _defaultSearchEngineUrl = settingsWindow.DefaultSearchEngineUrl;
                _isTabSuspensionEnabled = settingsWindow.IsTabSuspensionEnabled;
                _restoreSessionOnStartup = settingsWindow.RestoreSessionOnStartup;
                TrackerBlocker.IsEnabled = settingsWindow.IsTrackerProtectionEnabled;
                _isPdfViewerEnabled = settingsWindow.IsPdfViewerEnabled;
                // Los colores y la orientación se actualizan directamente a través de los eventos OnColorsChanged y OnToolbarPositionChanged
                SaveSettings();
                MessageBox.Show("Configuración guardada. Los cambios se aplicarán al abrir nuevas pestañas o al hacer clic en 'Inicio'.", "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            settingsWindow.OnClearBrowseData -= SettingsWindow_OnClearBrowseData;
            settingsWindow.OnSuspendInactiveTabs -= SettingsWindow_OnSuspendInactiveTabs;
            settingsWindow.OnColorsChanged -= SettingsWindow_OnColorsChanged;
            settingsWindow.OnToolbarPositionChanged -= SettingsWindow_OnToolbarPositionChanged; // NUEVO: Desuscribirse
        }

        /// <summary>
        /// Manejador para cuando los colores del tema cambian en la ventana de configuración.
        /// </summary>
        private void SettingsWindow_OnColorsChanged(Color backgroundColor, Color foregroundColor)
        {
            BrowserBackgroundColor = backgroundColor;
            BrowserForegroundColor = foregroundColor;
            SaveSettings(); // Guardar los nuevos colores inmediatamente
        }

        /// <summary>
        /// NUEVO: Manejador para cuando la posición de la barra de herramientas cambia en la ventana de configuración.
        /// </summary>
        private void SettingsWindow_OnToolbarPositionChanged(ToolbarPosition newPosition)
        {
            _currentToolbarPosition = newPosition;
            ApplyToolbarPosition(newPosition);
            SaveSettings();
        }


        private async void SettingsWindow_OnClearBrowseData()
        {
            WebView2 anyWebView = GetCurrentWebView();

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

                await _defaultEnvironment.ClearBrowseDataAsync(dataKinds);
                MessageBox.Show("Datos de navegación (caché, cookies, etc.) borrados con éxito.", "Limpieza Completa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No se pudo acceder al motor del navegador para borrar los datos del perfil normal.", "Error de Limpieza", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsWindow_OnSuspendInactiveTabs()
        {
            if (!_isTabSuspensionEnabled)
            {
                MessageBox.Show("La suspensión de pestañas no está habilitada en la configuración. Habilítela para usar esta función.", "Suspensión Deshabilitada", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var browserTab in _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).ToList())
            {
                if (browserTab != SelectedTabItem && !browserTab.IsIncognito && !browserTab.IsSplit)
                {
                    if (browserTab.LeftWebView != null && browserTab.LeftWebView.CoreWebView2 != null)
                    {
                        string suspendedUrl = browserTab.LeftWebView.Source.OriginalString;

                        browserTab.LeftWebView.Dispose();
                        browserTab.LeftWebView = null;

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
                        browserTab.Tab.Tag = suspendedUrl;

                        string originalHeaderText = browserTab.HeaderTextBlock.Text;
                        if (!originalHeaderText.StartsWith("(Suspendida) "))
                        {
                            browserTab.HeaderTextBlock.Text = "(Suspendida) " + originalHeaderText;
                        }
                    }
                }
            }
        }

        private void LoadSettings()
        {
            string savedHomePage = ConfigurationManager.AppSettings[HomePageSettingKey];
            if (!string.IsNullOrEmpty(savedHomePage))
            {
                _defaultHomePage = savedHomePage;
            }

            string savedAdBlockerState = ConfigurationManager.AppSettings[AdBlockerSettingKey];
            if (bool.TryParse(savedAdBlockerState, out bool isEnabled))
            {
                AdBlocker.IsEnabled = isEnabled;
            }
            else
            {
                AdBlocker.IsEnabled = false;
            }

            string savedSearchEngineUrl = ConfigurationManager.AppSettings[DefaultSearchEngineSettingKey];
            if (!string.IsNullOrEmpty(savedSearchEngineUrl))
            {
                _defaultSearchEngineUrl = savedSearchEngineUrl;
            }

            string savedTabSuspensionState = ConfigurationManager.AppSettings[TabSuspensionSettingKey];
            if (bool.TryParse(savedTabSuspensionState, out bool isTabSuspensionEnabled))
            {
                _isTabSuspensionEnabled = isTabSuspensionEnabled;
            }
            else
            {
                _isTabSuspensionEnabled = false;
            }

            string savedRestoreSessionState = ConfigurationManager.AppSettings[RestoreSessionSettingKey];
            if (bool.TryParse(savedRestoreSessionState, out bool restoreSession))
            {
                _restoreSessionOnStartup = restoreSession;
            }
            else
            {
                _restoreSessionOnStartup = true;
            }

            string savedTrackerProtectionState = ConfigurationManager.AppSettings[TrackerProtectionSettingKey];
            if (bool.TryParse(savedTrackerProtectionState, out bool isTrackerProtectionEnabled))
            {
                TrackerBlocker.IsEnabled = isTrackerProtectionEnabled;
            }
            else
            {
                TrackerBlocker.IsEnabled = false;
            }

            string savedPdfViewerState = ConfigurationManager.AppSettings[PdfViewerSettingKey];
            if (bool.TryParse(savedPdfViewerState, out bool isPdfViewerEnabled))
            {
                _isPdfViewerEnabled = isPdfViewerEnabled;
            }
            else
            {
                _isPdfViewerEnabled = true;
            }

            // Cargar colores del tema
            if (ConfigurationManager.AppSettings[BrowserBackgroundColorKey] != null &&
                ColorConverter.ConvertFromString(ConfigurationManager.AppSettings[BrowserBackgroundColorKey]) is Color bgColor)
            {
                BrowserBackgroundColor = bgColor;
            }
            else
            {
                BrowserBackgroundColor = (Color)Application.Current.Resources["BrowserBackgroundColor"]; // Usar el valor por defecto de XAML
            }

            if (ConfigurationManager.AppSettings[BrowserForegroundColorKey] != null &&
                ColorConverter.ConvertFromString(ConfigurationManager.AppSettings[BrowserForegroundColorKey]) is Color fgColor)
            {
                BrowserForegroundColor = fgColor;
            }
            else
            {
                BrowserForegroundColor = (Color)Application.Current.Resources["BrowserForegroundColor"]; // Usar el valor por defecto de XAML
            }

            // NUEVO: Cargar orientación de la barra de herramientas
            string savedToolbarPosition = ConfigurationManager.AppSettings[ToolbarOrientationKey];
            if (Enum.TryParse(savedToolbarPosition, out ToolbarPosition position))
            {
                _currentToolbarPosition = position;
            }
            else
            {
                _currentToolbarPosition = ToolbarPosition.Top; // Por defecto
            }
        }

        private void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (config.AppSettings.Settings[HomePageSettingKey] == null)
                config.AppSettings.Settings.Add(HomePageSettingKey, _defaultHomePage);
            else
                config.AppSettings.Settings[HomePageSettingKey].Value = _defaultHomePage;

            if (config.AppSettings.Settings[AdBlockerSettingKey] == null)
                config.AppSettings.Settings.Add(AdBlockerSettingKey, AdBlocker.IsEnabled.ToString());
            else
                config.AppSettings.Settings[AdBlockerSettingKey].Value = AdBlocker.IsEnabled.ToString();

            if (config.AppSettings.Settings[DefaultSearchEngineSettingKey] == null)
                config.AppSettings.Settings.Add(DefaultSearchEngineSettingKey, _defaultSearchEngineUrl);
            else
                config.AppSettings.Settings[DefaultSearchEngineSettingKey].Value = _defaultSearchEngineUrl;

            if (config.AppSettings.Settings[TabSuspensionSettingKey] == null)
                config.AppSettings.Settings.Add(TabSuspensionSettingKey, _isTabSuspensionEnabled.ToString());
            else
                config.AppSettings.Settings[TabSuspensionSettingKey].Value = _isTabSuspensionEnabled.ToString();

            if (config.AppSettings.Settings[RestoreSessionSettingKey] == null)
                config.AppSettings.Settings.Add(RestoreSessionSettingKey, _restoreSessionOnStartup.ToString());
            else
                config.AppSettings.Settings[RestoreSessionSettingKey].Value = _restoreSessionOnStartup.ToString();

            if (config.AppSettings.Settings[TrackerProtectionSettingKey] == null)
                config.AppSettings.Settings.Add(TrackerProtectionSettingKey, TrackerBlocker.IsEnabled.ToString());
            else
                config.AppSettings.Settings[TrackerProtectionSettingKey].Value = TrackerBlocker.IsEnabled.ToString();

            if (config.AppSettings.Settings[PdfViewerSettingKey] == null)
                config.AppSettings.Settings.Add(PdfViewerSettingKey, _isPdfViewerEnabled.ToString());
            else
                config.AppSettings.Settings[PdfViewerSettingKey].Value = _isPdfViewerEnabled.ToString();

            // Guardar colores del tema
            if (config.AppSettings.Settings[BrowserBackgroundColorKey] == null)
                config.AppSettings.Settings.Add(BrowserBackgroundColorKey, BrowserBackgroundColor.ToString());
            else
                config.AppSettings.Settings[BrowserBackgroundColorKey].Value = BrowserBackgroundColor.ToString();

            if (config.AppSettings.Settings[BrowserForegroundColorKey] == null)
                config.AppSettings.Settings.Add(BrowserForegroundColorKey, BrowserForegroundColor.ToString());
            else
                config.AppSettings.Settings[BrowserForegroundColorKey].Value = BrowserForegroundColor.ToString();

            // NUEVO: Guardar orientación de la barra de herramientas
            if (config.AppSettings.Settings[ToolbarOrientationKey] == null)
                config.AppSettings.Settings.Add(ToolbarOrientationKey, _currentToolbarPosition.ToString());
            else
                config.AppSettings.Settings[ToolbarOrientationKey].Value = _currentToolbarPosition.ToString();


            if (_restoreSessionOnStartup)
            {
                List<string> currentUrls = new List<string>();
                foreach (var tab in _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup))
                {
                    if (!tab.IsIncognito && tab.LeftWebView != null && tab.LeftWebView.CoreWebView2 != null)
                    {
                        currentUrls.Add(tab.LeftWebView.Source.OriginalString);
                    }
                    else if (!tab.IsIncognito && tab.LeftWebView == null && tab.Tab.Tag is string suspendedUrl)
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
                if (config.AppSettings.Settings[LastSessionUrlsSettingKey] != null)
                {
                    config.AppSettings.Settings.Remove(LastSessionUrlsSettingKey);
                }
            }


            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
            _extensionManager.SaveExtensionsState();
            UpdateUncleanShutdownFlag(false);

            if (_speechSynthesizer != null)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _speechSynthesizer.Dispose();
                _speechSynthesizer = null;
            }

            foreach (var tab in _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup))
            {
                tab.LeftWebView?.Dispose();
                tab.RightWebView?.Dispose();
            }
            _tabGroupManager.TabGroups.Clear();

            if (_incognitoEnvironment != null)
            {
                string incognitoUserDataFolder = _incognitoEnvironment.UserDataFolder;
                _incognitoEnvironment = null;
                try
                {
                    if (Directory.Exists(incognitoUserDataFolder))
                    {
                        Directory.Delete(incognitoUserDataFolder, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al eliminar la carpeta de datos de incógnito: {ex.Message}");
                }
            }
            if (_defaultEnvironment != null)
            {
                _defaultEnvironment = null;
            }
        }

        // ---------- NUEVO: Lógica para Custom Window Chrome y Redimensionamiento ----------
        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    // Restaurar al tamaño normal al arrastrar si está maximizada
                    this.WindowState = WindowState.Normal;
                    UpdateMaximizeRestoreButtonContent();
                }
                this.DragMove();
            }
        }

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
            UpdateMaximizeRestoreButtonContent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            UpdateMaximizeRestoreButtonContent();
        }

        private void UpdateMaximizeRestoreButtonContent()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeRestoreButton.Content = "❐"; // Ícono de restaurar
                MaximizeRestoreButton.ToolTip = "Restaurar";
            }
            else
            {
                MaximizeRestoreButton.Content = "⬜"; // Ícono de maximizar
                MaximizeRestoreButton.ToolTip = "Maximizar";
            }
        }

        private HwndSource _hwndSource;
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            this.SourceInitialized += (s, args) =>
            {
                _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                if (_hwndSource != null)
                {
                    _hwndSource.AddHook(HwndSourceHook);
                }
            };
        }

        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    handled = true;
                    return (IntPtr)HitTest(lParam.ToInt32());
            }
            return IntPtr.Zero;
        }

        private int HitTest(int lParam)
        {
            Point p = PointFromScreen(new Point((lParam << 16) >> 16, lParam & 0xffff));
            double borderWidth = 5; // Ancho del borde para redimensionar
            double captionHeight = 30; // Altura de la barra de título personalizada

            if (p.Y < borderWidth)
            {
                if (p.X < borderWidth) return HTTOPLEFT;
                if (p.X > this.ActualWidth - borderWidth) return HTTOPRIGHT;
                return HTTOP;
            }
            if (p.Y > this.ActualHeight - borderWidth)
            {
                if (p.X < borderWidth) return HTBOTTOMLEFT;
                if (p.X > this.ActualWidth - borderWidth) return HTBOTTOMRIGHT;
                return HTBOTTOM;
            }
            if (p.X < borderWidth) return HTLEFT;
            if (p.X > this.ActualWidth - borderWidth) return HTRIGHT;

            if (p.Y < captionHeight) return HTCAPTION; // Área de arrastre de la ventana

            return HTCAPTION; // Si no está en un borde, se puede arrastrar desde cualquier parte de la "barra de título"
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr handle = (new WindowInteropHelper(this)).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }

        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024: // WM_GETMINMAXINFO
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return (IntPtr)0;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Ajusta los valores para las dimensiones mínimas de la ventana
            mmi.ptMinTrackSize.x = (int)SystemParameters.MinimumWindowWidth;
            mmi.ptMinTrackSize.y = (int)SystemParameters.MinimumWindowHeight;

            // En un entorno de múltiples pantallas, se calcula el área de trabajo de la pantalla principal
            // para que la ventana maximizada no se extienda por encima de la barra de tareas.
            var workArea = SystemParameters.WorkArea;
            mmi.ptMaxPosition.x = (int)workArea.Left;
            mmi.ptMaxPosition.y = (int)workArea.Top;
            mmi.ptMaxSize.x = (int)workArea.Width;
            mmi.ptMaxSize.y = (int)workArea.Height;

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
        // ---------- FIN NUEVO: Lógica para Custom Window Chrome ----------


        // NUEVO: Método para aplicar la orientación de la barra de herramientas
        private void ApplyToolbarPosition(ToolbarPosition position)
        {
            Grid mainGrid = (Grid)((Border)this.Content).Child; // Accede al Grid principal dentro del Border

            // Limpiar Grid.Row/Column y DockPanel.Dock de MainToolbarContainer y TabGroupContainer
            // Esto es crucial para reasignar correctamente
            Grid.SetRow(MainToolbarContainer, 0); // Reset a 0 para quitar posibles asignaciones laterales
            Grid.SetColumn(MainToolbarContainer, 0);

            Grid.SetRow(TabGroupContainer, 0); // Reset a 0 para quitar posibles asignaciones
            Grid.SetColumn(TabGroupContainer, 0);

            // Eliminar ColumnDefinitions/RowDefinitions temporales si existieran
            // Reestablecer las definiciones de filas y columnas del Grid principal de la ventana
            mainGrid.ColumnDefinitions.Clear();
            mainGrid.RowDefinitions.Clear();

            // Recrear las definiciones de filas para la barra de título, barra de herramientas y contenido
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Barra de título personalizada
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Barra de herramientas
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Contenido (pestañas)

            // Asegurar que MainToolbarContainer esté en la fila 1 por defecto (debajo de la barra de título)
            Grid.SetRow(MainToolbarContainer, 1);
            Grid.SetColumn(MainToolbarContainer, 0); // Asegurarse de que ocupe toda la columna si es top/bottom

            // Asegurar que TabGroupContainer esté en la fila 2 por defecto
            Grid.SetRow(TabGroupContainer, 2);
            Grid.SetColumn(TabGroupContainer, 0);

            // Reiniciar visibilidad y ancho/alto de los placeholders laterales
            LeftToolbarPlaceholder.Visibility = Visibility.Collapsed;
            LeftToolbarPlaceholder.Width = 0;
            RightToolbarPlaceholder.Visibility = Visibility.Collapsed;
            RightToolbarPlaceholder.Width = 0;

            // Mover los botones a un StackPanel temporal para facilitar la reasignación
            List<UIElement> toolbarButtons = MainToolbarContainer.Children.OfType<StackPanel>().Where(sp => DockPanel.GetDock(sp) == Dock.Left).FirstOrDefault()?.Children.OfType<Button>().ToList() ?? new List<Button>();
            List<UIElement> rightToolbarButtons = MainToolbarContainer.Children.OfType<StackPanel>().Where(sp => DockPanel.GetDock(sp) == Dock.Right).FirstOrDefault()?.Children.OfType<Button>().ToList() ?? new List<Button>();
            UIElement urlAndProgressGrid = MainToolbarContainer.Children.OfType<Grid>().FirstOrDefault();


            // Limpiar los StackPanels antes de rellenarlos
            ((StackPanel)MainToolbarContainer.Children.OfType<StackPanel>().Where(sp => DockPanel.GetDock(sp) == Dock.Left).FirstOrDefault())?.Children.Clear();
            ((StackPanel)MainToolbarContainer.Children.OfType<StackPanel>().Where(sp => DockPanel.GetDock(sp) == Dock.Right).FirstOrDefault())?.Children.Clear();


            switch (position)
            {
                case ToolbarPosition.Top:
                    MainToolbarContainer.Visibility = Visibility.Visible;
                    Grid.SetRow(MainToolbarContainer, 1);
                    Grid.SetColumn(MainToolbarContainer, 0); // Asegura que esté en la columna 0 si hay columnas adicionales
                    MainToolbarContainer.Orientation = Orientation.Horizontal;
                    MainToolbarContainer.Height = Double.NaN; // Altura automática
                    MainToolbarContainer.Width = Double.NaN; // Ancho automático (para ocupar todo el espacio)
                    MainToolbarContainer.LastChildFill = true; // El Grid de URL y progreso ocupa el espacio restante
                    MainToolbarContainer.BorderThickness = new Thickness(0, 0, 0, 1); // Borde inferior

                    // Establecer ColumnDefinitions para MainToolbarContainer
                    // Esto es necesario para que el Grid del URL ocupe el espacio restante
                    MainToolbarContainer.Children.Clear();
                    DockPanel toolbarDockPanel = new DockPanel();
                    StackPanel leftButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    StackPanel rightButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

                    foreach (var btn in toolbarButtons) leftButtonsPanel.Children.Add(btn);
                    foreach (var btn in rightToolbarButtons) rightButtonsPanel.Children.Add(btn);

                    DockPanel.SetDock(leftButtonsPanel, Dock.Left);
                    DockPanel.SetDock(rightButtonsPanel, Dock.Right);
                    if (urlAndProgressGrid != null) DockPanel.SetDock(urlAndProgressGrid, Dock.Left); // Se rellena automáticamente

                    toolbarDockPanel.Children.Add(leftButtonsPanel);
                    toolbarDockPanel.Children.Add(rightButtonsPanel);
                    if (urlAndProgressGrid != null) toolbarDockPanel.Children.Add(urlAndProgressGrid);
                    MainToolbarContainer.Children.Add(toolbarDockPanel);

                    // Pestañas ocupan la fila 2
                    Grid.SetRow(TabGroupContainer, 2);
                    Grid.SetColumn(TabGroupContainer, 0);

                    // Ajustar el margen del FindBar si la barra de herramientas cambia de ancho/alto
                    FindBar.Margin = new Thickness(10); // Margen predeterminado
                    Grid.SetRow(FindBar, 2); // Asegurar que el FindBar esté en la fila de contenido principal
                    Grid.SetColumn(FindBar, 0);

                    break;

                case ToolbarPosition.Left:
                    MainToolbarContainer.Visibility = Visibility.Collapsed; // Ocultar la barra de herramientas superior
                    LeftToolbarPlaceholder.Visibility = Visibility.Visible;
                    LeftToolbarPlaceholder.Width = 150; // Ancho fijo para la barra lateral
                    LeftToolbarPlaceholder.Height = Double.NaN; // Altura automática

                    // Reorganizar Grid principal para acomodar la barra lateral
                    mainGrid.ColumnDefinitions.Clear();
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Barra izquierda
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Contenido

                    // Mover la barra de título a la primera columna, ocupando ambas columnas
                    Grid.SetColumn(mainGrid.Children.OfType<Grid>().First(), 0); // Barra de título personalizada
                    Grid.SetColumnSpan(mainGrid.Children.OfType<Grid>().First(), 2);

                    // Asignar el placeholder de la izquierda a su lugar
                    Grid.SetRow(LeftToolbarPlaceholder, 1); // Debajo de la barra de título
                    Grid.SetColumn(LeftToolbarPlaceholder, 0);
                    Grid.SetRowSpan(LeftToolbarPlaceholder, 2); // Ocupar todo el espacio vertical restante

                    // Mover los botones al LeftToolbarPlaceholder
                    StackPanel leftToolbarContent = new StackPanel { Orientation = Orientation.Vertical };
                    foreach (var btn in toolbarButtons.Concat(rightToolbarButtons).OfType<Button>()) // Mover todos los botones aquí
                    {
                        btn.Width = Double.NaN; // Permitir que el botón se ajuste al ancho del StackPanel
                        btn.Height = 30; // Altura estándar para botones verticales
                        btn.Margin = new Thickness(5); // Espacio entre botones
                        leftToolbarContent.Children.Add(btn);
                    }
                    if (urlAndProgressGrid != null)
                    {
                        urlAndProgressGrid.Width = Double.NaN; // Ajustar a ancho
                        urlAndProgressGrid.Height = 80; // Altura para el textbox de URL
                        urlAndProgressGrid.Margin = new Thickness(5);
                        leftToolbarContent.Children.Insert(0, urlAndProgressGrid); // Poner la URL arriba
                    }
                    LeftToolbarPlaceholder.Children.Add(leftToolbarContent);


                    // Las pestañas ahora ocupan la columna 1
                    Grid.SetRow(TabGroupContainer, 1); // Fila 1 (debajo de la barra de título)
                    Grid.SetColumn(TabGroupContainer, 1);
                    Grid.SetRowSpan(TabGroupContainer, 2); // Ocupar todo el espacio vertical restante

                    FindBar.Margin = new Thickness(10, 10, 10, 10); // Ajustar el margen para la nueva posición
                    Grid.SetRow(FindBar, 2);
                    Grid.SetColumn(FindBar, 1);
                    break;

                case ToolbarPosition.Right:
                    MainToolbarContainer.Visibility = Visibility.Collapsed;
                    RightToolbarPlaceholder.Visibility = Visibility.Visible;
                    RightToolbarPlaceholder.Width = 150;
                    RightToolbarPlaceholder.Height = Double.NaN;

                    mainGrid.ColumnDefinitions.Clear();
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Contenido
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Barra derecha

                    Grid.SetColumn(mainGrid.Children.OfType<Grid>().First(), 0); // Barra de título personalizada
                    Grid.SetColumnSpan(mainGrid.Children.OfType<Grid>().First(), 2);

                    Grid.SetRow(RightToolbarPlaceholder, 1);
                    Grid.SetColumn(RightToolbarPlaceholder, 1);
                    Grid.SetRowSpan(RightToolbarPlaceholder, 2);

                    StackPanel rightToolbarContent = new StackPanel { Orientation = Orientation.Vertical };
                    foreach (var btn in toolbarButtons.Concat(rightToolbarButtons).OfType<Button>())
                    {
                        btn.Width = Double.NaN;
                        btn.Height = 30;
                        btn.Margin = new Thickness(5);
                        rightToolbarContent.Children.Add(btn);
                    }
                    if (urlAndProgressGrid != null)
                    {
                        urlAndProgressGrid.Width = Double.NaN;
                        urlAndProgressGrid.Height = 80;
                        urlAndProgressGrid.Margin = new Thickness(5);
                        rightToolbarContent.Children.Insert(0, urlAndProgressGrid);
                    }
                    RightToolbarPlaceholder.Children.Add(rightToolbarContent);

                    Grid.SetRow(TabGroupContainer, 1);
                    Grid.SetColumn(TabGroupContainer, 0);
                    Grid.SetRowSpan(TabGroupContainer, 2);

                    FindBar.Margin = new Thickness(10, 10, 10, 10);
                    Grid.SetRow(FindBar, 2);
                    Grid.SetColumn(FindBar, 0);
                    break;

                case ToolbarPosition.Bottom:
                    MainToolbarContainer.Visibility = Visibility.Visible;
                    Grid.SetRow(MainToolbarContainer, 2); // Mover a la fila 2 (debajo de las pestañas)
                    Grid.SetColumn(MainToolbarContainer, 0);
                    MainToolbarContainer.Orientation = Orientation.Horizontal;
                    MainToolbarContainer.Height = Double.NaN;
                    MainToolbarContainer.Width = Double.NaN;
                    MainToolbarContainer.LastChildFill = true;
                    MainToolbarContainer.BorderThickness = new Thickness(0, 1, 0, 0); // Borde superior

                    // Eliminar las filas 1 y 2 del Grid principal y volver a añadir las necesarias
                    mainGrid.RowDefinitions.Clear();
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Barra de título personalizada
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Contenido (pestañas)
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Barra de herramientas (abajo)

                    // Reestablecer la asignación de filas
                    Grid.SetRow(mainGrid.Children.OfType<Grid>().First(), 0); // Barra de título personalizada
                    Grid.SetRow(TabGroupContainer, 1); // Pestañas ahora en la fila 1
                    Grid.SetRow(MainToolbarContainer, 2); // Barra de herramientas abajo en la fila 2

                    // Volver a llenar el DockPanel
                    MainToolbarContainer.Children.Clear();
                    toolbarDockPanel = new DockPanel();
                    leftButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    rightButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

                    foreach (var btn in toolbarButtons) leftButtonsPanel.Children.Add(btn);
                    foreach (var btn in rightToolbarButtons) rightButtonsPanel.Children.Add(btn);

                    DockPanel.SetDock(leftButtonsPanel, Dock.Left);
                    DockPanel.SetDock(rightButtonsPanel, Dock.Right);
                    if (urlAndProgressGrid != null) DockPanel.SetDock(urlAndProgressGrid, Dock.Left);

                    toolbarDockPanel.Children.Add(leftButtonsPanel);
                    toolbarDockPanel.Children.Add(rightButtonsPanel);
                    if (urlAndProgressGrid != null) toolbarDockPanel.Children.Add(urlAndProgressGrid);
                    MainToolbarContainer.Children.Add(toolbarDockPanel);

                    FindBar.Margin = new Thickness(10); // Ajustar el margen del FindBar
                    Grid.SetRow(FindBar, 1); // Asegurar que el FindBar esté en la fila de contenido principal
                    Grid.SetColumn(FindBar, 0);
                    break;
            }
            // Asegúrate de que el FindBar siempre esté en el mismo Grid.Row/Column del contenido principal
            // Esto es un poco hacky pero funciona con las reconfiguraciones de Grid
            Grid.SetRow(FindBar, Grid.GetRow(TabGroupContainer));
            Grid.SetColumn(FindBar, Grid.GetColumn(TabGroupContainer));
        }

        private void ClearToolbarButtons()
        {
            // Mover los botones de vuelta a un lugar temporal antes de reasignarlos
            List<UIElement> currentButtons = new List<UIElement>();

            // Obtener botones de MainToolbarContainer si es visible
            if (MainToolbarContainer.Visibility == Visibility.Visible)
            {
                var leftPanel = MainToolbarContainer.Children.OfType<DockPanel>().FirstOrDefault()?.Children.OfType<StackPanel>().Where(sp => DockPanel.GetDock(sp) == Dock.Left).FirstOrDefault();
                if (leftPanel != null) currentButtons.AddRange(leftPanel.Children.OfType<Button>().ToList());

                var rightPanel = MainToolbarContainer.Children.OfType<DockPanel>().FirstOrDefault()?.Children.OfType<StackPanel>().Where(sp => DockPanel.GetDock(sp) == Dock.Right).FirstOrDefault();
                if (rightPanel != null) currentButtons.AddRange(rightPanel.Children.OfType<Button>().ToList());
            }
            // Obtener botones de LeftToolbarPlaceholder si es visible
            else if (LeftToolbarPlaceholder.Visibility == Visibility.Visible)
            {
                var contentPanel = LeftToolbarPlaceholder.Children.OfType<StackPanel>().FirstOrDefault();
                if (contentPanel != null)
                {
                    currentButtons.AddRange(contentPanel.Children.OfType<Button>().ToList());
                    var urlGrid = contentPanel.Children.OfType<Grid>().FirstOrDefault();
                    if (urlGrid != null) currentButtons.Insert(0, urlGrid); // Mover el grid de URL también
                }
            }
            // Obtener botones de RightToolbarPlaceholder si es visible
            else if (RightToolbarPlaceholder.Visibility == Visibility.Visible)
            {
                var contentPanel = RightToolbarPlaceholder.Children.OfType<StackPanel>().FirstOrDefault();
                if (contentPanel != null)
                {
                    currentButtons.AddRange(contentPanel.Children.OfType<Button>().ToList());
                    var urlGrid = contentPanel.Children.OfType<Grid>().FirstOrDefault();
                    if (urlGrid != null) currentButtons.Insert(0, urlGrid);
                }
            }

            // Limpiar todos los contenedores
            MainToolbarContainer.Children.Clear();
            LeftToolbarPlaceholder.Children.Clear();
            RightToolbarPlaceholder.Children.Clear();

            // Re-agregar los botones a los StackPanels del DockPanel principal
            // Esto es un parche para que siempre existan los paneles y se puedan añadir elementos.
            // Una forma más robusta sería mantener una referencia a los StackPanels y moverlos.
            // Por simplicidad, los recreamos si es necesario o los llenamos si ya existen.
            DockPanel toolbarDockPanel = new DockPanel();
            StackPanel leftButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            StackPanel rightButtonsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid urlAndProgressGrid = this.FindName("UrlTextBox") is TextBox urlTxtBox ? (Grid)urlTxtBox.Parent : null; // Encuentra el Grid del URL

            foreach (var element in currentButtons)
            {
                if (element is Button btn)
                {
                    // Restablecer estilos y márgenes por defecto para el layout horizontal
                    btn.Width = 30;
                    btn.Height = 25;
                    btn.Margin = new Thickness(0,0,5,0);
                    // Decidir si va a la izquierda o a la derecha (puedes usar el nombre del botón o una propiedad)
                    // Por simplicidad, todos los botones excepto los de "Utilidad" (Incognito, AddBookmark, NewTab, Settings) van a la izquierda
                    if (btn.Name == "IncognitoButton" || btn.Name == "AddBookmarkButton" || btn.Name == "NewTabButton" || btn.Name == "SettingsButton")
                    {
                        rightButtonsPanel.Children.Add(btn);
                    }
                    else
                    {
                        leftButtonsPanel.Children.Add(btn);
                    }
                }
                else if (element is Grid grid && grid.Name == "UrlTextBox") // Asumiendo que el Grid de URL tiene un nombre o se puede identificar
                {
                     // No lo añadimos aquí, se añadirá por separado en el MainToolbarContainer
                }
            }

            // Asegurarse de que los StackPanels y el Grid de URL siempre existan en el DockPanel
            DockPanel.SetDock(leftButtonsPanel, Dock.Left);
            DockPanel.SetDock(rightButtonsPanel, Dock.Right);
            if (urlAndProgressGrid != null)
            {
                DockPanel.SetDock(urlAndProgressGrid, Dock.Left); // Este es el que llenará el espacio
                toolbarDockPanel.Children.Add(leftButtonsPanel);
                toolbarDockPanel.Children.Add(rightButtonsPanel);
                toolbarDockPanel.Children.Add(urlAndProgressGrid); // El último hijo en DockPanel.Left ocupará el espacio restante
            }
            else
            {
                toolbarDockPanel.Children.Add(leftButtonsPanel);
                toolbarDockPanel.Children.Add(rightButtonsPanel);
            }

            // Añadir el DockPanel al MainToolbarContainer (si es que la barra de herramientas principal está siendo usada)
            // Esto solo se hace si el destino es Top/Bottom
            if (MainToolbarContainer.Visibility == Visibility.Visible)
            {
                MainToolbarContainer.Children.Add(toolbarDockPanel);
            }
            // Si el destino es Left/Right, los botones se manejarán en los placeholders
            else
            {
                if (LeftToolbarPlaceholder.Visibility == Visibility.Visible)
                {
                    var newContent = new StackPanel { Orientation = Orientation.Vertical };
                    if (urlAndProgressGrid != null)
                    {
                        urlAndProgressGrid.Width = Double.NaN;
                        urlAndProgressGrid.Height = 80;
                        urlAndProgressGrid.Margin = new Thickness(5);
                        newContent.Children.Add(urlAndProgressGrid);
                    }
                    foreach (var btn in leftButtonsPanel.Children.OfType<Button>().Concat(rightButtonsPanel.Children.OfType<Button>()))
                    {
                        btn.Width = Double.NaN;
                        btn.Height = 30;
                        btn.Margin = new Thickness(5);
                        newContent.Children.Add(btn);
                    }
                    LeftToolbarPlaceholder.Children.Add(newContent);
                }
                else if (RightToolbarPlaceholder.Visibility == Visibility.Visible)
                {
                    var newContent = new StackPanel { Orientation = Orientation.Vertical };
                    if (urlAndProgressGrid != null)
                    {
                        urlAndProgressGrid.Width = Double.NaN;
                        urlAndProgressGrid.Height = 80;
                        urlAndProgressGrid.Margin = new Thickness(5);
                        newContent.Children.Add(urlAndProgressGrid);
                    }
                    foreach (var btn in leftButtonsPanel.Children.OfType<Button>().Concat(rightButtonsPanel.Children.OfType<Button>()))
                    {
                        btn.Width = Double.NaN;
                        btn.Height = 30;
                        btn.Margin = new Thickness(5);
                        newContent.Children.Add(btn);
                    }
                    RightToolbarPlaceholder.Children.Add(newContent);
                }
            }
        }

    }

    /// <summary>
    /// Clase auxiliar para implementar ICommand de forma sencilla.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    /// <summary>
    /// NUEVO: Enumeración para la posición de la barra de herramientas.
    /// </summary>
    public enum ToolbarPosition
    {
        Top,
        Left,
        Right,
        Bottom
    }
}
