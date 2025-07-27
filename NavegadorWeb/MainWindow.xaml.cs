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
using System.ComponentModel;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Timers;

// Asegúrate de que estas directivas 'using' estén presentes para las clases auxiliares
using NavegadorWeb.Classes; // Para TabItemData, TabGroup, TabGroupManager, CapturedPageData, RelayCommand, TabGroupState, ToolbarPosition
using NavegadorWeb.Extensions; // Para CustomExtension, ExtensionManager


namespace NavegadorWeb
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _defaultHomePage = "https://www.google.com";
        private const string HomePageSettingKey = "DefaultHomePage";
        private const string AdBlockerSettingKey = "AdBlockerEnabled";
        private const string DefaultSearchEngineSettingKey = "DefaultSearchEngine";
        private const string TabSuspensionSettingKey = "TabSuspensionEnabled";
        private const string RestoreSessionSettingKey = "RestoreSessionOnStartup";
        private const string LastSessionUrlsSettingKey = "LastSessionUrls";
        private const string LastSessionTabGroupsSettingKey = "LastSessionTabGroups";
        private const string LastSelectedTabGroupSettingKey = "LastSelectedTabGroup";

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
                    ApplyTheme();
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
        public ExtensionManager ExtensionManager { get; private set; }

        private SpeechSynthesizer? _speechSynthesizer;
        private bool _isReadingAloud = false;

        public ObservableCollection<CapturedPageData> CapturedPagesForGemini { get; set; } = new ObservableCollection<CapturedPageData>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private const double MIN_WIDTH = 800;
        private const double MIN_HEIGHT = 600;

        private System.Timers.Timer _tabSuspensionTimer;
        private TimeSpan _tabSuspensionDelay = TimeSpan.FromMinutes(5);
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
            InitializeComponent(); // Este método es generado automáticamente por WPF
            this.DataContext = this;

            TabGroupManager = new TabGroupManager();
            ExtensionManager = new ExtensionManager();

            LoadSettings();
            ApplyAdBlockerSettings();
            ApplyTheme();

            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();

            _tabSuspensionTimer = new System.Timers.Timer(_tabSuspensionDelay.TotalMilliseconds);
            _tabSuspensionTimer.Elapsed += TabSuspensionTimer_Elapsed;
            _tabSuspensionTimer.AutoReset = true;
            if (IsTabSuspensionEnabled)
            {
                StartTabSuspensionTimer();
            }

            this.PreviewMouseMove += MainWindow_UserActivity;
            this.PreviewKeyDown += MainWindow_UserActivity;
        }

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
            if (IsTabSuspensionEnabled)
            {
                _tabSuspensionTimer.Stop();
                _tabSuspensionTimer.Start();
            }
        }

        private async void TabSuspensionTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                foreach (var group in TabGroupManager.TabGroups)
                {
                    foreach (var tab in group.TabsInGroup)
                    {
                        if (tab != SelectedTabItem && tab.WebViewInstance != null && tab.WebViewInstance.CoreWebView2 != null)
                        {
                            tab.IsSuspended = true;
                            tab.LastSuspendedUrl = tab.WebViewInstance.Source.ToString();
                            tab.WebViewInstance.CoreWebView2.Stop();
                            tab.WebViewInstance.Source = new Uri("about:blank");
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
                AddNewTab(_defaultHomePage);
            }
        }

        private void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[HomePageSettingKey].Value = _defaultHomePage;
            config.AppSettings.Settings[AdBlockerSettingKey].Value = IsAdBlockerEnabled.ToString();
            config.AppSettings.Settings[TabSuspensionSettingKey].Value = IsTabSuspensionEnabled.ToString();

            if (bool.Parse(ConfigurationManager.AppSettings[RestoreSessionSettingKey] ?? "false"))
            {
                SaveCurrentSession();
            }
            else
            {
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

            var allUrls = TabGroupManager.TabGroups
                                        .SelectMany(g => g.TabsInGroup)
                                        .Select(tab => tab.WebViewInstance?.Source?.ToString())
                                        .Where(url => !string.IsNullOrEmpty(url) && url != "about:blank")
                                        .ToList();
            config.AppSettings.Settings[LastSessionUrlsSettingKey].Value = JsonSerializer.Serialize(allUrls);

            var groupStates = TabGroupManager.TabGroups.Select(g => new TabGroupState
            {
                GroupId = g.GroupId,
                GroupName = g.GroupName,
                TabUrls = g.TabsInGroup.Select(t => t.WebViewInstance?.Source?.ToString()).Where(url => !string.IsNullOrEmpty(url) && url != "about:blank").ToList(),
                SelectedTabUrl = g.SelectedTabItem?.WebViewInstance?.Source?.ToString()
            }).ToList();
            config.AppSettings.Settings[LastSessionTabGroupsSettingKey].Value = JsonSerializer.Serialize(groupStates);

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
                        TabGroupManager.TabGroups.Clear();

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

                            if (!string.IsNullOrEmpty(groupState.SelectedTabUrl))
                            {
                                var selectedTab = newGroup.TabsInGroup.FirstOrDefault(t => t.WebViewInstance?.Source?.ToString() == groupState.SelectedTabUrl);
                                if (selectedTab != null)
                                {
                                    newGroup.SelectedTabItem = selectedTab;
                                }
                            }
                        }

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
                            TabGroupManager.SelectedTabGroup = TabGroupManager.GetDefaultGroup();
                            BrowserTabs.ItemsSource = TabGroupManager.GetDefaultGroup().TabsInGroup;
                            SelectedTabItem = TabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault();
                        }
                    }
                    else
                    {
                        AddNewTab(_defaultHomePage);
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
                AddNewTab(_defaultHomePage);
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
            Resources.Remove("BrowserBackgroundColor");
            Resources.Remove("BrowserBackgroundBrush");
            Resources.Remove("BrowserForegroundColor");
            Resources.Remove("BrowserForegroundBrush");

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
            MainBorder.Background = (SolidColorBrush)Resources["BrowserBackgroundBrush"];
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow != null &&
                (Application.Current.MainWindow.WindowState == WindowState.Maximized ||
                 (Application.Current.MainWindow.ActualWidth == SystemParameters.WorkArea.Width &&
                  Application.Current.MainWindow.ActualHeight == SystemParameters.WorkArea.Height)))
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
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

            BrowserTabs.ItemsSource = TabGroupManager.GetDefaultGroup().TabsInGroup;
            SelectedTabItem = TabGroupManager.GetDefaultGroup().TabsInGroup.FirstOrDefault();

            BrowserTabs.ItemTemplate = (DataTemplate)this.Resources["TabHeaderTemplate"];

            foreach (var group in TabGroupManager.TabGroups)
            {
                foreach (var tab in group.TabsInGroup)
                {
                    tab.WebViewInstance.SourceChanged += WebView_SourceChanged;
                    tab.WebViewInstance.NavigationCompleted += WebView_NavigationCompleted;
                    tab.WebViewInstance.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
                    tab.WebViewInstance.WebMessageReceived += WebView_WebMessageReceived;
                    tab.WebViewInstance.DownloadStarting += WebView_DownloadStarting;
                }
            }

            ExtensionManager.LoadExtensions();
            ExtensionsMenuItem.DataContext = ExtensionManager;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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

            SaveSettings();

            _speechSynthesizer?.Dispose();

            _tabSuspensionTimer?.Stop();
            _tabSuspensionTimer?.Dispose();

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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
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
                CapturedData = new CapturedPageData { Url = url }
            };

            webView.Source = new Uri(url);
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.SourceChanged += WebView_SourceChanged;
            webView.WebMessageReceived += WebView_WebMessageReceived;
            webView.DownloadStarting += WebView_DownloadStarting;
            webView.ContextMenuOpening += WebView_ContextMenuOpening;

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
            AddNewTab(_defaultHomePage);
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TabItemData tabToClose)
            {
                var currentGroup = TabGroupManager.GetGroupByTab(tabToClose);
                if (currentGroup != null)
                {
                    currentGroup.RemoveTab(tabToClose);
                    tabToClose.WebViewInstance?.Dispose();

                    if (currentGroup.TabsInGroup.Count == 0 && TabGroupManager.TabGroups.Count > 1)
                    {
                        TabGroupManager.RemoveGroup(currentGroup);
                    }

                    if (TabGroupManager.TabGroups.All(g => g.TabsInGroup.Count == 0))
                    {
                        AddNewTab(_defaultHomePage);
                    }
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
            e.CanExecute = true;
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
                    if (url.Contains("."))
                    {
                        fullUrl = "https://" + url;
                    }
                    else
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
                    AddressBar.Text = tab.Url;

                    UpdateTabTitleAndFavicon(tab, webView);

                    if (tab.IsReaderMode)
                    {
                        tab.IsReaderMode = false;
                    }

                    var textExtractionExtension = ExtensionManager.Extensions.FirstOrDefault(ext => ext.Name == "Text Extractor");
                    if (textExtractionExtension != null && textExtractionExtension.IsEnabled)
                    {
                        string scriptContent = textExtractionExtension.LoadScriptContent();
                        if (!string.IsNullOrEmpty(scriptContent))
                        {
                            webView.CoreWebView2.ExecuteScriptAsync(scriptContent);
                        }
                    }

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
                }
            }
        }

        private async void UpdateTabTitleAndFavicon(TabItemData tab, WebView2 webView)
        {
            if (webView.CoreWebView2 != null)
            {
                try
                {
                    string title = await webView.CoreWebView2.ExecuteScriptAsync("document.title");
                    tab.Title = title.Replace("\"", "");

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
                        if (!faviconUrl.Contains("://"))
                        {
                            Uri baseUri = new Uri(webView.Source.ToString());
                            Uri absoluteUri = new Uri(baseUri, faviconUrl);
                            faviconUrl = absoluteUri.ToString();
                        }

                        try
                        {
                            using (var httpClient = new System.Net.Http.HttpClient())
                            {
                                byte[] faviconBytes = await httpClient.GetByteArrayAsync(faviconUrl);
                                tab.Favicon = new BitmapImage();
                                tab.Favicon.BeginInit();
                                tab.Favicon.StreamSource = new MemoryStream(faviconBytes);
                                tab.Favicon.EndInit();

                                tab.CapturedData.FaviconBase64 = Convert.ToBase64String(faviconBytes);
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
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                if (IsAdBlockerEnabled)
                {
                    webView.CoreWebView2.SetWebResourceContextFilter(
                        CoreWebView2WebResourceContext.Image, CoreWebView2WebResourceContext.Script,
                        CoreWebView2WebResourceContext.Stylesheet, CoreWebView2WebResourceContext.Media,
                        CoreWebView2WebResourceContext.Font);
                }

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
                UpdateNavigationButtons();
            }
            else
            {
                AddressBar.Text = "";
                UpdateNavigationButtons();
            }
        }

        private void UpdateBrowserControls()
        {
            IsFindBarVisible = false;
            FindTextBox.Text = "";
            FindResultsTextBlock.Text = "";
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.StopFindInPage();

            UpdateNavigationButtons();
        }

        private void UpdateNavigationButtons()
        {
            GoBackButton.IsEnabled = SelectedTabItem?.WebViewInstance?.CanGoBack ?? false;
            GoForwardButton.IsEnabled = SelectedTabItem?.WebViewInstance?.CanGoForward ?? false;
        }

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
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.StopFindInPage();
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
                        false,
                        (sender, args) =>
                        {
                            FindResultsTextBlock.Text = $"{args.ActiveMatch}/{args.Matches}";
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
                    false,
                    (sender, args) =>
                    {
                        FindResultsTextBlock.Text = $"{args.ActiveMatch}/{args.Matches}";
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
                    false,
                    (sender, args) =>
                    {
                        FindResultsTextBlock.Text = $"{args.ActiveMatch}/{args.Matches}";
                    }
                );
            }
            else
            {
                // Manejar el caso donde el texto de búsqueda está vacío o no hay WebView2
                FindResultsTextBlock.Text = "";
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que HistoryWindow exista en tu proyecto y sea accesible
            var historyWindow = new HistoryWindow();
            historyWindow.ShowDialog();
        }

        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que BookmarksWindow exista en tu proyecto y sea accesible
            var bookmarksWindow = new BookmarksWindow();
            bookmarksWindow.ShowDialog();
        }

        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que PasswordManagerWindow exista en tu proyecto y sea accesible
            var passwordManagerWindow = new PasswordManagerWindow();
            passwordManagerWindow.ShowDialog();
        }

        private void DataExtractionButton_Click(object sender, RoutedEventArgs e)
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

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que SettingsWindow exista en tu proyecto y sea accesible
            var settingsWindow = new SettingsWindow();
            settingsWindow.HomePage = _defaultHomePage;
            settingsWindow.IsAdBlockerEnabled = IsAdBlockerEnabled;
            settingsWindow.IsTabSuspensionEnabled = IsTabSuspensionEnabled;

            if (settingsWindow.ShowDialog() == true)
            {
                _defaultHomePage = settingsWindow.HomePage;
                IsAdBlockerEnabled = settingsWindow.IsAdBlockerEnabled;
                IsTabSuspensionEnabled = settingsWindow.IsTabSuspensionEnabled;
                SaveSettings();
            }
        }

        private void PipButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null && !string.IsNullOrEmpty(SelectedTabItem.Url))
            {
                string videoUrl = SelectedTabItem.Url;

                try
                {
                    // Asegúrate de que PipWindow exista en tu proyecto y sea accesible
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

        private void WebView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Puedes agregar elementos personalizados al menú contextual aquí si es necesario
        }

        private async void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                if (_isReadingAloud)
                {
                    _speechSynthesizer?.SpeakAsyncCancelAll();
                    _isReadingAloud = false;
                    ReadAloudButton.ToolTip = "Leer en voz alta";
                }
                else
                {
                    try
                    {
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
                            _speechSynthesizer?.SpeakAsync(pageText);
                            _isReadingAloud = true;
                            ReadAloudButton.ToolTip = "Detener lectura";
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

        private async void ReaderModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                try
                {
                    if (SelectedTabItem.IsReaderMode)
                    {
                        if (!string.IsNullOrEmpty(SelectedTabItem.Url))
                        {
                            SelectedTabItem.WebViewInstance.Source = new Uri(SelectedTabItem.Url);
                        }
                        SelectedTabItem.IsReaderMode = false;
                        ReaderModeButton.ToolTip = "Modo lector";
                    }
                    else
                    {
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

        private void IncognitoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("El modo incógnito abre una nueva ventana donde la actividad de navegación no se guarda en el historial ni en las cookies después de cerrar la ventana.", "Modo Incógnito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is CustomExtension extension)
            {
                extension.IsEnabled = !extension.IsEnabled;
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
                        await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync(scriptContent);
                    }
                    else
                    {
                        MessageBox.Show($"La extensión '{extension.Name}' ha sido {(extension.IsEnabled ? "activada" : "desactivada")}. Puede que necesite recargar la página para ver los cambios.", "Extensiones", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que ExtensionsWindow exista en tu proyecto y sea accesible
            var extensionsWindow = new ExtensionsWindow(ExtensionManager);
            extensionsWindow.ShowDialog();
        }

        private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView == null) return;

            string message = e.WebMessageAsJson;

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
                                        tab.CapturedData.ExtractedText = extractedText;
                                    }
                                }
                                break;
                            case "passwordDetected":
                                if (doc.RootElement.TryGetProperty("url", out JsonElement urlElement) &&
                                    doc.RootElement.TryGetProperty("username", out JsonElement usernameElement) &&
                                    doc.RootElement.TryGetProperty("password", out JsonElement passwordElement)) // Corrected from TryToGetProperty
                                {
                                    string url = urlElement.GetString() ?? "";
                                    string username = usernameElement.GetString() ?? "";
                                    string password = passwordElement.GetString() ?? "";

                                    var result = MessageBox.Show($"¿Deseas guardar la contraseña para {username} en {url}?", "Guardar Contraseña", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        PasswordManager.SavePassword(url, username, password);
                                        MessageBox.Show("Contraseña guardada exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error al parsear mensaje de WebView2 como JSON: {ex.Message}");
                Console.WriteLine($"Mensaje recibido de WebView2: {message}");
            }
        }

        private void WebView_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            e.Cancel = true;

            string suggestedFileName = Path.GetFileName(e.ResultFilePath);

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.FileName = suggestedFileName;
            saveFileDialog.Title = "Guardar archivo";

            if (saveFileDialog.ShowDialog() == true)
            {
                e.ResultFilePath = saveFileDialog.FileName;
                e.Cancel = false;

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
                e.Cancel = true;
            }
        }

        public ICommand AddTabGroupCommand => new RelayCommand(_ => AddNewTabGroup());

        private void AddNewTabGroup()
        {
            var newGroup = new TabGroup($"Grupo {TabGroupManager.TabGroups.Count + 1}");
            TabGroupManager.AddGroup(newGroup);
            TabGroupManager.SelectedTabGroup = newGroup;
            AddNewTab(_defaultHomePage);
            BrowserTabs.ItemsSource = newGroup.TabsInGroup;
        }

        public ICommand SelectTabGroupCommand => new RelayCommand(parameter =>
        {
            if (parameter is TabGroup selectedGroup)
            {
                TabGroupManager.SelectedTabGroup = selectedGroup;
                BrowserTabs.ItemsSource = selectedGroup.TabsInGroup;
                SelectedTabItem = selectedGroup.TabsInGroup.FirstOrDefault();
            }
        });

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
                        handled = true;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private async void GeminiButton_Click(object sender, RoutedEventArgs e)
        {
            var capturedDataList = new ObservableCollection<CapturedPageData>();

            if (SelectedTabItem != null)
            {
                if (SelectedTabItem.WebViewInstance.CoreWebView2 == null)
                {
                    await SelectedTabItem.WebViewInstance.EnsureCoreWebView2Async(null);
                }

                try
                {
                    string extractedText = await GetPageText(SelectedTabItem.WebViewInstance.CoreWebView2);
                    SelectedTabItem.CapturedData.ExtractedText = extractedText;

                    string screenshotBase64 = await CaptureScreenshotAsync(SelectedTabItem.WebViewInstance);
                    SelectedTabItem.CapturedData.ScreenshotBase64 = screenshotBase64;

                    if (string.IsNullOrEmpty(SelectedTabItem.CapturedData.FaviconBase64) && SelectedTabItem.Favicon != null)
                    {
                        SelectedTabItem.CapturedData.FaviconBase64 = ConvertBitmapImageToBase64(SelectedTabItem.Favicon);
                    }

                    SelectedTabItem.CapturedData.Url = SelectedTabItem.Url;
                    SelectedTabItem.CapturedData.Title = SelectedTabItem.Title;

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

            var geminiViewerWindow = new GeminiDataViewerWindow(capturedDataList);
            geminiViewerWindow.Owner = this;

            if (geminiViewerWindow.ShowDialog() == true)
            {
                string userQuestion = geminiViewerWindow.UserQuestion;

                MessageBox.Show($"Datos enviados a Gemini con la pregunta: '{userQuestion}'", "Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
                IsGeminiModeActive = true;
            }
            else
            {
                MessageBox.Show("Envío a Gemini cancelado.", "Gemini", MessageBoxButton.OK, MessageBoxImage.Information);
                IsGeminiModeActive = false;
            }
        }

        private async Task<string> GetPageText(CoreWebView2 webView)
        {
            if (webView == null) return string.Empty;

            try
            {
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

        private async Task<string> CaptureScreenshotAsync(WebView2 webView)
        {
            if (webView.CoreWebView2 == null)
            {
                await webView.EnsureCoreWebView2Async(null);
            }

            try
            {
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

                    const int MAX_DIMENSION = 8000;
                    contentWidth = Math.Min(contentWidth, MAX_DIMENSION);
                    contentHeight = Math.Min(contentHeight, MAX_DIMENSION);

                    double originalWidth = webView.Width;
                    double originalHeight = webView.Height;

                    Dispatcher.Invoke(() =>
                    {
                        webView.Width = contentWidth;
                        webView.Height = contentHeight;
                        webView.Measure(new Size(contentWidth, contentHeight));
                        webView.Arrange(new Rect(0, 0, contentWidth, contentHeight));
                    });

                    await Task.Delay(50);

                    using (MemoryStream stream = new MemoryStream())
                    {
                        await webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);

                        Dispatcher.Invoke(() =>
                        {
                            webView.Width = originalWidth;
                            webView.Height = originalHeight;
                            webView.Measure(new Size(originalWidth, originalHeight));
                            webView.Arrange(new Rect(0, 0, originalWidth, originalHeight));
                        });

                        byte[] imageBytes = stream.ToArray();
                        return "data:image/png;base64," + Convert.ToBase64String(imageBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al capturar captura de pantalla: {ex.Message}");
                return string.Empty;
            }
        }

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
