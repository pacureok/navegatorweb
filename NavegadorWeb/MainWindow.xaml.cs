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
    // NO necesitamos una declaración 'public partial class MainWindow : Window' adicional aquí.
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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
        private const string ToolbarOrientationKey = "ToolbarOrientation";

        private string _defaultSearchEngineUrl = "https://www.google.com/search?q=";
        private bool _isTabSuspensionEnabled = false;
        private bool _restoreSessionOnStartup = true;
        private bool _isPdfViewerEnabled = true;
        private ToolbarPosition _currentToolbarPosition = ToolbarPosition.Top;

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
                    if (mainGrid != null && mainGrid.RowDefinitions.Count > 0)
                    {
                        if (mainGrid.Children[0] is Grid titleBarGrid)
                        {
                            titleBarGrid.Background = new SolidColorBrush(value);
                        }
                    }
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
                    ApplyForegroundToWindowControls();
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


        private CoreWebView2Environment? _defaultEnvironment;
        private CoreWebView2Environment? _incognitoEnvironment;

        private string _readerModeScript = string.Empty;
        private string _darkModeScript = string.Empty;
        private string _pageColorExtractionScript = string.Empty;
        private string _microphoneControlScript = string.Empty;

        private SpeechSynthesizer _speechSynthesizer;
        private bool _isReadingAloud = false;

        private bool _isFindBarVisible = false; // Declaración única aquí
        private CoreWebView2FindInPage? _findInPage;

        private string? _lastFailedUrl = null;
        private System.Timers.Timer? _connectivityTimer;
        private bool _isOfflineGameActive = false;

        private bool _isGeminiModeActive = false;


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
            InitializeComponent(); // Esto inicializa los elementos XAML y los asigna a sus campos.
            _tabGroupManager = new TabGroupManager();
            _extensionManager = new ExtensionManager();
            this.DataContext = this;

            LoadSettings();
            InitializeEnvironments();
            LoadReaderModeScript();
            LoadDarkModeScript();
            LoadPageColorExtractionScript();
            LoadMicrophoneControlScript();

            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();
            _speechSynthesizer.SpeakCompleted += SpeechSynthesizer_SpeakCompleted;

            InitializeCommands();

            this.SourceInitialized += MainWindow_SourceInitialized;
            this.StateChanged += MainWindow_StateChanged;
            ApplyForegroundToWindowControls();
            ApplyToolbarPosition(_currentToolbarPosition);

            _connectivityTimer = new System.Timers.Timer(5000);
            _connectivityTimer.Elapsed += ConnectivityTimer_Elapsed;
            _connectivityTimer.AutoReset = true;
            _connectivityTimer.Enabled = false;
        }

        private void ApplyForegroundToWindowControls()
        {
            if (MaximizeRestoreButton != null)
            {
                MaximizeRestoreButton.Foreground = BrowserForegroundColor != null ? new SolidColorBrush(BrowserForegroundColor) : Brushes.Black;
                MinimizeButton.Foreground = BrowserForegroundColor != null ? new SolidColorBrush(BrowserForegroundColor) : Brushes.Black;
                CloseButton.Foreground = BrowserForegroundColor != null ? new SolidColorBrush(BrowserForegroundColor) : Brushes.Black;
            }
            if (WindowTitleText != null)
            {
                WindowTitleText.Foreground = BrowserForegroundColor != null ? new SolidColorBrush(BrowserForegroundColor) : Brushes.Black;
            }
            UpdateToolbarButtonForeground();
        }

        private void UpdateToolbarButtonForeground()
        {
            var navigationButtons = (AddressBar.Parent as StackPanel)?.Children.OfType<Button>();
            if (navigationButtons != null)
            {
                foreach (var child in navigationButtons)
                {
                    if (child != CloseButton)
                    {
                        child.Foreground = new SolidColorBrush(BrowserForegroundColor);
                    }
                }
            }

            if (AddressBar != null) AddressBar.Foreground = new SolidColorBrush(BrowserForegroundColor);
            // Asegúrate de que FindTextBox y FindResultsTextBlock existan en XAML si los usas.
            if (FindTextBox != null) FindTextBox.Foreground = new SolidColorBrush(BrowserForegroundColor);
            if (FindResultsTextBlock != null) FindResultsTextBlock.Foreground = new SolidColorBrush(BrowserForegroundColor);
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

        private void ToggleFullscreen(object? parameter)
        {
            if (this.WindowState == WindowState.Maximized && this.WindowStyle == WindowStyle.None)
            {
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
            }
            UpdateMaximizeRestoreButtonContent();
        }

        private void OpenDevTools(object? parameter)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.OpenDevToolsWindow();
            }
        }

        private void CloseCurrentTab(object? parameter)
        {
            if (SelectedTabItem != null)
            {
                CloseBrowserTab(SelectedTabItem.Tab!);
            }
        }

        private void FocusUrlTextBox(object? parameter)
        {
            AddressBar.Focus();
            AddressBar.SelectAll();
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SpeechSynthesizer_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            _isReadingAloud = false;
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

        private void LoadMicrophoneControlScript()
        {
            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MicrophoneControl.js");
                if (File.Exists(scriptPath))
                {
                    _microphoneControlScript = File.ReadAllText(scriptPath);
                }
                else
                {
                    MessageBox.Show("Advertencia: El archivo 'MicrophoneControl.js' no se encontró. El control de micrófono de la página no funcionará.", "Archivo Faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el script de control de micrófono: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void InitializeEnvironments()
        {
            string? webView2Version = null;
            try
            {
                webView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Exception)
            {
                webView2Version = null;
            }

            if (string.IsNullOrEmpty(webView2Version))
            {
                MessageBoxResult result = MessageBox.Show(
                    "El componente WebView2 Runtime de Microsoft Edge no está instalado en tu sistema.\n" +
                    "Este navegador lo requiere para funcionar.\n\n" +
                    "¿Deseas descargarlo e instalarlo ahora?",
                    "WebView2 Runtime No Encontrado",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo("https://developer.microsoft.com/en-us/microsoft-edge/webview2/?form=MA13LH") { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No se pudo abrir el enlace de descarga: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                Application.Current.Shutdown();
                return;
            }

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
                MessageBox.Show($"Error al inicializar los entornos del navegador: {ex.Message}\nPor favor, asegúrate de que tu instalación de WebView2 Runtime no esté corrupta o intenta reinstalarlo.", "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
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
                string? savedUrlsJson = ConfigurationManager.AppSettings[LastSessionUrlsSettingKey];
                if (!string.IsNullOrEmpty(savedUrlsJson))
                {
                    try
                    {
                        List<string>? savedUrls = JsonSerializer.Deserialize<List<string>>(savedUrlsJson);
                        if (savedUrls != null && savedUrls.Any())
                        {
                            CrashRecoveryWindow recoveryWindow = new CrashRecoveryWindow();
                            recoveryWindow.Owner = this;
                            recoveryWindow.ShowDialog();

                            if (recoveryWindow.ShouldRestoreSession)
                            {
                                foreach (var group in _tabGroupManager.TabGroups.ToList())
                                {
                                    foreach (var tabItem in group.TabsInGroup.ToList())
                                    {
                                        CloseBrowserTab(tabItem.Tab!);
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


        private async void AddNewTab(string? url = null, bool isIncognito = false, TabGroup? targetGroup = null)
        {
            if (_defaultEnvironment == null || _incognitoEnvironment == null)
            {
                MessageBox.Show("El navegador no está listo para crear nuevas pestañas. Por favor, reinicia la aplicación.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
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
            browserTab.GeminiFeatureIcon = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center };
            browserTab.GeminiFeatureIcon.Source = browserTab.GeminiIconSource;
            browserTab.GeminiFeatureIcon.Visibility = Visibility.Collapsed;
            
            browserTab.HeaderTextBlock = new TextBlock { Text = "Cargando...", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };

            browserTab.FaviconImage.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("FaviconSource") { Source = browserTab });
            browserTab.AudioIconImage.SetBinding(Image.VisibilityProperty, new System.Windows.Data.Binding("IsAudioPlaying") { Source = browserTab, Converter = (System.Windows.Data.IValueConverter)this.FindResource("BooleanToVisibilityConverter") });
            browserTab.AudioIconImage.MouseLeftButtonUp += AudioIcon_MouseLeftButtonUp;
            browserTab.ExtensionIconImage.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("ExtensionActiveIcon") { Source = browserTab });
            browserTab.ExtensionIconImage.SetBinding(Image.VisibilityProperty, new System.Windows.Data.Binding("IsExtensionActive") { Source = browserTab, Converter = (System.Windows.Data.IValueConverter)this.FindResource("BooleanToVisibilityConverter") });
            browserTab.BlockedIconImage.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("SiteBlockedIcon") { Source = browserTab });
            browserTab.BlockedIconImage.SetBinding(Image.VisibilityProperty, new System.Windows.Data.Binding("IsSiteBlocked") { Source = browserTab, Converter = (System.Windows.Data.IValueConverter)this.FindResource("BooleanToVisibilityConverter") });
            browserTab.GeminiFeatureIcon.SetBinding(Image.VisibilityProperty, new System.Windows.Data.Binding("IsSelectedForGemini") { Source = browserTab, Converter = (System.Windows.Data.IValueConverter)this.FindResource("BooleanToVisibilityConverter") });


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
            DockPanel.SetDock(browserTab.GeminiFeatureIcon, Dock.Left);
            DockPanel.SetDock(browserTab.ExtensionIconImage, Dock.Left);
            DockPanel.SetDock(browserTab.BlockedIconImage, Dock.Left);
            DockPanel.SetDock(browserTab.HeaderTextBlock, Dock.Left);
            DockPanel.SetDock(closeButton, Dock.Right);

            tabHeaderPanel.Children.Add(browserTab.FaviconImage);
            tabHeaderPanel.Children.Add(browserTab.AudioIconImage);
            tabHeaderPanel.Children.Add(browserTab.GeminiFeatureIcon);
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

        private void WebView_Loaded(object? sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = sender as WebView2;
            if (currentWebView != null)
            {
                currentWebView.EnsureCoreWebView2Async(null);
            }
        }

        private void ConfigureCoreWebView2(WebView2? currentWebView, CoreWebView2InitializationCompletedEventArgs e, CoreWebView2Environment? environment)
        {
            if (currentWebView != null && e.IsSuccess)
            {
                currentWebView.CoreWebView2.Environment.SetCustomFileExtensions(new[] { ".pdf", ".docx", ".xlsx" });

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
                currentWebView.CoreWebView2.IsAudioPlayingChanged += CoreWebView2_IsAudioPlayingChanged;
                currentWebView.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed;
                currentWebView.CoreWebView2.WebResourceResponseReceived -= CoreWebView2_WebResourceResponseReceived;


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


        private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (AdBlocker.IsEnabled && AdBlocker.IsBlocked(e.Request.Uri))
            {
                e.Response = ((WebView2)sender!).CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Forbidden", "Content-Type: text/plain\nAccess-Control-Allow-Origin: *"
                );
                var browserTab = GetBrowserTabItemFromWebView(sender as WebView2);
                if (browserTab != null) browserTab.IsSiteBlocked = true;
                return;
            }

            if (TrackerBlocker.IsEnabled && TrackerBlocker.IsBlocked(e.Request.Uri))
            {
                e.Response = ((WebView2)sender!).CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Forbidden", "Content-Type: text/plain\nAccess-Control-Allow-Origin: *"
                );
                var browserTab = GetBrowserTabItemFromWebView(sender as WebView2);
                if (browserTab != null) browserTab.IsSiteBlocked = true;
                return;
            }
        }

        private void CoreWebView2_WebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            // No se necesita lógica adicional aquí para IsSiteBlocked, ya se maneja en WebResourceRequested.
        }


        private async void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            if (_isPdfViewerEnabled && e.DownloadOperation.Uri.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                e.Handled = true;
                PdfViewerWindow pdfViewer = new PdfViewerWindow(e.DownloadOperation.Uri, _defaultEnvironment!);
                pdfViewer.Owner = this;
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
            saveFileDialog.Owner = this;

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

        private void CoreWebView2_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                this,
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

        private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            WebView2? currentWebView = sender as WebView2;
            var browserTab = GetBrowserTabItemFromWebView(currentWebView);

            if (browserTab != null && SelectedTabItem == browserTab)
            {
                AddressBar.Text = currentWebView!.CoreWebView2.Source;
            }
            if (browserTab != null) browserTab.IsSiteBlocked = false;
        }


        private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            WebView2? currentWebView = sender as WebView2;
            var browserTab = GetBrowserTabItemFromWebView(currentWebView);

            if (browserTab != null && SelectedTabItem == browserTab)
            {
                if (!e.IsSuccess)
                {
                    if (e.WebErrorStatus == CoreWebView2WebErrorStatus.Disconnected ||
                        e.WebErrorStatus == CoreWebView2WebErrorStatus.InternetDisconnected ||
                        e.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionAborted ||
                        e.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionReset ||
                        e.WebErrorStatus == CoreWebView2WebErrorStatus.HostNameNotResolved)
                    {
                        _lastFailedUrl = currentWebView!.CoreWebView2.Source;
                        _isOfflineGameActive = true;
                        string offlineGamePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OfflineGame.html");
                        if (File.Exists(offlineGamePath))
                        {
                            currentWebView.CoreWebView2.Navigate($"file:///{offlineGamePath.Replace("\\", "/")}");
                            _connectivityTimer!.Enabled = true;
                        }
                        else
                        {
                            MessageBox.Show(this, $"La navegación a {currentWebView.CoreWebView2.Source} falló debido a la falta de conexión a Internet y el juego offline no se encontró.", "Error de Navegación", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationAborted)
                    {
                        string errorPagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomErrorPage.html");
                        if (File.Exists(errorPagePath))
                        {
                            currentWebView!.CoreWebView2.Navigate($"file:///{errorPagePath.Replace("\\", "/")}");
                        }
                        else
                        {
                            MessageBox.Show(this, $"La navegación a {currentWebView!.CoreWebView2.Source} falló con el código de error {e.WebErrorStatus}", "Error de Navegación", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    _connectivityTimer!.Enabled = false;
                    _lastFailedUrl = null;
                    _isOfflineGameActive = false;

                    if (!browserTab.IsIncognito && browserTab.LeftWebView == currentWebView)
                    {
                        HistoryManager.AddHistoryEntry(currentWebView!.CoreWebView2.Source, currentWebView.CoreWebView2.DocumentTitle);
                    }

                    await InjectEnabledExtensions(currentWebView!, browserTab);

                    if (!string.IsNullOrEmpty(_pageColorExtractionScript))
                    {
                        try
                        {
                            string resultJson = await currentWebView!.CoreWebView2.ExecuteScriptAsync(_pageColorExtractionScript);
                            if (resultJson != null && resultJson != "null")
                            {
                                var colorData = JsonSerializer.Deserialize<Dictionary<string, string>>(resultJson);
                                if (colorData != null && colorData.ContainsKey("dominantColor"))
                                {
                                    string dominantColorHex = colorData["dominantColor"];
                                    try
                                    {
                                        Color pageColor = (Color)ColorConverter.ConvertFromString(dominantColorHex);
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
                        await webView.CoreWebView2!.ExecuteScriptAsync(scriptContent);
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


        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!_isGeminiModeActive)
            {
            }

            if (_isOfflineGameActive && !e.Uri.StartsWith($"file:///{AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/")}/OfflineGame.html", StringComparison.OrdinalIgnoreCase))
            {
                _isOfflineGameActive = false;
                _connectivityTimer!.Enabled = false;
                _lastFailedUrl = null;
            }

            if (_isPdfViewerEnabled && e.Uri.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                PdfViewerWindow pdfViewer = new PdfViewerWindow(e.Uri, _defaultEnvironment!);
                pdfViewer.Owner = this;
                pdfViewer.Show();
                return;
            }
        }

        private void WebView_DocumentTitleChanged(object? sender, object e)
        {
            WebView2? currentWebView = sender as WebView2;
            if (currentWebView != null)
            {
                var browserTab = GetBrowserTabItemFromWebView(currentWebView);
                if (browserTab != null)
                {
                    if (browserTab.LeftWebView == currentWebView)
                    {
                        string title = currentWebView.CoreWebView2!.DocumentTitle;
                        if (browserTab.IsIncognito)
                        {
                            browserTab.HeaderTextBlock!.Text = "(Incógnito) " + title;
                        }
                        else
                        {
                            browserTab.HeaderTextBlock!.Text = title;
                        }
                    }
                }

                if (SelectedTabItem == browserTab && browserTab.LeftWebView == currentWebView)
                {
                    WindowTitleText!.Text = currentWebView!.CoreWebView2.DocumentTitle + " - Aurora Browser";
                }
            }
        }

        private async void CoreWebView2_FaviconChanged(object? sender, object e)
        {
            WebView2? currentWebView = sender as WebView2;
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

        private void CoreWebView2_IsAudioPlayingChanged(object? sender, object e)
        {
            WebView2? currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            var browserTab = GetBrowserTabItemFromWebView(currentWebView);
            if (browserTab == null) return;

            browserTab.IsAudioPlaying = currentWebView.CoreWebView2.IsAudioPlaying;
        }

        private void AudioIcon_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            Image? audioIcon = sender as Image;
            if (audioIcon == null) return;

            BrowserTabItem? browserTab = audioIcon.DataContext as BrowserTabItem;
            if (browserTab == null || browserTab.LeftWebView == null || browserTab.LeftWebView.CoreWebView2 == null) return;

            browserTab.LeftWebView.CoreWebView2.IsMuted = !browserTab.LeftWebView.CoreWebView2.IsMuted;
            audioIcon.ToolTip = browserTab.LeftWebView.CoreWebView2.IsMuted ? "Audio silenciado (clic para reactivar)" : "Reproduciendo audio (clic para silenciar/reactivar)";
        }


        private void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
        {
            WebView2? failedWebView = sender as WebView2;
            if (failedWebView == null) return;

            var browserTab = GetBrowserTabItemFromWebView(failedWebView);
            if (browserTab == null) return;

            string message = $"El proceso de la página '{failedWebView.Source}' ha fallado.\n" +
                             $"Tipo de fallo: {e.ProcessFailedKind}\n" +
                             $"Estado del error: {e.Reason}";

            MessageBoxResult result = MessageBox.Show(this, message + "\n\n¿Deseas recargar la página?",
                                                      "Página No Responde",
                                                      MessageBoxButton.YesNo,
                                                      MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                failedWebView.CoreWebView2!.Reload();
            }
            else
            {
                string errorPagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomErrorPage.html");
                if (File.Exists(errorPagePath))
                {
                    failedWebView.CoreWebView2!.Navigate($"file:///{errorPagePath.Replace("\\", "/")}");
                }
            }
        }


        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrlInCurrentTab();
        }

        private void AddressBar_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToUrlInCurrentTab();
            }
        }

        private void NavigateToUrlInCurrentTab()
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show(this, "No hay una pestaña activa o el navegador no está listo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string input = AddressBar.Text.Trim();
            string urlToNavigate = input;

            if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uriResult) ||
                (uriResult!.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
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
                MessageBox.Show(this, $"Error al navegar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null && currentWebView.CoreWebView2.CanGoBack)
            {
                currentWebView.CoreWebView2.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null && currentWebView.CoreWebView2.CanGoForward)
            {
                currentWebView.CoreWebView2.GoForward();
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.Reload();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.Navigate(_defaultHomePage);
            }
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        private void MinesweeperButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show(this, "No hay una pestaña activa para abrir el Buscaminas.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            currentWebView.CoreWebView2.Navigate("https://pacureok.github.io/Buscaminasbasico/");
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow historyWindow = new HistoryWindow();
            historyWindow.Owner = this;
            if (historyWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(historyWindow.SelectedUrl))
                {
                    AddressBar.Text = historyWindow.SelectedUrl;
                    NavigateToUrlInCurrentTab();
                }
            }
        }

        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            BookmarksWindow bookmarksWindow = new BookmarksWindow();
            bookmarksWindow.Owner = this;
            if (bookmarksWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(bookmarksWindow.SelectedUrl))
                {
                    AddressBar.Text = bookmarksWindow.SelectedUrl;
                    NavigateToUrlInCurrentTab();
                }
            }
        }

        private void AddBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                var browserTab = SelectedTabItem;
                if (browserTab != null && browserTab.IsIncognito)
                {
                    MessageBox.Show(this, "No se pueden añadir marcadores en modo incógnito.", "Error al Añadir Marcador", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show(this, "No se pudo añadir la página a marcadores. Asegúrate de que la página esté cargada y tenga un título.", "Error al Añadir Marcador", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show(this, "No hay una página activa para añadir a marcadores.", "Error al Añadir Marcador", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DownloadsButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadsWindow downloadsWindow = new DownloadsWindow();
            downloadsWindow.Owner = this;
            downloadsWindow.Show();
        }

        private async void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show(this, "No hay una página activa para aplicar el modo lectura.", "Error de Modo Lectura", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show(this, $"Error al aplicar el modo lectura: {ex.Message}", "Error de Modo Lectura", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(this, "Advertencia: El archivo 'ReaderMode.js' no se encontró. El modo lectura no funcionará.", "Archivo Faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _isReadingAloud = false;
                return;
            }

            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show(this, "No hay una página activa para leer en voz alta.", "Leer en Voz Alta", MessageBoxButton.OK, MessageBoxImage.Error);
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

                pageText = JsonSerializer.Deserialize<string>(pageText)!;

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    _speechSynthesizer.SpeakAsync(pageText);
                    _isReadingAloud = true;
                }
                else
                {
                    MessageBox.Show(this, "No se encontró texto legible en la página actual.", "Leer en Voz Alta", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error al leer en voz alta: {ex.Message}", "Error de Lectura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SplitScreenButton_Click(object sender, RoutedEventArgs e)
        {
            var currentTab = SelectedTabItem;
            if (currentTab == null || currentTab.LeftWebView == null || currentTab.LeftWebView.CoreWebView2 == null)
            {
                MessageBox.Show(this, "No hay una pestaña activa o el navegador no está listo.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (currentTab.IsSplit)
            {
                DisableSplitScreenForCurrentTab(currentTab);
            }
            else
            {
                await EnableSplitScreenForCurrentTab(currentTab, _defaultHomePage);
            }
        }

        private async void AIButton_Click(object sender, RoutedEventArgs e)
        {
            SetGeminiMode(true);
            
            AskGeminiWindow geminiWindow = new AskGeminiWindow(new ObservableCollection<BrowserTabItem>());
            geminiWindow.Owner = this;

            if (geminiWindow.ShowDialog() == true)
            {
                if (SelectedTabItem != null && SelectedTabItem.LeftWebView != null)
                {
                    if (!SelectedTabItem.IsSplit || SelectedTabItem.RightWebView == null)
                    {
                        await EnableSplitScreenForCurrentTab(SelectedTabItem, "https://gemini.google.com/");
                    }
                    else
                    {
                        SelectedTabItem.RightWebView.CoreWebView2!.Navigate("https://gemini.google.com/");
                    }

                    await Task.Delay(2000);

                    var dataToInject = new
                    {
                        userQuestion = geminiWindow.UserQuestion,
                        capturedPages = geminiWindow.CapturedData.Select(cp => new
                        {
                            url = cp.Url,
                            title = cp.Title,
                            screenshotBase64 = cp.ScreenshotBase64,
                            pageText = cp.PageText,
                            faviconBase64 = cp.FaviconBase64
                        }).ToList()
                    };

                    string jsonString = JsonSerializer.Serialize(dataToInject);

                    string injectionScript = $@"
                        (function() {{
                            const data = {jsonString};

                            let container = document.getElementById('auroraGeminiIntegrationContainer');
                            if (!container) {{
                                container = document.createElement('div');
                                container.id = 'auroraGeminiIntegrationContainer';
                                container.style.cssText = `
                                    position: absolute;
                                    top: 10px;
                                    left: 10px;
                                    right: 10px;
                                    background-color: rgba(255, 255, 255, 0.95);
                                    border: 2px solid #9B59B6;
                                    border-radius: 10px;
                                    padding: 15px;
                                    z-index: 99999;
                                    max-height: 90%;
                                    overflow-y: auto;
                                    box-shadow: 0 4px 12px rgba(0,0,0,0.2);
                                    font-family: 'Segoe UI', sans-serif;
                                    color: #333;
                                `;
                                document.body.prepend(container);
                            }} else {{
                                container.innerHTML = '';
                            }}

                            const closeButton = document.createElement('button');
                            closeButton.textContent = 'X';
                            closeButton.style.cssText = `
                                position: absolute;
                                top: 5px;
                                right: 5px;
                                background: #dc3545;
                                color: white;
                                border: none;
                                border-radius: 50%;
                                width: 25px;
                                height: 25px;
                                cursor: pointer;
                                font-size: 14px;
                                line-height: 1;
                                text-align: center;
                            `;
                            closeButton.onclick = () => container.remove();
                            container.appendChild(closeButton);

                            const header = document.createElement('h3');
                            header.textContent = 'Datos de Aurora Browser para Gemini:';
                            header.style.cssText = 'color: #9B59B6; margin-top: 5px; margin-bottom: 10px;';
                            container.appendChild(header);

                            const questionPara = document.createElement('p');
                            questionPara.innerHTML = `<strong>Tu Pregunta:</strong> ${data.userQuestion}`;
                            questionPara.style.cssText = 'background-color: #e6f7ff; padding: 10px; border-radius: 5px; margin-bottom: 15px; font-style: italic;';
                            container.appendChild(questionPara);

                            data.capturedPages.forEach(page => {{
                                const pageDiv = document.createElement('div');
                                pageDiv.style.cssText = `
                                    border: 1px solid #eee;
                                    border-radius: 8px;
                                    padding: 10px;
                                    margin-bottom: 10px;
                                    background-color: #fcfcfc;
                                    box-shadow: 0 1px 3px rgba(0,0,0,0.05);
                                `;

                                const pageTitle = document.createElement('h4');
                                pageTitle.style.cssText = 'margin-top: 0; margin-bottom: 5px; color: #007bff; display: flex; align-items: center;';
                                if (page.faviconBase64) {{
                                    const faviconImg = document.createElement('img');
                                    faviconImg.src = `data:image/x-icon;base64,${page.faviconBase64}`;
                                    faviconImg.style.cssText = 'width: 16px; height: 16px; margin-right: 8px;';
                                    pageTitle.appendChild(faviconImg);
                                }}
                                pageTitle.innerHTML += page.title || page.url;
                                pageDiv.appendChild(pageTitle);

                                const urlPara = document.createElement('p');
                                urlPara.innerHTML = `<strong>URL:</strong> <a href='${page.url}' target='_blank' style='color: #0056b3;'>${page.url}</a>`;
                                urlPara.style.cssText = 'font-size: 0.9em; margin-bottom: 5px; word-break: break-all;';
                                pageDiv.appendChild(urlPara);

                                if (page.screenshotBase64) {{
                                    const screenshotImg = document.createElement('img');
                                    screenshotImg.src = `data:image/png;base64,${page.screenshotBase64}`;
                                    screenshotImg.style.cssText = `
                                        max-width: 100%;
                                        height: auto;
                                        display: block;
                                        margin: 10px 0;
                                        border: 1px solid #ddd;
                                        border-radius: 5px;
                                    `;
                                    pageDiv.appendChild(screenshotImg);
                                }}

                                if (page.pageText) {{
                                    const textHeader = document.createElement('p');
                                    textHeader.innerHTML = '<strong>Texto Extraído (primeros 500 caracteres):</strong>';
                                    textHeader.style.cssText = 'margin-bottom: 5px; font-size: 0.9em;';
                                    pageDiv.appendChild(textHeader);

                                    const textContent = document.createElement('div');
                                    textContent.textContent = page.pageText.substring(0, 500) + (page.pageText.length > 500 ? '...' : '');
                                    textContent.style.cssText = `
                                        background-color: #f9f9f9;
                                        border: 1px dashed #ccc;
                                        padding: 8px;
                                        max-height: 100px;
                                        overflow-y: auto;
                                        font-size: 0.8em;
                                        white-space: pre-wrap;
                                    `;
                                    pageDiv.appendChild(textContent);
                                }}
                                container.appendChild(pageDiv);
                            }});

                            const instructionsPara = document.createElement('p');
                            instructionsPara.innerHTML = `<br><strong>Instrucciones:</strong> Para que Gemini procese esta información, por favor, copia y pega el texto y las URLs relevantes en el cuadro de chat de Gemini, y sube las imágenes si es necesario.`;
                            instructionsPara.style.cssText = 'margin-top: 20px; font-size: 0.95em; color: #666; border-top: 1px dashed #eee; padding-top: 10px;';
                            container.appendChild(instructionsPara);

                        }})();
                    ";
                    
                    SelectedTabItem.RightWebView!.CoreWebView2!.ExecuteScriptAsync(injectionScript);

                    MessageBox.Show(this, "Las capturas de pantalla, URLs y tu pregunta se han mostrado en el panel de Gemini. Por favor, copia y pega la información relevante en el chat de Gemini.", "Información para Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(this, "Para usar esta función, por favor, activa el modo de pantalla dividida en la pestaña actual y asegúrate de que el panel derecho esté visible.", "Modo de Pantalla Dividida Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            
            SetGeminiMode(false);
        }

        private void SetGeminiMode(bool isActive)
        {
            _isGeminiModeActive = isActive;
            if (isActive)
            {
                Application.Current.Resources["OriginalBrowserBackgroundColor"] = BrowserBackgroundColor;
                Application.Current.Resources["OriginalBrowserForegroundColor"] = BrowserForegroundColor;

                BrowserBackgroundColor = (Color)Application.Current.Resources["GeminiBackgroundColor"];
                BrowserForegroundColor = (Color)Application.Current.Resources["GeminiForegroundColor"];
            }
            else
            {
                if (Application.Current.Resources.Contains("OriginalBrowserBackgroundColor") &&
                    Application.Current.Resources.Contains("OriginalBrowserForegroundColor"))
                {
                    BrowserBackgroundColor = (Color)Application.Current.Resources["OriginalBrowserBackgroundColor"];
                    BrowserForegroundColor = (Color)Application.Current.Resources["OriginalBrowserForegroundColor"];
                    Application.Current.Resources.Remove("OriginalBrowserBackgroundColor");
                    Application.Current.Resources.Remove("OriginalBrowserForegroundColor");
                }
                else
                {
                    LoadSettings();
                    ApplyForegroundToWindowControls();
                }
            }
            ApplyToolbarPosition(_currentToolbarPosition);
            UpdateToolbarButtonForeground();
        }


        private async void CoreWebView2_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            WebView2? currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            var browserTab = GetBrowserTabItemFromWebView(currentWebView);
            if (browserTab != null && browserTab.IsIncognito) return;

            string currentUrl = currentWebView.CoreWebView2.Source;
            string? username = null;
            string? password = null;

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

        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            WebView2? currentWebView = sender as WebView2;
            if (currentWebView == null || currentWebView.CoreWebView2 == null) return;

            string message = e.WebMessageAsJson;
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(message))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("type", out JsonElement typeElement) && typeElement.GetString() == "loginSubmit")
                    {
                        string? url = root.GetProperty("url").GetString();
                        string? username = root.GetProperty("username").GetString();
                        string? password = root.GetProperty("password").GetString();

                        if (url != null && username != null && password != null)
                        {
                            MessageBoxResult result = MessageBox.Show(this,
                                $"¿Deseas guardar la contraseña para el usuario '{username}' en '{new Uri(url).Host}'?",
                                "Guardar Contraseña",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question
                            );

                            if (result == MessageBoxResult.Yes)
                            {
                                PasswordManager.AddOrUpdatePassword(url, username, password);
                                MessageBox.Show(this, "Contraseña guardada con éxito.", "Contraseña Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    else if (root.TryGetProperty("type", out typeElement) && typeElement.GetString() == "retryConnection")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (!string.IsNullOrEmpty(_lastFailedUrl))
                            {
                                AddressBar.Text = _lastFailedUrl;
                                NavigateToUrlInCurrentTab();
                            }
                            else
                            {
                                AddNewTab(_defaultHomePage);
                            }
                        });
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
            return _defaultEnvironment!;
        }


        public List<BrowserTabItem> GetBrowserTabItems()
        {
            return _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).ToList();
        }

        public void CloseBrowserTab(TabItem tabToClose)
        {
            Button? closeButton = null;
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

                    if (!browserTabItem.ParentGroup!.TabsInGroup.Any() && _tabGroupManager.TabGroups.Count > 1)
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
                        SelectedTabItem.Tab!.IsSelected = true;
                    }
                }
            }
        }

        private TabItem? GetCurrentBrowserTabItemInternal()
        {
            return SelectedTabItem?.Tab;
        }


        private async Task EnableSplitScreenForCurrentTab(BrowserTabItem tabItem, string rightPanelUrl)
        {
            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _isReadingAloud = false;
            }

            WebView2 webView2 = new WebView2();
            webView2.Source = new Uri(rightPanelUrl);
            webView2.Name = "WebView2_Tab" + tabItem.ParentGroup!.TabsInGroup.IndexOf(tabItem);
            webView2.HorizontalAlignment = HorizontalAlignment.Stretch;
            webView2.VerticalAlignment = VerticalAlignment.Stretch;

            CoreWebView2Environment envToUse = tabItem.IsIncognito ? _incognitoEnvironment! : _defaultEnvironment!;
            webView2.CoreWebView2InitializationCompleted += (s, ev) => ConfigureCoreWebView2(webView2, ev, envToUse);

            await webView2.EnsureCoreWebView2Async(null);

            Grid splitGrid = new Grid();
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(tabItem.LeftWebView!, 0);
            splitGrid.Children.Add(tabItem.LeftWebView!);

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

            tabItem.Tab!.Content = splitGrid;
            tabItem.RightWebView = webView2;
            tabItem.IsSplit = true;
        }

        private void DisableSplitScreenForCurrentTab(BrowserTabItem tabItem)
        {
            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _isReadingAloud = false;
            }

            Grid? currentGrid = tabItem.Tab?.Content as Grid;
            if (currentGrid != null)
            {
                currentGrid.Children.Remove(tabItem.LeftWebView!);
            }

            if (tabItem.RightWebView != null)
            {
                tabItem.RightWebView.Dispose();
                tabItem.RightWebView = null;
            }

            Grid singleViewGrid = new Grid();
            singleViewGrid.Children.Add(tabItem.LeftWebView!);
            tabItem.Tab!.Content = singleViewGrid;
            tabItem.IsSplit = false;
        }

        private void IncognitoButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(_defaultHomePage, isIncognito: true);
        }

        public void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            Button? closeButton = sender as Button;
            TabItem? tabToClose = closeButton?.Tag as TabItem;

            if (tabToClose != null)
            {
                var browserTabItem = GetBrowserTabItemFromTabItem(tabToClose);

                if (browserTabItem != null)
                {
                    browserTabItem.ParentGroup?.TabsInGroup.Remove(browserTabItem);
                    browserTabItem.LeftWebView?.Dispose();
                    browserTabItem.RightWebView?.Dispose();

                    if (!browserTabItem.ParentGroup!.TabsInGroup.Any() && _tabGroupManager.TabGroups.Count > 1)
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
                        SelectedTabItem.Tab!.IsSelected = true;
                    }
                }
            }
        }

        private void AddressBar_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // No se necesita código aquí si el ContextMenu está definido directamente en XAML.
        }

        private void OpenInNewTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(AddressBar.Text);
        }

        private void OpenInNewIncognitoTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(AddressBar.Text, isIncognito: true);
        }


        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            TabControl? currentTabControl = sender as TabControl;
            if (currentTabControl != null && currentTabControl.SelectedItem is BrowserTabItem selectedBrowserTab)
            {
                SelectedTabItem = selectedBrowserTab;
                UpdateUrlTextBoxFromCurrentTab();

                if (_isReadingAloud)
                {
                    _speechSynthesizer.SpeakAsyncCancelAll();
                    _isReadingAloud = false;
                }

                if (selectedBrowserTab.LeftWebView == null)
                {
                    if (_isTabSuspensionEnabled)
                    {
                        string? urlToReload = selectedBrowserTab.Tab?.Tag?.ToString();

                        WebView2 newWebView = new WebView2();
                        newWebView.Source = new Uri(urlToReload ?? _defaultHomePage);
                        newWebView.Name = "WebView1_Tab" + (selectedBrowserTab.ParentGroup!.TabsInGroup.IndexOf(selectedBrowserTab) + 1);
                        newWebView.HorizontalAlignment = HorizontalAlignment.Stretch;
                        newWebView.VerticalAlignment = VerticalAlignment.Stretch;

                        newWebView.Loaded += WebView_Loaded;
                        CoreWebView2Environment envToUse = selectedBrowserTab.IsIncognito ? _incognitoEnvironment! : _defaultEnvironment!;
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
                        selectedBrowserTab.Tab!.Content = tabContent;

                        selectedBrowserTab.LeftWebView = newWebView;
                        selectedBrowserTab.RightWebView = null;
                        selectedBrowserTab.IsSplit = false;

                        string originalHeaderText = selectedBrowserTab.HeaderTextBlock!.Text;
                        if (!originalHeaderText.StartsWith("(Suspendida) "))
                        {
                            selectedBrowserTab.HeaderTextBlock.Text = "(Suspendida) " + originalHeaderText;
                        }
                    }
                    else
                    {
                        string? urlToReload = selectedBrowserTab.Tab?.Tag?.ToString();
                        selectedBrowserTab.ParentGroup!.TabsInGroup.Remove(selectedBrowserTab);
                        AddNewTab(urlToReload, selectedBrowserTab.IsIncognito, selectedBrowserTab.ParentGroup);
                    }
                }
            }
            _isFindBarVisible = false;
            ClearFindResults();
        }

        private void UpdateUrlTextBoxFromCurrentTab()
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null)
            {
                AddressBar.Text = currentWebView.CoreWebView2.Source;
                this.Title = currentWebView.CoreWebView2.DocumentTitle + " - Aurora Browser";
                WindowTitleText!.Text = this.Title;
            }
            else
            {
                AddressBar.Text = string.Empty;
                this.Title = "Aurora Browser";
                WindowTitleText!.Text = this.Title;
            }
        }

        public WebView2? GetCurrentWebView()
        {
            return SelectedTabItem?.LeftWebView;
        }

        private BrowserTabItem? GetBrowserTabItemFromTabItem(TabItem tabItem)
        {
            return _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(bti => bti.Tab == tabItem);
        }

        private BrowserTabItem? GetBrowserTabItemFromWebView(WebView2? webView)
        {
            return _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).FirstOrDefault(bti => bti.LeftWebView == webView || bti.RightWebView == webView);
        }


        private void CheckAndSuggestTabSuspension()
        {
            const int MaxTabsBeforeSuggestion = 15;
            int activeTabs = _tabGroupManager.TabGroups.SelectMany(g => g.TabsInGroup).Count(t => t.LeftWebView != null && !t.IsIncognito && !t.IsSplit);

            if (_isTabSuspensionEnabled && activeTabs > MaxTabsBeforeSuggestion)
            {
                MessageBoxResult result = MessageBox.Show(this,
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
            SettingsWindow settingsWindow = new SettingsWindow(
                _defaultHomePage, AdBlocker.IsEnabled, _defaultSearchEngineUrl,
                _isTabSuspensionEnabled, _restoreSessionOnStartup, TrackerBlocker.IsEnabled,
                _isPdfViewerEnabled, BrowserBackgroundColor, BrowserForegroundColor, _currentToolbarPosition);

            settingsWindow.Owner = this;
            settingsWindow.OnClearBrowsingData += SettingsWindow_OnClearBrowsingData;
            settingsWindow.OnSuspendInactiveTabs += SettingsWindow_OnSuspendInactiveTabs;
            settingsWindow.OnColorsChanged += SettingsWindow_OnColorsChanged;
            settingsWindow.OnToolbarPositionChanged += SettingsWindow_OnToolbarPositionChanged;

            if (settingsWindow.ShowDialog() == true)
            {
                _defaultHomePage = settingsWindow.HomePage;
                AdBlocker.IsEnabled = settingsWindow.IsAdBlockerEnabled;
                _defaultSearchEngineUrl = settingsWindow.DefaultSearchEngineUrl;
                _isTabSuspensionEnabled = settingsWindow.IsTabSuspensionEnabled;
                _restoreSessionOnStartup = settingsWindow.RestoreSessionOnStartup;
                TrackerBlocker.IsEnabled = settingsWindow.IsTrackerProtectionEnabled;
                _isPdfViewerEnabled = settingsWindow.IsPdfViewerEnabled;
                SaveSettings();
                MessageBox.Show(this, "Configuración guardada. Los cambios se aplicarán al abrir nuevas pestañas o al hacer clic en 'Inicio'.", "Configuración Guardada", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            settingsWindow.OnClearBrowsingData -= SettingsWindow_OnClearBrowsingData;
            settingsWindow.OnSuspendInactiveTabs -= SettingsWindow_OnSuspendInactiveTabs;
            settingsWindow.OnColorsChanged -= SettingsWindow_OnColorsChanged;
            settingsWindow.OnToolbarPositionChanged -= SettingsWindow_OnToolbarPositionChanged;
        }

        private void SettingsWindow_OnColorsChanged(Color backgroundColor, Color foregroundColor)
        {
            BrowserBackgroundColor = backgroundColor;
            BrowserForegroundColor = foregroundColor;
            SaveSettings();
        }

        private void SettingsWindow_OnToolbarPositionChanged(ToolbarPosition newPosition)
        {
            _currentToolbarPosition = newPosition;
            ApplyToolbarPosition(newPosition);
            SaveSettings();
        }


        private async void SettingsWindow_OnClearBrowsingData()
        {
            WebView2? anyWebView = GetCurrentWebView();

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
                MessageBox.Show(this, "Datos de navegación (caché, cookies, etc.) borrados con éxito.", "Limpieza Completa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(this, "No se pudo acceder al motor del navegador para borrar los datos del perfil normal.", "Error de Limpieza", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SettingsWindow_OnSuspendInactiveTabs()
        {
            if (!_isTabSuspensionEnabled)
            {
                MessageBox.Show(this, "La suspensión de pestañas no está habilitada en la configuración. Habilítela para usar esta función.", "Suspensión Deshabilitada", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        browserTab.Tab!.Content = suspendedMessage;
                        browserTab.Tab.Tag = suspendedUrl;

                        string originalHeaderText = browserTab.HeaderTextBlock!.Text;
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
            string? savedHomePage = ConfigurationManager.AppSettings[HomePageSettingKey];
            if (!string.IsNullOrEmpty(savedHomePage))
            {
                _defaultHomePage = savedHomePage;
            }

            string? savedAdBlockerState = ConfigurationManager.AppSettings[AdBlockerSettingKey];
            if (bool.TryParse(savedAdBlockerState, out bool isEnabled))
            {
                AdBlocker.IsEnabled = isEnabled;
            }
            else
            {
                AdBlocker.IsEnabled = false;
            }

            string? savedSearchEngineUrl = ConfigurationManager.AppSettings[DefaultSearchEngineSettingKey];
            if (!string.IsNullOrEmpty(savedSearchEngineUrl))
            {
                _defaultSearchEngineUrl = savedSearchEngineUrl;
            }

            string? savedTabSuspensionState = ConfigurationManager.AppSettings[TabSuspensionSettingKey];
            if (bool.TryParse(savedTabSuspensionState, out bool isTabSuspensionEnabled))
            {
                _isTabSuspensionEnabled = isTabSuspensionEnabled;
            }
            else
            {
                _isTabSuspensionEnabled = false;
            }

            string? savedRestoreSessionState = ConfigurationManager.AppSettings[RestoreSessionSettingKey];
            if (bool.TryParse(savedRestoreSessionState, out bool restoreSession))
            {
                _restoreSessionOnStartup = restoreSession;
            }
            else
            {
                _restoreSessionOnStartup = true;
            }

            string? savedTrackerProtectionState = ConfigurationManager.AppSettings[TrackerProtectionSettingKey];
            if (bool.TryParse(savedTrackerProtectionState, out bool isTrackerProtectionEnabled))
            {
                TrackerBlocker.IsEnabled = isTrackerProtectionEnabled;
            }
            else
            {
                TrackerBlocker.IsEnabled = false;
            }

            string? savedPdfViewerState = ConfigurationManager.AppSettings[PdfViewerSettingKey];
            if (bool.TryParse(savedPdfViewerState, out bool isPdfViewerEnabled))
            {
                _isPdfViewerEnabled = isPdfViewerEnabled;
            }
            else
            {
                _isPdfViewerEnabled = true;
            }

            if (ConfigurationManager.AppSettings[BrowserBackgroundColorKey] != null &&
                ColorConverter.ConvertFromString(ConfigurationManager.AppSettings[BrowserBackgroundColorKey]) is Color bgColor)
            {
                BrowserBackgroundColor = bgColor;
            }
            else
            {
                BrowserBackgroundColor = (Color)Application.Current.Resources["DefaultBrowserBackgroundColor"];
            }

            if (ConfigurationManager.AppSettings[BrowserForegroundColorKey] != null &&
                ColorConverter.ConvertFromString(ConfigurationManager.AppSettings[BrowserForegroundColorKey]) is Color fgColor)
            {
                BrowserForegroundColor = fgColor;
            }
            else
            {
                BrowserForegroundColor = (Color)Application.Current.Resources["DefaultBrowserForegroundColor"];
            }

            string? savedToolbarPosition = ConfigurationManager.AppSettings[ToolbarOrientationKey];
            if (Enum.TryParse(savedToolbarPosition, out ToolbarPosition position))
            {
                _currentToolbarPosition = position;
            }
            else
            {
                _currentToolbarPosition = ToolbarPosition.Top;
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

            if (config.AppSettings.Settings[BrowserBackgroundColorKey] == null)
                config.AppSettings.Settings.Add(BrowserBackgroundColorKey, BrowserBackgroundColor.ToString());
            else
                config.AppSettings.Settings[BrowserBackgroundColorKey].Value = BrowserBackgroundColor.ToString();

            if (config.AppSettings.Settings[BrowserForegroundColorKey] == null)
                config.AppSettings.Settings.Add(BrowserForegroundColorKey, BrowserForegroundColor.ToString());
            else
                config.AppSettings.Settings[BrowserForegroundColorKey].Value = BrowserForegroundColor.ToString();

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
                    else if (!tab.IsIncognito && tab.LeftWebView == null && tab.Tab?.Tag is string suspendedUrl)
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

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
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

            if (_connectivityTimer != null)
            {
                _connectivityTimer.Stop();
                _connectivityTimer.Dispose();
                _connectivityTimer = null;
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

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (this.WindowState == WindowState.Maximized)
                {
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

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            UpdateMaximizeRestoreButtonContent();
        }

        private void UpdateMaximizeRestoreButtonContent()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeRestoreButton!.Content = "❐";
                MaximizeRestoreButton.ToolTip = "Restaurar";
            }
            else
            {
                MaximizeRestoreButton!.Content = "⬜";
                MaximizeRestoreButton.ToolTip = "Maximizar";
            }
        }

        private HwndSource? _hwndSource;
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
            double borderWidth = 5;
            double captionHeight = 30;

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

            if (p.Y < captionHeight) return HTCAPTION;

            return HTCAPTION;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
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
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;

            mmi.ptMinTrackSize.x = (int)SystemParameters.MinimumWindowWidth;
            mmi.ptMinTrackSize.y = (int)SystemParameters.MinimumWindowHeight;

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

        private void ApplyToolbarPosition(ToolbarPosition position)
        {
            Grid mainGrid = (Grid)((Border)this.Content).Child;

            Grid.SetRow(BrowserTabs, 0);
            Grid.SetColumn(BrowserTabs, 0);

            mainGrid.ColumnDefinitions.Clear();
            mainGrid.RowDefinitions.Clear();

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            List<Button> allButtons = new List<Button>();
            allButtons.Add(BackButton); allButtons.Add(ForwardButton); allButtons.Add(ReloadButton);
            allButtons.Add(MinesweeperButton);

            StackPanel? urlAndButtonsStackPanel = AddressBar.Parent as StackPanel;


            switch (position)
            {
                case ToolbarPosition.Top:
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    Grid.SetRow(BrowserTabs, 1);
                    Grid.SetColumn(BrowserTabs, 0);
                    break;

                case ToolbarPosition.Left:
                    MessageBox.Show("Las posiciones de barra de herramientas Izquierda/Derecha/Inferior no están completamente implementadas con la estructura XAML actual. Se usará la posición Superior.", "Funcionalidad Limitada", MessageBoxButton.OK, MessageBoxImage.Information);
                    _currentToolbarPosition = ToolbarPosition.Top;
                    ApplyToolbarPosition(ToolbarPosition.Top);
                    return;

                case ToolbarPosition.Right:
                    MessageBox.Show("Las posiciones de barra de herramientas Izquierda/Derecha/Inferior no están completamente implementadas con la estructura XAML actual. Se usará la posición Superior.", "Funcionalidad Limitada", MessageBoxButton.OK, MessageBoxImage.Information);
                    _currentToolbarPosition = ToolbarPosition.Top;
                    ApplyToolbarPosition(ToolbarPosition.Top);
                    return;

                case ToolbarPosition.Bottom:
                    MessageBox.Show("Las posiciones de barra de herramientas Izquierda/Derecha/Inferior no están completamente implementadas con la estructura XAML actual. Se usará la posición Superior.", "Funcionalidad Limitada", MessageBoxButton.OK, MessageBoxImage.Information);
                    _currentToolbarPosition = ToolbarPosition.Top;
                    ApplyToolbarPosition(ToolbarPosition.Top);
                    return;
            }

            UpdateToolbarButtonForeground();
        }


        private void ConnectivityTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    _connectivityTimer!.Enabled = false;
                    if (!string.IsNullOrEmpty(_lastFailedUrl))
                    {
                        MessageBoxResult result = MessageBox.Show(this,
                            $"¡Internet conectado! ¿Deseas recargar la página:\n{_lastFailedUrl}?",
                            "Conexión Restablecida",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information
                        );

                        if (result == MessageBoxResult.Yes)
                        {
                            AddressBar.Text = _lastFailedUrl;
                            NavigateToUrlInCurrentTab();
                        }
                        _lastFailedUrl = null;
                        _isOfflineGameActive = false;
                    }
                }
            });
        }

        private void ClearFindResults()
        {
            if (FindTextBox != null) FindTextBox.Text = string.Empty;
            if (FindResultsTextBlock != null) FindResultsTextBlock.Text = string.Empty;
            if (_findInPage != null) _findInPage.ClearHighlight();
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (FindBar != null)
            {
                _isFindBarVisible = !_isFindBarVisible;
                FindBar.Visibility = _isFindBarVisible ? Visibility.Visible : Visibility.Collapsed;
                if (_isFindBarVisible)
                {
                    FindTextBox.Focus();
                }
                else
                {
                    ClearFindResults();
                }
            }
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (FindBar != null)
            {
                _isFindBarVisible = false;
                FindBar.Visibility = Visibility.Collapsed;
                ClearFindResults();
            }
        }

        private void CoreWebView2_FindInPageCompleted(object? sender, CoreWebView2FindInPageCompletedEventArgs e)
        {
            if (FindResultsTextBlock != null)
            {
                FindResultsTextBlock.Text = $"{e.Matches} resultados";
            }
        }

        // Manejadores de eventos para los botones de búsqueda (Find)
        private async void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                WebView2? currentWebView = GetCurrentWebView();
                if (currentWebView != null && currentWebView.CoreWebView2 != null)
                {
                    if (_findInPage == null)
                    {
                        _findInPage = currentWebView.CoreWebView2.FindInPage;
                    }
                    _findInPage.Find(FindTextBox.Text, CoreWebView2FindMatchOptions.None);
                }
            }
        }

        private void FindTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (FindTextBox.Text == "Buscar...")
            {
                FindTextBox.Text = string.Empty;
            }
        }

        private void FindTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FindTextBox.Text))
            {
                FindTextBox.Text = "Buscar...";
            }
        }

        private async void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null && _findInPage != null)
            {
                _findInPage.Find(FindTextBox.Text, CoreWebView2FindMatchOptions.None);
            }
        }

        private async void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView != null && currentWebView.CoreWebView2 != null && _findInPage != null)
            {
                _findInPage.Find(FindTextBox.Text, CoreWebView2FindMatchOptions.None | CoreWebView2FindMatchOptions.Backwards);
            }
        }

        // Manejadores de eventos para otros botones que faltaban
        private void PerformanceMonitorButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Monitor de Rendimiento no implementado aún.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            PasswordManagerWindow passwordManagerWindow = new PasswordManagerWindow();
            passwordManagerWindow.Owner = this;
            passwordManagerWindow.ShowDialog();
        }

        private void ExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            ExtensionWindow extensionWindow = new ExtensionWindow(_extensionManager);
            extensionWindow.Owner = this;
            extensionWindow.ShowDialog();
        }

        private void MicrophoneToggleButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show(this, "No hay una página activa para controlar el micrófono.", "Error de Micrófono", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrEmpty(_microphoneControlScript))
            {
                try
                {
                    // Este script debería alternar el estado del micrófono o mostrar un mensaje.
                    // Suponemos que el script MicrophoneControl.js tiene una función para esto.
                    currentWebView.CoreWebView2.ExecuteScriptAsync(_microphoneControlScript);
                    MessageBox.Show("Comando de control de micrófono enviado a la página.", "Micrófono", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error al controlar el micrófono: {ex.Message}", "Error de Micrófono", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(this, "Advertencia: El archivo 'MicrophoneControl.js' no se encontró. El control de micrófono de la página no funcionará.", "Archivo Faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show(this, "No hay una página activa para capturar.", "Error de Captura", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    Filter = "Archivos PNG (*.png)|*.png",
                    Title = "Guardar captura de pantalla como..."
                };
                saveFileDialog.Owner = this;

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        await currentWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
                    }
                    MessageBox.Show(this, $"Captura de pantalla guardada en:\n{saveFileDialog.FileName}", "Captura Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error al guardar la captura de pantalla: {ex.Message}", "Error de Captura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
        Left,
        Right,
        Bottom
    }
}
