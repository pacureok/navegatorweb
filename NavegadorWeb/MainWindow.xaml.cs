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

        private bool _isFindBarVisible = false;
        [cite_start]// Eliminada la variable _findInPage, ya que CoreWebView2FindInPage no existe. 

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
                    "WebView2 Runtime Requerido",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo("cmd", "/c start https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section") { CreateNoWindow = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No se pudo abrir el enlace de descarga: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                this.Close();
                return;
            }

            // Inicializar el entorno predeterminado (para pestañas normales)
            _defaultEnvironment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiNavegadorWeb", "WebView2UserData")
            );

            // Inicializar el entorno de incógnito (modo privado)
            _incognitoEnvironment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: Path.Combine(Path.GetTempPath(), "MiNavegadorWebIncognito", Guid.NewGuid().ToString())
            );

            // Asegúrate de que los entornos se inicialicen antes de agregar pestañas.
            // Esto es crucial para evitar errores de referencia nula.
            AddNewTab(_defaultHomePage, _defaultEnvironment); // Carga la página de inicio por defecto
            LoadLastSession(); // Intenta restaurar la sesión después de inicializar
        }


        // Implementación de la interfaz INotifyPropertyChanged
        // ya está presente en el snippet, no hay cambios aquí.
        // public event PropertyChangedEventHandler? PropertyChanged;
        // protected void OnPropertyChanged(string propertyName) { ... }


        // Resto de métodos (se asume que están correctos o se corregirán a medida que aparezcan errores específicos)

        // Manejadores de eventos de la ventana
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // La inicialización de los entornos y la adición de pestañas ahora se maneja en InitializeEnvironments.
            // Asegúrate de que InitializeEnvironments sea llamado en el constructor antes de Window_Loaded,
            // o que la lógica de InitializeEnvironments sea asíncrona y no bloquee el UI.
            // Si la llamada a InitializeEnvironments es async, el Window_Loaded no debe depender de ella directamente
            // o esperar a que termine.
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveLastSession();
            Application.Current.Shutdown();
        }

        // Manejo de los botones de la barra de título
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

        private void UpdateMaximizeRestoreButtonContent()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeRestoreButton.Content = "🗗"; // Icono de restaurar
                MaximizeRestoreButton.ToolTip = "Restaurar";
            }
            else
            {
                MaximizeRestoreButton.Content = "🗖"; // Icono de maximizar
                MaximizeRestoreButton.ToolTip = "Maximizar";
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // Doble clic para maximizar/restaurar
            {
                MaximizeRestoreButton_Click(sender, e);
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove(); // Mover la ventana
            }
        }

        // Manejo de la barra de direcciones
        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string url = AddressBar.Text;
                NavigateToUrl(url);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabItem?.Tab?.GoBack();
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabItem?.Tab?.GoForward();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTabItem?.Tab?.Reload();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl(_defaultHomePage);
        }

        private void NavigateToUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                string navigatedUrl = url;
                if (!url.Contains("://"))
                {
                    // Si no tiene esquema, intenta prefijar con "https://" o usar el motor de búsqueda
                    if (url.Contains(".") && !url.Contains(" ")) // Es una URL, no una frase de búsqueda simple
                    {
                        navigatedUrl = "https://" + url;
                    }
                    else // Es una frase de búsqueda
                    {
                        navigatedUrl = _defaultSearchEngineUrl + Uri.EscapeDataString(url);
                    }
                }
                SelectedTabItem?.Tab?.Navigate(navigatedUrl);
            }
        }

        // Métodos relacionados con pestañas
        public void AddNewTab(string url = "about:blank", CoreWebView2Environment? environment = null, bool isIncognito = false)
        {
            if (environment == null)
            {
                environment = isIncognito ? _incognitoEnvironment : _defaultEnvironment;
            }

            if (environment == null)
            {
                MessageBox.Show("Error: El entorno de WebView2 no está inicializado. No se puede abrir una nueva pestaña.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            BrowserTabItem newTabItem = new BrowserTabItem(url, environment, isIncognito);
            _tabGroupManager.GetDefaultGroup().TabsInGroup.Add(newTabItem);
            BrowserTabs.SelectedItem = newTabItem;

            // Vincular los eventos de la pestaña
            newTabItem.Tab!.ContentLoading += WebView_ContentLoading;
            newTabItem.Tab.NavigationCompleted += WebView_NavigationCompleted;
            newTabItem.Tab.SourceChanged += WebView_SourceChanged;
            newTabItem.Tab.TitleChanged += WebView_TitleChanged;
            newTabItem.Tab.IsAudioPlayingChanged += WebView_IsAudioPlayingChanged;
            newTabItem.Tab.IconChanged += WebView_IconChanged;
            newTabItem.Tab.ContainsFullScreenElementChanged += WebView_ContainsFullScreenElementChanged;
            newTabItem.Tab.NewWindowRequested += WebView_NewWindowRequested;
            newTabItem.Tab.WebMessageReceived += WebView_WebMessageReceived;
            newTabItem.Tab.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;


            // Actualizar el HeaderTemplate de las pestañas
            // Esto ya se hace al inicializar el TabControl, pero si necesitas actualizarlo dinámicamente
            // aquí es donde lo harías (ej: BrowserTabs.ItemTemplate = CreateTabHeaderTemplate();)
        }

        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            if (BrowserTabs.SelectedItem is BrowserTabItem selectedTab)
            {
                SelectedTabItem = selectedTab;
                AddressBar.Text = selectedTab.AddressBarText; // Actualiza la barra de dirección
                WindowTitleText.Text = selectedTab.Title; // Actualiza el título de la ventana

                // Cuando cambia la pestaña, asegúrate de que la barra de búsqueda se reinicie
                CloseFindBarButton_Click(null, null); // Cierra la barra de búsqueda de la pestaña anterior
            }
            else
            {
                // Si no hay pestañas seleccionadas (ej: al cerrar la última)
                AddressBar.Text = "";
                WindowTitleText.Text = "Aurora Browser";
            }
            UpdateToolbarButtonForeground(); // Asegurar que los colores de los botones se apliquen correctamente.
        }

        private void CloseBrowserTab(WebView2 webViewToClose)
        {
            BrowserTabItem? tabItemToRemove = _tabGroupManager.GetDefaultGroup().TabsInGroup
                                            .FirstOrDefault(ti => ti.Tab == webViewToClose);

            if (tabItemToRemove != null)
            {
                // Dispose del WebView2 para liberar recursos
                tabItemToRemove.Tab?.Dispose();

                // Quitar la pestaña del TabGroupManager
                _tabGroupManager.GetDefaultGroup().TabsInGroup.Remove(tabItemToRemove);

                // Si no quedan pestañas, cerrar la ventana o abrir una nueva pestaña por defecto
                if (!_tabGroupManager.GetDefaultGroup().TabsInGroup.Any())
                {
                    this.Close(); // O AddNewTab();
                }
            }
        }

        // Manejadores de eventos de WebView2
        private void WebView_ContentLoading(object? sender, CoreWebView2ContentLoadingEventArgs e)
        {
            if (sender is WebView2 currentWebView)
            {
                var tab = _tabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault(t => t.Tab == currentWebView);
                if (tab != null)
                {
                    tab.IsLoading = true;
                    // Opcional: Mostrar un indicador de carga en la pestaña
                }
            }
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (sender is WebView2 currentWebView)
            {
                var tab = _tabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault(t => t.Tab == currentWebView);
                if (tab != null)
                {
                    tab.IsLoading = false;
                    tab.AddressBarText = currentWebView.Source.OriginalString; // Actualiza la URL
                    AddressBar.Text = tab.AddressBarText; // Asegura que la barra de URL refleje la de la pestaña activa

                    // Si la navegación falló, podríamos mostrar una página de error local
                    if (!e.IsSuccess)
                    {
                        // currentWebView.NavigateToString("<h1>Error de navegación</h1><p>No se pudo cargar la página.</p>");
                        // Considera una página de error más robusta o lógica de reintento.
                    }

                    // Después de que la navegación se completa, inyectar el script de acoplamiento de color
                    if (currentWebView.CoreWebView2 != null && !string.IsNullOrEmpty(_pageColorExtractionScript))
                    {
                        // Se ejecuta el script y se espera que envíe un web message con el color dominante
                        currentWebView.CoreWebView2.ExecuteScriptAsync(_pageColorExtractionScript);
                    }
                    // Ejecutar script de micrófono si está habilitado
                    if (currentWebView.CoreWebView2 != null && !string.IsNullOrEmpty(_microphoneControlScript))
                    {
                        currentWebView.CoreWebView2.ExecuteScriptAsync(_microphoneControlScript);
                    }

                    // Asegurarse de que el modo oscuro se aplique al cargar una nueva página
                    if (_isDarkModeEnabled)
                    {
                        ApplyDarkModeToWebView(currentWebView);
                    }

                    // Resetear el estado del juego offline si estaba activo y ahora hay conexión
                    if (_isOfflineGameActive && NetworkInterface.GetIsNetworkAvailable())
                    {
                        _isOfflineGameActive = false;
                        SelectedTabItem?.SetOfflineGameStatus(false);
                    }
                }
            }
        }

        private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (sender is WebView2 currentWebView)
            {
                var tab = _tabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault(t => t.Tab == currentWebView);
                if (tab != null)
                {
                    tab.AddressBarText = currentWebView.Source.OriginalString;
                    if (SelectedTabItem == tab)
                    {
                        AddressBar.Text = tab.AddressBarText;
                    }
                }
            }
        }

        private void WebView_TitleChanged(object? sender, object e)
        {
            if (sender is WebView2 currentWebView)
            {
                var tab = _tabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault(t => t.Tab == currentWebView);
                if (tab != null)
                {
                    tab.Title = currentWebView.CoreWebView2.DocumentTitle;
                    if (SelectedTabItem == tab)
                    {
                        WindowTitleText.Text = tab.Title;
                    }
                }
            }
        }

        private void WebView_IsAudioPlayingChanged(object? sender, object e)
        {
            if (sender is WebView2 currentWebView)
            {
                var tab = _tabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault(t => t.Tab == currentWebView);
                if (tab != null)
                {
                    tab.IsAudioPlaying = currentWebView.CoreWebView2.IsAudioPlaying;
                }
            }
        }

        private void WebView_IconChanged(object? sender, object e)
        {
            if (sender is WebView2 currentWebView)
            {
                var tab = _tabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault(t => t.Tab == currentWebView);
                if (tab != null)
                {
                    // Puedes obtener el favicon a través de CoreWebView2.FaviconUri
                    // y cargarlo como una ImageSource en tu TabItem.IconSource
                    if (!string.IsNullOrEmpty(currentWebView.CoreWebView2.FaviconUri))
                    {
                        try
                        {
                            tab.IconSource = new BitmapImage(new Uri(currentWebView.CoreWebView2.FaviconUri));
                        }
                        catch (UriFormatException)
                        {
                            // Ignorar URIs de favicon inválidas
                        }
                    }
                    else
                    {
                        tab.IconSource = null; // O un icono por defecto
                    }
                }
            }
        }

        private void WebView_ContainsFullScreenElementChanged(object? sender, object e)
        {
            if (sender is WebView2 currentWebView)
            {
                // Ajusta el estado de la ventana principal si WebView2 entra o sale de pantalla completa
                if (currentWebView.CoreWebView2.ContainsFullScreenElement)
                {
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    this.WindowStyle = WindowStyle.None; // Vuelve al estilo "None" que habíamos establecido
                    // Si ya estaba maximizada por el usuario, debería volver a maximizada
                    // Si estaba normal, debería volver a normal.
                    this.WindowState = WindowState.Normal; // O el estado previo si lo guardaste
                }
                UpdateMaximizeRestoreButtonContent();
            }
        }

        private void WebView_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Abrir la nueva URL en una nueva pestaña
            e.Handled = true; // Indicar que manejamos la solicitud
            AddNewTab(e.Uri, _defaultEnvironment);
        }

        private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (sender is WebView2 currentWebView)
            {
                string message = e.WebMessageAsJson;
                // Procesar mensajes web (ej. desde scripts inyectados)
                // Si el script de extracción de color envía un mensaje:
                if (message.Contains("dominantColor"))
                {
                    try
                    {
                        var colorData = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
                        if (colorData != null && colorData.TryGetValue("dominantColor", out string? hexColor))
                        {
                            if (ColorConverter.ConvertFromString(hexColor) is Color extractedColor)
                            {
                                // Aquí puedes usar el color extraído, por ejemplo, para ajustar el tema del navegador.
                                // Cuidado de no cambiar el tema constantemente si el color cambia muy rápido.
                                // Console.WriteLine($"Dominant color: {extractedColor}");
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Mensaje no es JSON válido o no es el esperado
                    }
                }
                else if (message.Contains("MicrophoneStatus"))
                {
                    // Manejar mensajes del script de control de micrófono
                    try
                    {
                        var statusData = JsonSerializer.Deserialize<Dictionary<string, bool>>(message);
                        if (statusData != null && statusData.TryGetValue("MicrophoneStatus", out bool isMicOn))
                        {
                            // Actualizar algún indicador de UI si el micrófono de la página está activo
                            // Console.WriteLine($"Micrófono de página: {isMicOn}");
                        }
                    }
                    catch (JsonException) { }
                }
                else if (message == "\"toggleFullscreen\"")
                {
                    // Este mensaje viene de un script para alternar pantalla completa
                    ToggleFullscreen(null);
                }
                // Si tienes un script para encontrar en la página, también podrías manejar su comunicación aquí
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (sender is WebView2 currentWebView)
            {
                if (e.IsSuccess)
                {
                    // Configurar el WebView2 después de que se haya inicializado CoreWebView2
                    currentWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false; // Deshabilita atajos de teclado del navegador (Ctrl+N, Ctrl+T, etc.)
                    currentWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                    currentWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;
                    currentWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
                    currentWebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
                    currentWebView.CoreWebView2.Settings.IsStatusBarEnabled = false; // Oculta la barra de estado inferior
                    currentWebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false; // Deshabilita navegación con swipe
                    currentWebView.CoreWebView2.Settings.AreDevToolsEnabled = true; // Habilita herramientas de desarrollador
                    currentWebView.CoreWebView2.Settings.IsWebMessageEnabled = true; // Habilita la comunicación entre web y C#

                    // Registrar para eventos específicos si es necesario
                    // currentWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                    // currentWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

                    // Si tienes un TabItem que necesita esta instancia de WebView2, asegúrate de que esté asignada
                    var tabItem = _tabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault(t => t.Tab == currentWebView);
                    if (tabItem != null)
                    {
                        tabItem.CoreWebView2 = currentWebView.CoreWebView2;
                    }

                    // Después de la inicialización, inyectar el script de acoplamiento de color si no está ya inyectado
                    // Esto es útil si el script necesita ejecutarse tan pronto como la página esté lista
                    if (!string.IsNullOrEmpty(_pageColorExtractionScript))
                    {
                        currentWebView.CoreWebView2.ExecuteScriptAsync(_pageColorExtractionScript);
                    }
                    if (!string.IsNullOrEmpty(_microphoneControlScript))
                    {
                        currentWebView.CoreWebView2.ExecuteScriptAsync(_microphoneControlScript);
                    }
                    // Aplicar modo oscuro si es necesario
                    if (_isDarkModeEnabled)
                    {
                        ApplyDarkModeToWebView(currentWebView);
                    }
                }
                else
                {
                    MessageBox.Show($"Error al inicializar CoreWebView2: {e.InitializationException.Message}", "Error de WebView2", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Métodos para la barra de búsqueda (Find Bar)
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            _isFindBarVisible = !_isFindBarVisible;
            OnPropertyChanged(nameof(IsFindBarVisible)); // Notifica a la UI que la propiedad ha cambiado
            if (_isFindBarVisible)
            {
                FindTextBox.Focus();
                FindTextBox.SelectAll();
            }
            else
            {
                ClearFindResults();
            }
        }

        private void CloseFindBarButton_Click(object? sender, RoutedEventArgs? e)
        {
            _isFindBarVisible = false;
            OnPropertyChanged(nameof(IsFindBarVisible));
            ClearFindResults();
        }

        private async void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await PerformFindInPage();
            }
        }

        private async void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformFindInPage(forward: true);
        }

        private async void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformFindInPage(forward: false);
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Opcional: limpiar los resultados de búsqueda al cambiar el texto
            // ClearFindResults();
            // Esto es más óptimo si la búsqueda se inicia solo al presionar Enter o los botones de navegación.
        }

        private async Task PerformFindInPage(bool? forward = null)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView?.CoreWebView2 == null) return;

            string searchText = FindTextBox.Text;
            if (string.IsNullOrEmpty(searchText))
            {
                ClearFindResults();
                return;
            }

            // Realiza la búsqueda
            CoreWebView2FindInPageResult result;
            if (forward.HasValue)
            {
                result = await currentWebView.CoreWebView2.FindInPageAsync(searchText, forward.Value, false, false);
            }
            else // Primera búsqueda o al presionar Enter
            {
                result = await currentWebView.CoreWebView2.FindInPageAsync(searchText, true, false, false);
            }

            // Actualiza el texto de resultados
            if (result.Matches > 0)
            {
                FindResultsTextBlock.Text = $"{result.Index + 1} de {result.Matches}";
            }
            else
            {
                FindResultsTextBlock.Text = "No se encontraron resultados";
            }
        }

        private void ClearFindResults()
        {
            FindResultsTextBlock.Text = "";
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView?.CoreWebView2 != null)
            {
                currentWebView.CoreWebView2.StopFindInPage(CoreWebView2FindInPageKind.ClearSelection);
            }
        }


        // Métodos de Settings (Carga y Guardado)
        private void LoadSettings()
        {
            _defaultHomePage = ConfigurationManager.AppSettings[HomePageSettingKey] ?? "https://www.google.com";
            // ... cargar otras configuraciones ...
            if (bool.TryParse(ConfigurationManager.AppSettings[AdBlockerSettingKey], out bool adBlockerEnabled))
            {
                AdBlocker.IsEnabled = adBlockerEnabled;
            }
            _defaultSearchEngineUrl = ConfigurationManager.AppSettings[DefaultSearchEngineSettingKey] ?? "https://www.google.com/search?q=";

            if (bool.TryParse(ConfigurationManager.AppSettings[TabSuspensionSettingKey], out bool tabSuspensionEnabled))
            {
                _isTabSuspensionEnabled = tabSuspensionEnabled;
            }

            if (bool.TryParse(ConfigurationManager.AppSettings[RestoreSessionSettingKey], out bool restoreSession))
            {
                _restoreSessionOnStartup = restoreSession;
            }

            if (bool.TryParse(ConfigurationManager.AppSettings[PdfViewerSettingKey], out bool pdfViewerEnabled))
            {
                _isPdfViewerEnabled = pdfViewerEnabled;
            }

            if (Enum.TryParse(ConfigurationManager.AppSettings[ToolbarOrientationKey], out ToolbarPosition toolbarPos))
            {
                _currentToolbarPosition = toolbarPos;
            }

            if (ColorConverter.ConvertFromString(ConfigurationManager.AppSettings[BrowserBackgroundColorKey]) is Color bgColor)
            {
                BrowserBackgroundColor = bgColor;
            }
            else
            {
                BrowserBackgroundColor = (Color)Application.Current.Resources["DefaultBrowserBackgroundColor"];
            }

            if (ColorConverter.ConvertFromString(ConfigurationManager.AppSettings[BrowserForegroundColorKey]) is Color fgColor)
            {
                BrowserForegroundColor = fgColor;
            }
            else
            {
                BrowserForegroundColor = (Color)Application.Current.Resources["DefaultBrowserForegroundColor"];
            }

            // Cargar estado de Tracker Protection
            if (bool.TryParse(ConfigurationManager.AppSettings[TrackerProtectionSettingKey], out bool trackerProtectionEnabled))
            {
                TrackerBlocker.IsEnabled = trackerProtectionEnabled;
            }
        }

        private void SaveSettings()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationManager.AppSettings.File);
            config.AppSettings.Settings[HomePageSettingKey].Value = _defaultHomePage;
            config.AppSettings.Settings[AdBlockerSettingKey].Value = AdBlocker.IsEnabled.ToString();
            config.AppSettings.Settings[DefaultSearchEngineSettingKey].Value = _defaultSearchEngineUrl;
            config.AppSettings.Settings[TabSuspensionSettingKey].Value = _isTabSuspensionEnabled.ToString();
            config.AppSettings.Settings[RestoreSessionSettingKey].Value = _restoreSessionOnStartup.ToString();
            config.AppSettings.Settings[PdfViewerSettingKey].Value = _isPdfViewerEnabled.ToString();
            config.AppSettings.Settings[ToolbarOrientationKey].Value = _currentToolbarPosition.ToString();
            config.AppSettings.Settings[BrowserBackgroundColorKey].Value = BrowserBackgroundColor.ToString();
            config.AppSettings.Settings[BrowserForegroundColorKey].Value = BrowserForegroundColor.ToString();
            config.AppSettings.Settings[TrackerProtectionSettingKey].Value = TrackerBlocker.IsEnabled.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        // Métodos para persistencia de sesión
        private void SaveLastSession()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationManager.AppSettings.File);
            var sessionUrls = _tabGroupManager.GetDefaultGroup().TabsInGroup
                                .Where(tab => !tab.IsIncognito && tab.Tab?.Source != null)
                                .Select(tab => tab.Tab!.Source.OriginalString)
                                .ToList();

            string jsonUrls = JsonSerializer.Serialize(sessionUrls);
            if (config.AppSettings.Settings[LastSessionUrlsSettingKey] == null)
            {
                config.AppSettings.Settings.Add(LastSessionUrlsSettingKey, jsonUrls);
            }
            else
            {
                config.AppSettings.Settings[LastSessionUrlsSettingKey].Value = jsonUrls;
            }

            // Guardar flag de cierre "limpio"
            if (config.AppSettings.Settings[UncleanShutdownFlagKey] == null)
            {
                config.AppSettings.Settings.Add(UncleanShutdownFlagKey, "false");
            }
            else
            {
                config.AppSettings.Settings[UncleanShutdownFlagKey].Value = "false";
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void LoadLastSession()
        {
            if (_restoreSessionOnStartup && (ConfigurationManager.AppSettings[UncleanShutdownFlagKey] == null || ConfigurationManager.AppSettings[UncleanShutdownFlagKey] == "true"))
            {
                string? jsonUrls = ConfigurationManager.AppSettings[LastSessionUrlsSettingKey];
                if (!string.IsNullOrEmpty(jsonUrls))
                {
                    try
                    {
                        List<string>? sessionUrls = JsonSerializer.Deserialize<List<string>>(jsonUrls);
                        if (sessionUrls != null && sessionUrls.Any())
                        {
                            // Limpiar la pestaña inicial "about:blank"
                            if (_tabGroupManager.GetDefaultGroup().TabsInGroup.Any() && _tabGroupManager.GetDefaultGroup().TabsInGroup.First().Tab?.Source.OriginalString == "about:blank")
                            {
                                CloseBrowserTab(_tabGroupManager.GetDefaultGroup().TabsInGroup.First().Tab!);
                            }

                            foreach (string url in sessionUrls)
                            {
                                AddNewTab(url, _defaultEnvironment);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"Error al cargar la sesión anterior: {ex.Message}", "Error de Sesión", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            // Después de cargar la sesión, establecer el flag de cierre como "no limpio"
            // para la próxima vez, a menos que se guarde correctamente.
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationManager.AppSettings.File);
            if (config.AppSettings.Settings[UncleanShutdownFlagKey] == null)
            {
                config.AppSettings.Settings.Add(UncleanShutdownFlagKey, "true");
            }
            else
            {
                config.AppSettings.Settings[UncleanShutdownFlagKey].Value = "true";
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        // Métodos de navegación y gestión de UI
        private WebView2? GetCurrentWebView()
        {
            return SelectedTabItem?.Tab;
        }

        private void ApplyToolbarPosition(ToolbarPosition position)
        {
            // Lógica para cambiar la posición de la barra de herramientas
            // Tendrás que modificar las definiciones de Grid.Row y Grid.Column
            // en MainWindow.xaml para que esto tenga efecto.
            // Esto implica reordenar o cambiar las propiedades adjuntas de Grid en el StackPanel de la barra de herramientas.
            // Para simplificar, asumiremos que la barra de herramientas siempre está en Grid.Row="1" y la barra de búsqueda en Grid.Row="2".
            // Para cambiar su posición, necesitarías una lógica más compleja que modifique la estructura del Grid en tiempo de ejecución
            // o usar un Grid más flexible con Rows/Columns definidas por código o Triggers complejos en XAML.
            // De momento, solo ajusta la apariencia de los botones si es necesario.
        }

        // Métodos para abrir ventanas secundarias
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
            // Recargar configuraciones después de que la ventana de configuración se cierre
            LoadSettings();
            // Aplicar los nuevos colores o posición de la barra de herramientas inmediatamente
            ApplyForegroundToWindowControls();
            ApplyToolbarPosition(_currentToolbarPosition);
        }

        private void PasswordsButton_Click(object sender, RoutedEventArgs e)
        {
            PasswordManagerWindow passwordWindow = new PasswordManagerWindow();
            passwordWindow.Owner = this;
            passwordWindow.ShowDialog();
        }

        private void ExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            ExtensionManagerWindow extensionManagerWindow = new ExtensionManagerWindow(_extensionManager);
            extensionManagerWindow.Owner = this;
            extensionManagerWindow.ShowDialog();
            // Después de que la ventana de extensiones se cierra, recargar extensiones si es necesario
            _extensionManager.LoadExtensions();
        }

        private void PerformanceButton_Click(object sender, RoutedEventArgs e)
        {
            PerformanceMonitorWindow perfMonitorWindow = new PerformanceMonitorWindow();
            perfMonitorWindow.Owner = this;
            perfMonitorWindow.Show(); // Mostrar como no modal
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryWindow historyWindow = new HistoryWindow();
            historyWindow.Owner = this;
            historyWindow.ShowDialog();
        }

        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            BookmarkManagerWindow bookmarkWindow = new BookmarkManagerWindow();
            bookmarkWindow.Owner = this;
            bookmarkWindow.ShowDialog();
        }

        private void DownloadsButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadManagerWindow downloadWindow = new DownloadManagerWindow();
            downloadWindow.Owner = this;
            downloadWindow.ShowDialog();
        }

        // Modos especiales
        private bool _isReaderModeActive = false;
        private async void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView?.CoreWebView2 == null || string.IsNullOrEmpty(_readerModeScript)) return;

            _isReaderModeActive = !_isReaderModeActive;

            if (_isReaderModeActive)
            {
                await currentWebView.CoreWebView2.ExecuteScriptAsync(_readerModeScript);
            }
            else
            {
                // Para salir del modo lectura, la forma más sencilla es recargar la página
                // o inyectar un script que deshaga los cambios. Recargar es más fiable.
                currentWebView.Reload();
            }
            // Opcional: Cambiar el icono del botón para reflejar el estado
            // (sender as Button).Content = _isReaderModeActive ? "📄 (Activo)" : "📄";
        }

        private bool _isDarkModeEnabled = false; // Estado para el modo oscuro
        private async void ToggleDarkModeButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkModeEnabled = !_isDarkModeEnabled;
            WebView2? currentWebView = GetCurrentWebView();

            if (currentWebView?.CoreWebView2 != null)
            {
                if (_isDarkModeEnabled)
                {
                    ApplyDarkModeToWebView(currentWebView);
                }
                else
                {
                    // Desactivar modo oscuro (recargar o inyectar script para revertir)
                    currentWebView.Reload(); // La forma más sencilla de revertir.
                }
            }
            // Opcional: Cambiar el icono del botón
            // (sender as Button).Content = _isDarkModeEnabled ? "☀️" : "🌙";
        }

        private async void ApplyDarkModeToWebView(WebView2 webView)
        {
            if (webView.CoreWebView2 == null || string.IsNullOrEmpty(_darkModeScript)) return;

            await webView.CoreWebView2.ExecuteScriptAsync(_darkModeScript);
        }


        private void NewIncognitoTabButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab("about:blank", _incognitoEnvironment, isIncognito: true);
        }

        private void PipButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView?.CoreWebView2 == null) return;

            string videoUrl = currentWebView.Source.OriginalString;
            if (string.IsNullOrEmpty(videoUrl) || !IsVideoUrl(videoUrl))
            {
                MessageBox.Show("No hay un video válido para mostrar en Picture-in-Picture.", "Advertencia PIP", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Crear una nueva instancia de PipWindow con la URL del video y el mismo entorno CoreWebView2
            PipWindow pipWindow = new PipWindow(videoUrl, currentWebView.CoreWebView2.Environment);
            pipWindow.Show();
        }

        private bool IsVideoUrl(string url)
        {
            // Implementa aquí la lógica para detectar si la URL contiene un video.
            // Esto podría ser complejo y puede requerir expresiones regulares o librerías externas.
            // Para YouTube, podrías buscar "youtube.com/watch" o "youtu.be".
            // Para otros, podrías buscar extensiones de archivo de video (.mp4, .webm, .ogg, etc.)
            // o encabezados Content-Type si hicieras una solicitud HTTP.
            return url.Contains("youtube.com/watch") || url.Contains("youtu.be") ||
                   url.EndsWith(".mp4") || url.EndsWith(".webm") || url.EndsWith(".ogg");
        }

        private void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView?.CoreWebView2 == null) return;

            if (_isReadingAloud)
            {
                _speechSynthesizer.SpeakAsyncCancelAll();
                _isReadingAloud = false;
                // Opcional: Cambiar el icono del botón a "Reproducir"
            }
            else
            {
                // Obtener el texto de la página. Esto es complejo y puede requerir JavaScript.
                // Aquí hay un enfoque simple que podría no funcionar para todas las páginas:
                string script = "(function() { return document.body.innerText; })();";
                currentWebView.CoreWebView2.ExecuteScriptAsync(script).ContinueWith(task =>
                {
                    string pageTextJson = task.Result;
                    string pageText = JsonSerializer.Deserialize<string>(pageTextJson) ?? string.Empty;

                    if (!string.IsNullOrEmpty(pageText))
                    {
                        // Limitar el texto para evitar sobrecarga del sintetizador de voz
                        string textToSpeak = pageText.Length > 2000 ? pageText.Substring(0, 2000) + "..." : pageText;

                        Dispatcher.Invoke(() =>
                        {
                            _speechSynthesizer.SpeakAsync(textToSpeak);
                            _isReadingAloud = true;
                            // Opcional: Cambiar el icono del botón a "Pausar"
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("No se encontró texto en la página para leer en voz alta.", "Lectura en Voz Alta", MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    }
                });
            }
        }

        // Manejo de la conectividad y juego offline
        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            // Solo usar si necesitas controlar el redimensionamiento personalizado o eventos de ventana de bajo nivel.
            // IntPtr hwnd = new WindowInteropHelper(this).Handle;
            // HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Lógica para redimensionar sin bordes estándar
            switch (msg)
            {
                case WM_NCHITTEST:
                    Point mousePoint = new Point(lParam.ToInt32() & 0x0000FFFF, lParam.ToInt32() >> 16);
                    double borderThickness = 5; // Grosor para redimensionar
                    if (mousePoint.Y <= borderThickness)
                    {
                        if (mousePoint.X <= borderThickness) handled = true; return new IntPtr(HTTOPLEFT);
                        if (mousePoint.X >= this.ActualWidth - borderThickness) handled = true; return new IntPtr(HTTOPRIGHT);
                        handled = true; return new IntPtr(HTTOP);
                    }
                    else if (mousePoint.Y >= this.ActualHeight - borderThickness)
                    {
                        if (mousePoint.X <= borderThickness) handled = true; return new IntPtr(HTBOTTOMLEFT);
                        if (mousePoint.X >= this.ActualWidth - borderThickness) handled = true; return new IntPtr(HTBOTTOMRIGHT);
                        handled = true; return new IntPtr(HTBOTTOM);
                    }
                    else if (mousePoint.X <= borderThickness) { handled = true; return new IntPtr(HTLEFT); }
                    else if (mousePoint.X >= this.ActualWidth - borderThickness) { handled = true; return new IntPtr(HTRIGHT); }
                    else if (mousePoint.Y < TitleBarGrid.ActualHeight) { handled = true; return new IntPtr(HTCAPTION); } // Área arrastrable
                    break;
            }
            return IntPtr.Zero;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            UpdateMaximizeRestoreButtonContent();
        }

        private void ConnectivityTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            bool isConnected = NetworkInterface.GetIsNetworkAvailable();
            if (!isConnected && !_isOfflineGameActive)
            {
                Dispatcher.Invoke(() =>
                {
                    // Solo activar el juego offline una vez por desconexión
                    _isOfflineGameActive = true;
                    SelectedTabItem?.SetOfflineGameStatus(true);
                    MessageBox.Show("Parece que no tienes conexión a Internet. ¡Se ha activado el juego offline!", "Sin Conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
            else if (isConnected && _isOfflineGameActive)
            {
                // Cuando la conexión se restaura, reiniciar el estado del juego offline
                Dispatcher.Invoke(() =>
                {
                    _isOfflineGameActive = false;
                    SelectedTabItem?.SetOfflineGameStatus(false);
                });
            }
        }

        private void ToggleGeminiModeButton_Click(object sender, RoutedEventArgs e)
        {
            _isGeminiModeActive = !_isGeminiModeActive;

            if (_isGeminiModeActive)
            {
                // Activar el modo Gemini: cambia colores, etc.
                ApplyGeminiModeColors();
                MessageBox.Show("Modo Gemini activado. Ahora puedes usar el botón de IA para ver datos y enviar a Gemini.", "Modo Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Desactivar el modo Gemini: restaurar colores por defecto
                RestoreDefaultColors();
                MessageBox.Show("Modo Gemini desactivado.", "Modo Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyGeminiModeColors()
        {
            Application.Current.Resources["BrowserBackgroundColor"] = (Color)Application.Current.Resources["GeminiBackgroundColor"];
            Application.Current.Resources["BrowserBackgroundBrush"] = (SolidColorBrush)Application.Current.Resources["GeminiBackgroundBrush"];
            Application.Current.Resources["BrowserForegroundColor"] = (Color)Application.Current.Resources["GeminiForegroundColor"];
            Application.Current.Resources["BrowserForegroundBrush"] = (SolidColorBrush)Application.Current.Resources["GeminiForegroundBrush"];

            BrowserBackgroundColor = (Color)Application.Current.Resources["GeminiBackgroundColor"];
            BrowserForegroundColor = (Color)Application.Current.Resources["GeminiForegroundColor"];

            ApplyForegroundToWindowControls();
        }

        private void RestoreDefaultColors()
        {
            Application.Current.Resources["BrowserBackgroundColor"] = (Color)Application.Current.Resources["DefaultBrowserBackgroundColor"];
            Application.Current.Resources["BrowserBackgroundBrush"] = (SolidColorBrush)Application.Current.Resources["DefaultBrowserBackgroundBrush"];
            Application.Current.Resources["BrowserForegroundColor"] = (Color)Application.Current.Resources["DefaultBrowserForegroundColor"];
            Application.Current.Resources["BrowserForegroundBrush"] = (SolidColorBrush)Application.Current.Resources["DefaultBrowserForegroundBrush"];

            BrowserBackgroundColor = (Color)Application.Current.Resources["DefaultBrowserBackgroundColor"];
            BrowserForegroundColor = (Color)Application.Current.Resources["DefaultBrowserForegroundColor"];

            ApplyForegroundToWindowControls();
        }

        private async void AiButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isGeminiModeActive)
            {
                MessageBox.Show("El modo Gemini debe estar activado para usar esta función.", "Modo Gemini Desactivado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Capturar datos de la página actual
            WebView2? currentWebView = GetCurrentWebView();
            if (currentWebView?.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página web activa para capturar datos.", "Error de Captura", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // **IMPORTANTE**: La captura de datos real (texto, imágenes, etc.) requiere inyectar JavaScript
            // para extraer la información del DOM. Esto es un ejemplo simplificado.
            // Para una extracción robusta, necesitarías un script JS más avanzado y el manejo de mensajes web.

            // Ejemplo: Extraer el título de la página y el texto visible
            string pageTitle = await currentWebView.CoreWebView2.ExecuteScriptAsync("document.title");
            pageTitle = System.Text.Json.JsonSerializer.Deserialize<string>(pageTitle) ?? "Sin título";

            string pageContent = await currentWebView.CoreWebView2.ExecuteScriptAsync("(function() { return document.body.innerText; })();");
            pageContent = System.Text.Json.JsonSerializer.Deserialize<string>(pageContent) ?? "Sin contenido";

            // Limitar el contenido a un tamaño razonable para no exceder límites de la API de Gemini
            if (pageContent.Length > 2000) // Ejemplo de límite
            {
                pageContent = pageContent.Substring(0, 2000) + "...";
            }

            // Aquí deberías capturar más datos según sea necesario (capturas de pantalla, favicon, etc.)
            // Las capturas de pantalla de WebView2 son complejas y requieren renderizado a un Bitmap.
            // currentWebView.CoreWebView2.CapturePreviewToImageAsync(...)

            ObservableCollection<CapturedPageData> capturedData = new ObservableCollection<CapturedPageData>();
            capturedData.Add(new CapturedPageData
            {
                Url = currentWebView.Source.OriginalString,
                Title = pageTitle,
                ExtractedText = pageContent,
                ScreenshotBase64 = "", // Aquí iría la captura de pantalla en Base64
                FaviconBase64 = "" // Aquí iría el favicon en Base64
            });

            // Mostrar la ventana de visualización de datos de Gemini
            GeminiDataViewerWindow dataViewer = new GeminiDataViewerWindow("¿Qué información relevante hay en esta página?", capturedData);
            dataViewer.Owner = this;

            if (dataViewer.ShowDialog() == true)
            {
                // Si el usuario hace clic en "Enviar a Gemini" en GeminiDataViewerWindow
                string userQuestion = dataViewer.UserQuestion;
                // Aquí iría la llamada real a la API de Gemini
                MessageBox.Show($"Enviando a Gemini:\nPregunta: {userQuestion}\nDatos capturados de {capturedData.Count} páginas.", "Enviando a Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
                // Lógica para enviar a la API de Gemini (requiere un cliente de API)
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
}
