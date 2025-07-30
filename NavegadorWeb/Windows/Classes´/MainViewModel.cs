using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Web.WebView2.Wpf;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core; // Necessary for CoreWebView2 types
using System.Windows; // Necessary for Visibility, MessageBox, Application
using NavegadorWeb.Services; // For HistoryManager, SettingsManager, etc.
using NavegadorWeb.Windows; // ¡MUY IMPORTANTE! Asegúrate de que esta línea esté presente.
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Timers;
using Microsoft.Win32; // For SaveFileDialog
using System.Speech.Synthesis; // Necessary for SpeechSynthesizer
using System.Diagnostics; // Necessary for Debug.WriteLine

namespace NavegadorWeb.Classes
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // UI Bindable Properties
        public TabGroupManager TabGroupManager { get; set; }

        public TabItemData? SelectedTabItem
        {
            get => TabGroupManager.SelectedTabGroup?.SelectedTabItem;
            set
            {
                if (TabGroupManager.SelectedTabGroup?.SelectedTabItem != value)
                {
                    if (TabGroupManager.SelectedTabGroup != null)
                    {
                        TabGroupManager.SelectedTabGroup.SelectedTabItem = value;
                    }
                    OnPropertyChanged(nameof(SelectedTabItem));
                    OnPropertyChanged(nameof(CurrentUrl)); // Update address bar
                    OnPropertyChanged(nameof(CanGoBack));
                    OnPropertyChanged(nameof(CanGoForward));
                }
            }
        }

        private string _currentUrl = "about:blank";
        public string CurrentUrl
        {
            get => SelectedTabItem?.Url ?? "about:blank";
            set
            {
                // The URL is updated via SelectedTabItem.Url
                // This setter is mainly for the TwoWay binding of the AddressBar
                if (SelectedTabItem != null)
                {
                    Navigate(value);
                }
                else
                {
                    _currentUrl = value; // For the initial case with no tabs
                    OnPropertyChanged(nameof(CurrentUrl));
                }
            }
        }

        public bool CanGoBack => SelectedTabItem?.WebViewInstance.CoreWebView2?.CanGoBack ?? false;
        public bool CanGoForward => SelectedTabItem?.WebViewInstance.CoreWebView2?.CanGoForward ?? false;

        private double _downloadProgress;
        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                if (_downloadProgress != value)
                {
                    _downloadProgress = value;
                    OnPropertyChanged(nameof(DownloadProgress));
                }
            }
        }

        private Visibility _downloadProgressBarVisibility = Visibility.Collapsed;
        public Visibility DownloadProgressBarVisibility
        {
            get => _downloadProgressBarVisibility;
            set
            {
                if (_downloadProgressBarVisibility != value)
                {
                    _downloadProgressBarVisibility = value;
                    OnPropertyChanged(nameof(DownloadProgressBarVisibility));
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
                    if (!value)
                    {
                        SelectedTabItem?.WebViewInstance.CoreWebView2?.StopFindInPage(CoreWebView2StopFindInPageKind.ClearSelection);
                        FindSearchText = string.Empty;
                        FindResultsText = string.Empty;
                    }
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
                    FindInPage(_findSearchText, false); // Perform search when text changes
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

        // Commands
        public ICommand NavigateCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoForwardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand NewTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand SaveSessionCommand { get; }
        public ICommand AddNewTabGroupCommand { get; }
        public ICommand RemoveSelectedTabGroupCommand { get; }
        public ICommand DuplicateTabCommand { get; }
        public ICommand ToggleReaderModeCommand { get; }
        public ICommand CaptureScreenshotCommand { get; }
        public ICommand SuspendTabCommand { get; }
        public ICommand RestoreTabCommand { get; }
        public ICommand ReadAloudCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand OpenHistoryCommand { get; }
        public ICommand OpenBookmarksCommand { get; }
        public ICommand OpenPasswordManagerCommand { get; }
        public ICommand ExtractDataCommand { get; }
        public ICommand OpenExtensionsCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ShowAISelectionCommand { get; }
        public ICommand ToggleFindBarCommand { get; }
        public ICommand FindNextCommand { get; }
        public ICommand FindPreviousCommand { get; }
        public ICommand CloseFindBarCommand { get; }


        private System.Timers.Timer _suspensionTimer;
        private SpeechSynthesizer _speechSynthesizer;

        // Constructor
        public MainViewModel()
        {
            TabGroupManager = new TabGroupManager();
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();

            InitializeCommands();
            LoadSession();

            // Initialize suspension timer
            _suspensionTimer = new System.Timers.Timer(SettingsManager.SuspensionTimeMinutes * 60 * 1000); // Convert minutes to milliseconds
            _suspensionTimer.Elapsed += SuspensionTimer_Elapsed;
            _suspensionTimer.AutoReset = true; // Timer resets automatically
            _suspensionTimer.Start();
        }

        public void InitializeBrowser()
        {
            if (!TabGroupManager.TabGroups.Any() || !TabGroupManager.SelectedTabGroup.TabsInGroup.Any())
            {
                AddNewTab(SettingsManager.DefaultHomePage);
            }
        }

        private void InitializeCommands()
        {
            NavigateCommand = new RelayCommand(url => Navigate(url?.ToString()));
            GoBackCommand = new RelayCommand(_ => GoBack(), _ => CanGoBack);
            GoForwardCommand = new RelayCommand(_ => GoForward(), _ => CanGoForward);
            RefreshCommand = new RelayCommand(_ => Refresh());
            NewTabCommand = new RelayCommand(_ => AddNewTab(SettingsManager.DefaultHomePage));
            CloseTabCommand = new RelayCommand(tab => CloseTab(tab as TabItemData));
            SaveSessionCommand = new RelayCommand(_ => SaveSession());
            AddNewTabGroupCommand = new RelayCommand(_ => AddNewTabGroup());
            RemoveSelectedTabGroupCommand = new RelayCommand(group => RemoveTabGroup(group as TabGroup), group => TabGroupManager.TabGroups.Count > 1);
            DuplicateTabCommand = new RelayCommand(tab => DuplicateTab(tab as TabItemData));
            ToggleReaderModeCommand = new RelayCommand(_ => ToggleReaderMode());
            CaptureScreenshotCommand = new RelayCommand(async _ => await CaptureScreenshot());
            SuspendTabCommand = new RelayCommand(tab => SuspendTab(tab as TabItemData));
            RestoreTabCommand = new RelayCommand(tab => RestoreTab(tab as TabItemData));
            ReadAloudCommand = new RelayCommand(async _ => await ReadAloud());
            ClearHistoryCommand = new RelayCommand(_ => HistoryManager.ClearHistory());
            OpenHistoryCommand = new RelayCommand(_ => OpenHistoryWindow());
            OpenBookmarksCommand = new RelayCommand(_ => OpenBookmarksWindow());
            OpenPasswordManagerCommand = new RelayCommand(_ => OpenPasswordManagerWindow());
            ExtractDataCommand = new RelayCommand(async _ => await ExtractDataForAI());
            OpenExtensionsCommand = new RelayCommand(_ => OpenExtensionsWindow());
            OpenSettingsCommand = new RelayCommand(_ => OpenSettingsWindow());
            ShowAISelectionCommand = new RelayCommand(_ => ShowAISelection());
            ToggleFindBarCommand = new RelayCommand(_ => IsFindBarVisible = !IsFindBarVisible);
            FindNextCommand = new RelayCommand(_ => FindInPage(FindSearchText, true, false)); // Find next
            FindPreviousCommand = new RelayCommand(_ => FindInPage(FindSearchText, false, true)); // Find previous
            CloseFindBarCommand = new RelayCommand(_ => IsFindBarVisible = false);
        }

        private void SuspensionTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var group in TabGroupManager.TabGroups)
                {
                    foreach (var tab in group.TabsInGroup)
                    {
                        if (!tab.IsSuspended && (DateTime.Now - tab.LastActivity).TotalMinutes >= SettingsManager.SuspensionTimeMinutes)
                        {
                            SuspendTab(tab);
                        }
                    }
                }
            });
        }

        private void AddNewTab(string url)
        {
            var webView = new WebView2();
            var newTab = new TabItemData(webView);

            // Subscribe to CoreWebView2 events
            webView.CoreWebView2InitializationCompleted += (s, e) => OnWebViewInitialized(newTab, url);
            webView.CoreWebView2.NavigationCompleted += (s, e) => OnNavigationCompleted(newTab, e);
            webView.CoreWebView2.SourceChanged += (s, e) => OnSourceChanged(newTab, e);
            webView.CoreWebView2.DocumentTitleChanged += (s, e) => OnDocumentTitleChanged(newTab, e);
            webView.CoreWebView2.HistoryChanged += (s, e) => OnHistoryChanged(newTab, e);
            webView.CoreWebView2.FaviconChanged += async (s, e) => await OnFaviconChanged(newTab);
            webView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            webView.CoreWebView2.WebMessageReceived += (s, e) => OnWebMessageReceived(newTab, e);
            webView.CoreWebView2.NewWindowRequested += (s, e) => OnNewWindowRequested(newTab, e);

            TabGroupManager.SelectedTabGroup?.AddTab(newTab);
            SelectedTabItem = newTab;
            OnPropertyChanged(nameof(CurrentUrl));
        }

        private async void OnWebViewInitialized(TabItemData tab, string url)
        {
            try
            {
                await tab.WebViewInstance.EnsureCoreWebView2Async(null);
                tab.WebViewInstance.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                tab.WebViewInstance.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                tab.WebViewInstance.CoreWebView2.Settings.AreDevToolsEnabled = true;
                tab.WebViewInstance.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
                tab.WebViewInstance.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true; // Corrected: Removed extra .CoreWebView2

                if (!string.IsNullOrEmpty(url) && url != "about:blank")
                {
                    tab.WebViewInstance.CoreWebView2.Navigate(url);
                }
                tab.LastActivity = DateTime.Now;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error during WebView2 initialization: {ex.Message}", "WebView2 Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnNavigationStarting(TabItemData tab, CoreWebView2NavigationStartingEventArgs e)
        {
            tab.IsLoading = true;
            tab.Url = e.Uri;
            HistoryManager.AddHistoryEntry(tab.Title, e.Uri);
            tab.LastActivity = DateTime.Now;
            OnPropertyChanged(nameof(CurrentUrl));
        }

        private void OnNavigationCompleted(TabItemData tab, CoreWebView2NavigationCompletedEventArgs e)
        {
            tab.IsLoading = false;
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            tab.LastActivity = DateTime.Now;
        }

        private void OnSourceChanged(TabItemData tab, CoreWebView2SourceChangedEventArgs e)
        {
            tab.Url = tab.WebViewInstance.Source.OriginalString;
            OnPropertyChanged(nameof(CurrentUrl));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            tab.LastActivity = DateTime.Now;
        }

        private void OnDocumentTitleChanged(TabItemData tab, object e)
        {
            tab.Title = tab.WebViewInstance.CoreWebView2.DocumentTitle;
            tab.LastActivity = DateTime.Now;
        }

        private async Task OnFaviconChanged(TabItemData tab)
        {
            if (tab.WebViewInstance.CoreWebView2 != null)
            {
                try
                {
                    using (var stream = await tab.WebViewInstance.CoreWebView2.GetFaviconStreamAsync())
                    {
                        if (stream != null && stream.Length > 0)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                BitmapImage favicon = new BitmapImage();
                                favicon.BeginInit();
                                favicon.StreamSource = stream;
                                favicon.CacheOption = BitmapCacheOption.OnLoad;
                                favicon.EndInit();
                                favicon.Freeze();
                                tab.Favicon = favicon;
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting favicon: {ex.Message}");
                }
            }
        }

        private void OnHistoryChanged(TabItemData tab, object e)
        {
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            tab.LastActivity = DateTime.Now;
        }

        private void OnWebMessageReceived(TabItemData tab, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // Logic for web messages (JavaScript to C#)
        }

        private void OnNewWindowRequested(TabItemData tab, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Prevent new windows from opening and open them in a new tab instead
            e.Handled = true;
            AddNewTab(e.Uri);
        }

        private async void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => DownloadProgressBarVisibility = Visibility.Visible);

            e.Cancel = true; // Cancel download by default to handle it manually

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = e.ResultFilePath, // Default file name
                Filter = "All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                e.ResultFilePath = saveFileDialog.FileName; // Set the destination path chosen by the user
                e.Cancel = false; // Allow WebView2 to continue with the download
                e.Handled = true; // Indicate that we have handled the event

                e.DownloadOperation.ProgressChanged += (downloadSender, downloadArgs) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DownloadProgress = (double)e.DownloadOperation.BytesReceived / e.DownloadOperation.TotalBytesToReceive * 100;
                    });
                };

                e.DownloadOperation.StateChanged += (downloadSender, downloadArgs) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (e.DownloadOperation.State == CoreWebView2DownloadState.Completed)
                        {
                            MessageBox.Show($"Download complete: {e.ResultFilePath}", "Download", MessageBoxButton.OK, MessageBoxImage.Information);
                            DownloadProgressBarVisibility = Visibility.Collapsed;
                            DownloadProgress = 0;
                        }
                        else if (e.DownloadOperation.State == CoreWebView2DownloadState.Interrupted)
                        {
                            MessageBox.Show($"Download interrupted: {e.ResultFilePath}\nReason: {e.DownloadOperation.InterruptReason}", "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            DownloadProgressBarVisibility = Visibility.Collapsed;
                            DownloadProgress = 0;
                        }
                    });
                };
            }
            else
            {
                e.Cancel = true;
                e.Handled = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DownloadProgressBarVisibility = Visibility.Collapsed;
                    DownloadProgress = 0;
                });
            }
        }

        private void Navigate(string? url)
        {
            if (SelectedTabItem != null && !string.IsNullOrWhiteSpace(url))
            {
                string formattedUrl = url;
                if (!url.Contains("://") && !url.StartsWith("about:"))
                {
                    // Use the default search engine if it's not a full URL
                    formattedUrl = SettingsManager.DefaultSearchEngineUrl + Uri.EscapeDataString(url);
                }
                SelectedTabItem.WebViewInstance.CoreWebView2.Navigate(formattedUrl);
                SelectedTabItem.Url = formattedUrl; // Update the URL property immediately
                SelectedTabItem.LastActivity = DateTime.Now; // Mark activity
                HistoryManager.AddHistoryEntry(SelectedTabItem.Title, formattedUrl); // Save to history
                OnPropertyChanged(nameof(CurrentUrl));
            }
        }

        private void GoBack()
        {
            SelectedTabItem?.WebViewInstance.CoreWebView2?.GoBack();
            SelectedTabItem.LastActivity = DateTime.Now; // Mark activity
        }

        private void GoForward()
        {
            SelectedTabItem?.WebViewInstance.CoreWebView2?.GoForward();
            SelectedTabItem.LastActivity = DateTime.Now; // Mark activity
        }

        private void Refresh()
        {
            SelectedTabItem?.WebViewInstance.CoreWebView2?.Reload();
            SelectedTabItem.LastActivity = DateTime.Now; // Mark activity
        }

        private void CloseTab(TabItemData? tab)
        {
            if (tab != null && TabGroupManager.SelectedTabGroup != null)
            {
                TabGroupManager.SelectedTabGroup.RemoveTab(tab);
                tab.WebViewInstance.Dispose(); // Release WebView2 resources

                if (!TabGroupManager.SelectedTabGroup.TabsInGroup.Any())
                {
                    // If no tabs left in the group, remove the group or add a new tab
                    if (TabGroupManager.TabGroups.Count > 1)
                    {
                        TabGroupManager.RemoveTabGroup(TabGroupManager.SelectedTabGroup);
                    }
                    else // If it's the last group, add a new tab
                    {
                        AddNewTab(SettingsManager.DefaultHomePage);
                    }
                }
                OnPropertyChanged(nameof(CurrentUrl));
            }
        }

        private void AddNewTabGroup()
        {
            var newGroup = new TabGroup($"Grupo {TabGroupManager.TabGroups.Count + 1}");
            TabGroupManager.AddTabGroup(newGroup);
            TabGroupManager.SelectedTabGroup = newGroup; // Select the new group
            AddNewTab(SettingsManager.DefaultHomePage); // Add a default tab to the new group
        }

        private void RemoveTabGroup(TabGroup? group)
        {
            if (group != null && TabGroupManager.TabGroups.Count > 1)
            {
                // Dispose of all WebView2 instances in the group before removing it
                foreach (var tab in group.TabsInGroup.ToList()) // ToList to avoid modifying the collection while iterating
                {
                    tab.WebViewInstance.Dispose();
                }
                TabGroupManager.RemoveTabGroup(group);

                if (!TabGroupManager.TabGroups.Any())
                {
                    // If no groups left, create a default one and a tab
                    AddNewTabGroup();
                    AddNewTab(SettingsManager.DefaultHomePage);
                }
            }
        }

        private void DuplicateTab(TabItemData? tab)
        {
            if (tab != null)
            {
                AddNewTab(tab.Url);
            }
        }

        private void ToggleReaderMode()
        {
            if (SelectedTabItem != null)
            {
                SelectedTabItem.IsReaderMode = !SelectedTabItem.IsReaderMode;
                // Add logic here to apply CSS or manipulate the DOM for a real reader mode experience.
                // For example:
                // if (SelectedTabItem.IsReaderMode)
                // {
                //     await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.body.style.fontFamily='serif'; document.body.style.lineHeight='1.6';");
                // }
                // else
                // {
                //     await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.body.style.fontFamily=''; document.body.style.lineHeight='';");
                // }
            }
        }

        private async Task CaptureScreenshot()
        {
            if (SelectedTabItem?.WebViewInstance.CoreWebView2 != null)
            {
                try
                {
                    // Capture the WebView2 image
                    using (var stream = new MemoryStream())
                    {
                        await SelectedTabItem.WebViewInstance.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
                        stream.Position = 0; // Reset stream position
                        BitmapImage screenshot = new BitmapImage();
                        screenshot.BeginInit();
                        screenshot.CacheOption = BitmapCacheOption.OnLoad; // Load the image completely to free the stream
                        screenshot.StreamSource = stream;
                        screenshot.EndInit();
                        screenshot.Freeze(); // Freeze the BitmapImage so it can be accessed from other threads if needed

                        // Convert to Base64 and store in CapturedData
                        SelectedTabItem.CapturedData.ScreenshotBase64 = ConvertBitmapImageToBase64(screenshot);

                        MessageBox.Show("Screenshot saved to page data object.", "Screenshot Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error capturing screen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SuspendTab(TabItemData? tab)
        {
            if (tab != null && !tab.IsSuspended)
            {
                tab.LastSuspendedUrl = tab.Url;
                tab.IsSuspended = true;
                tab.WebViewInstance.CoreWebView2.Navigate("about:blank"); // Navigate to a blank page to free resources
                tab.WebViewInstance.Visibility = Visibility.Collapsed; // Hide the WebView2
                // You can change the Title and Favicon to indicate "suspended"
                tab.Title = $"Suspended: {tab.Title}";
                tab.Favicon = null; // Or a "suspended" icon
            }
        }

        private void RestoreTab(TabItemData? tab)
        {
            if (tab != null && tab.IsSuspended && !string.IsNullOrEmpty(tab.LastSuspendedUrl))
            {
                tab.IsSuspended = false;
                tab.WebViewInstance.Visibility = Visibility.Visible; // Show the WebView2
                tab.WebViewInstance.CoreWebView2.Navigate(tab.LastSuspendedUrl); // Reload the original URL
                tab.LastActivity = DateTime.Now; // Mark activity upon restoring
            }
        }

        private async Task ReadAloud()
        {
            if (SelectedTabItem?.WebViewInstance.CoreWebView2 != null)
            {
                try
                {
                    // Execute JavaScript to get the page body text
                    // This is a simplification; for cleaner text, you would need an HTML parser
                    string pageText = await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.body.innerText;");
                    pageText = System.Text.Json.JsonSerializer.Deserialize<string>(pageText) ?? string.Empty; // Deserialize the JSON string

                    if (!string.IsNullOrEmpty(pageText))
                    {
                        // Limit text to avoid overwhelming the speech synthesizer (optional)
                        if (pageText.Length > 2000)
                        {
                            pageText = pageText.Substring(0, 2000) + "... (truncated)";
                        }

                        _speechSynthesizer.SpeakAsync(pageText); // Read text asynchronously

                        MessageBox.Show("Reading page content aloud...", "Read Aloud", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("No text found to read on this page.", "Read Aloud", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading aloud: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenHistoryWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                HistoryWindow historyWindow = new HistoryWindow();
                bool? result = historyWindow.ShowDialog();

                if (result == true && !string.IsNullOrEmpty(historyWindow.SelectedUrl))
                {
                    Navigate(historyWindow.SelectedUrl);
                }
            });
        }

        private void OpenBookmarksWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                BookmarksWindow bookmarksWindow = new BookmarksWindow();
                bookmarksWindow.ShowDialog();
            });
        }

        private void OpenPasswordManagerWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PasswordManagerWindow passwordManagerWindow = new PasswordManagerWindow();
                passwordManagerWindow.ShowDialog();
            });
        }

        private async Task ExtractDataForAI()
        {
            if (SelectedTabItem?.WebViewInstance.CoreWebView2 != null)
            {
                var currentTab = SelectedTabItem;

                var html = await currentTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML;");
                var text = await currentTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.body.innerText;");

                currentTab.CapturedData.Url = currentTab.Url;
                currentTab.CapturedData.Title = currentTab.Title;
                currentTab.CapturedData.ExtractedText = JsonSerializer.Deserialize<string>(text) ?? string.Empty;

                try
                {
                    using (var screenshotStream = new MemoryStream())
                    {
                        await currentTab.WebViewInstance.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, screenshotStream);
                        screenshotStream.Position = 0;
                        BitmapImage screenshotImage = new BitmapImage();
                        screenshotImage.BeginInit();
                        screenshotImage.CacheOption = BitmapCacheOption.OnLoad;
                        screenshotImage.StreamSource = screenshotStream;
                        screenshotImage.EndInit();
                        screenshotImage.Freeze();
                        currentTab.CapturedData.ScreenshotBase64 = ConvertBitmapImageToBase64(screenshotImage);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error capturing preview for AI: {ex.Message}");
                    currentTab.CapturedData.ScreenshotBase64 = string.Empty;
                }

                currentTab.CapturedData.FaviconBase64 = string.Empty; // Placeholder, getting favicon is more complex

                GeminiDataViewerWindow geminiViewer = new GeminiDataViewerWindow(new ObservableCollection<CapturedPageData> { currentTab.CapturedData });
                geminiViewer.ShowDialog();
            }
        }

        private void OpenExtensionsWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ExtensionsWindow extensionsWindow = new ExtensionsWindow(); // Pass your ExtensionManager if needed
                extensionsWindow.ShowDialog();
            });
        }

        private void OpenSettingsWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SettingsWindow settingsWindow = new SettingsWindow(); // Pass your MainViewModel or SettingsViewModel
                settingsWindow.ShowDialog();
            });
        }

        private void ShowAISelection()
        {
            MessageBox.Show($"Current AI Assistant: {SettingsManager.DefaultAIModel}\n\n" +
                            "AI selection will be handled in the Settings window.", "AI Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void FindInPage(string searchText, bool findNext = false, bool findPrevious = false)
        {
            if (SelectedTabItem?.WebViewInstance.CoreWebView2 != null)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    SelectedTabItem.WebViewInstance.CoreWebView2.StopFindInPage(CoreWebView2StopFindInPageKind.ClearSelection);
                    FindResultsText = string.Empty;
                    return;
                }

                CoreWebView2FindParameters parameters = SelectedTabItem.WebViewInstance.CoreWebView2.CreateFindParameters();
                parameters.FindNext = findNext;
                parameters.SearchPrevious = findPrevious;
                parameters.MatchCase = false; // You can make this configurable
                parameters.WholeWord = false; // You can make this configurable

                CoreWebView2FindResult result = await SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(searchText, parameters);
                FindResultsText = $"{result.Matches} results";
            }
        }

        // Helper methods for image conversion
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
                Console.WriteLine($"Error converting BitmapImage to Base64: {ex.Message}");
                return string.Empty;
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Internal classes for session serialization
        private class SessionState
        {
            public string? SelectedTabGroupId { get; set; }
            public List<TabGroupState> TabGroupStates { get; set; } = new List<TabGroupState>();
        }
    }
}
