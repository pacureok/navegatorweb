using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Web.WebView2.Wpf;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core; // <--- ¡VERIFICADO!
using System.Windows; // <--- ¡VERIFICADO!
using NavegadorWeb.Services; // <--- ¡VERIFICADO! Para HistoryManager, SettingsManager, etc.
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Timers;

namespace NavegadorWeb.Classes
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Propiedades para la UI
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
                    OnPropertyChanged(nameof(CurrentUrl)); // Actualiza la barra de dirección
                    OnPropertyChanged(nameof(CanGoBack));
                    OnPropertyChanged(nameof(CanGoForward));
                }
            }
        }

        public string CurrentUrl
        {
            get => SelectedTabItem?.Url ?? "about:blank";
            set
            {
                if (SelectedTabItem != null && SelectedTabItem.Url != value)
                {
                    SelectedTabItem.Url = value;
                    OnPropertyChanged(nameof(CurrentUrl));
                }
            }
        }

        public bool CanGoBack => SelectedTabItem?.WebViewInstance.CoreWebView2.CanGoBack ?? false;
        public bool CanGoForward => SelectedTabItem?.WebViewInstance.CoreWebView2.CanGoForward ?? false;

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

        // Comandos
        public ICommand NavigateCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoForwardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand NewTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand SaveSessionCommand { get; }
        public ICommand AddTabGroupCommand { get; }
        public ICommand RemoveTabGroupCommand { get; }
        public ICommand DuplicateTabCommand { get; }
        public ICommand ToggleReaderModeCommand { get; }
        public ICommand CaptureScreenshotCommand { get; }
        public ICommand SuspendTabCommand { get; }
        public ICommand RestoreTabCommand { get; }
        public ICommand ReadAloudCommand { get; }
        public ICommand ClearHistoryCommand { get; } // Añadido
        public ICommand OpenHistoryCommand { get; } // Añadido

        private System.Timers.Timer _suspensionTimer;

        // Constructor
        public MainViewModel()
        {
            TabGroupManager = new TabGroupManager();
            LoadSession(); // Carga la sesión al iniciar

            if (!TabGroupManager.TabGroups.Any() || !TabGroupManager.TabGroups.First().TabsInGroup.Any())
            {
                // Si no hay sesión cargada o está vacía, crea una pestaña inicial
                AddNewTab("about:blank");
            }

            NavigateCommand = new RelayCommand(url => Navigate(url?.ToString()));
            GoBackCommand = new RelayCommand(_ => GoBack(), _ => CanGoBack);
            GoForwardCommand = new RelayCommand(_ => GoForward(), _ => CanGoForward);
            RefreshCommand = new RelayCommand(_ => Refresh());
            NewTabCommand = new RelayCommand(_ => AddNewTab("about:blank"));
            CloseTabCommand = new RelayCommand(tab => CloseTab(tab as TabItemData));
            SaveSessionCommand = new RelayCommand(_ => SaveSession());
            AddTabGroupCommand = new RelayCommand(_ => AddNewTabGroup());
            RemoveTabGroupCommand = new RelayCommand(group => RemoveTabGroup(group as TabGroup));
            DuplicateTabCommand = new RelayCommand(tab => DuplicateTab(tab as TabItemData));
            ToggleReaderModeCommand = new RelayCommand(_ => ToggleReaderMode());
            CaptureScreenshotCommand = new RelayCommand(async _ => await CaptureScreenshot());
            SuspendTabCommand = new RelayCommand(tab => SuspendTab(tab as TabItemData));
            RestoreTabCommand = new RelayCommand(tab => RestoreTab(tab as TabItemData));
            ReadAloudCommand = new RelayCommand(async _ => await ReadAloud());
            ClearHistoryCommand = new RelayCommand(_ => HistoryManager.ClearHistory()); // Usando HistoryManager
            OpenHistoryCommand = new RelayCommand(_ => OpenHistoryWindow()); // Usando HistoryWindow


            // Inicializa el temporizador de suspensión
            _suspensionTimer = new System.Timers.Timer(SettingsManager.SuspensionTimeMinutes * 60 * 1000); // Convertir minutos a milisegundos
            _suspensionTimer.Elapsed += SuspensionTimer_Elapsed;
            _suspensionTimer.AutoReset = true; // El temporizador se reinicia automáticamente
            _suspensionTimer.Start();
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
            webView.CoreWebView2InitializationCompleted += (s, e) => OnWebViewInitialized(newTab, url);
            webView.NavigationCompleted += (s, e) => OnNavigationCompleted(newTab, e);
            webView.SourceChanged += (s, e) => OnSourceChanged(newTab, e);
            webView.ContentLoading += (s, e) => OnContentLoading(newTab, e);
            webView.HistoryChanged += (s, e) => OnHistoryChanged(newTab, e);
            webView.DownloadStarting += CoreWebView2_DownloadStarting; // Adjuntar el evento de descarga
            // Adjuntar evento de recarga para favicon (si lo necesitas en el ViewModel)
            webView.CoreWebView2.FaviconChanged += async (s, e) => await OnFaviconChanged(newTab);


            TabGroupManager.SelectedTabGroup?.AddTab(newTab);
            SelectedTabItem = newTab;
            OnPropertyChanged(nameof(CurrentUrl)); // Asegurarse de que la URL se actualice en la UI
        }

        private async void OnWebViewInitialized(TabItemData tab, string url)
        {
            try
            {
                await tab.WebViewInstance.EnsureCoreWebView2Async();
                tab.WebViewInstance.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false; // Deshabilita F5 y Ctrl+R
                tab.WebViewInstance.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                tab.WebViewInstance.CoreWebView2.Settings.AreDevToolsEnabled = true;
                tab.WebViewInstance.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
                tab.WebViewInstance.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;

                tab.WebViewInstance.CoreWebView2.NavigationStarting += (s, e) => OnNavigationStarting(tab, e);
                // Otros eventos importantes pueden adjuntarse aquí si es necesario.

                if (!string.IsNullOrEmpty(url) && url != "about:blank")
                {
                    tab.WebViewInstance.CoreWebView2.Navigate(url);
                }
                tab.LastActivity = DateTime.Now; // Marcar actividad al inicializar

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during WebView2 initialization: {ex.Message}");
                // Puedes mostrar un MessageBox o loguear el error.
            }
        }

        private void OnNavigationStarting(TabItemData tab, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.IsUserInitiated) // Solo registrar si el usuario inició la navegación (clic, escribir URL)
            {
                HistoryManager.AddHistoryEntry(tab.Title, e.Uri); // Usando HistoryManager
            }
        }

        private void Navigate(string? url)
        {
            if (SelectedTabItem != null && !string.IsNullOrWhiteSpace(url))
            {
                string formattedUrl = url;
                if (!url.Contains("://") && !url.StartsWith("about:"))
                {
                    formattedUrl = "https://" + url; // Asume HTTPS si no hay protocolo
                }
                SelectedTabItem.WebViewInstance.CoreWebView2.Navigate(formattedUrl);
                SelectedTabItem.Url = formattedUrl; // Actualiza la propiedad URL inmediatamente
                SelectedTabItem.LastActivity = DateTime.Now; // Marcar actividad
                HistoryManager.AddHistoryEntry(SelectedTabItem.Title, formattedUrl); // Guarda en el historial
                OnPropertyChanged(nameof(CurrentUrl));
            }
        }

        private void GoBack()
        {
            SelectedTabItem?.WebViewInstance.CoreWebView2.GoBack();
            SelectedTabItem.LastActivity = DateTime.Now; // Marcar actividad
        }

        private void GoForward()
        {
            SelectedTabItem?.WebViewInstance.CoreWebView2.GoForward();
            SelectedTabItem.LastActivity = DateTime.Now; // Marcar actividad
        }

        private void Refresh()
        {
            SelectedTabItem?.WebViewInstance.CoreWebView2.Reload();
            SelectedTabItem.LastActivity = DateTime.Now; // Marcar actividad
        }

        private void CloseTab(TabItemData? tab)
        {
            if (tab != null && TabGroupManager.SelectedTabGroup != null)
            {
                TabGroupManager.SelectedTabGroup.RemoveTab(tab);
                tab.WebViewInstance.Dispose(); // Libera los recursos de WebView2

                if (!TabGroupManager.SelectedTabGroup.TabsInGroup.Any())
                {
                    // Si no quedan pestañas en el grupo, añade una nueva por defecto
                    AddNewTab("about:blank");
                }
                OnPropertyChanged(nameof(CurrentUrl));
            }
        }

        private void AddNewTabGroup()
        {
            var newGroup = new TabGroup($"Grupo {TabGroupManager.TabGroups.Count + 1}");
            TabGroupManager.AddTabGroup(newGroup);
            // Si quieres que el nuevo grupo tenga una pestaña inicial, podrías hacer:
            // AddNewTab("about:blank"); // Esto añadiría a la pestaña activa, no necesariamente al nuevo grupo.
            // Para añadir al nuevo grupo y seleccionarlo:
            // var webView = new WebView2();
            // var newTab = new TabItemData(webView);
            // newGroup.AddTab(newTab);
            // TabGroupManager.SelectedTabGroup = newGroup;
            // SelectedTabItem = newTab;
            // OnWebViewInitialized(newTab, "about:blank");
        }

        private void RemoveTabGroup(TabGroup? group)
        {
            if (group != null)
            {
                // Disponer de todos los WebView2 en el grupo antes de removerlo
                foreach (var tab in group.TabsInGroup)
                {
                    tab.WebViewInstance.Dispose();
                }
                TabGroupManager.RemoveTabGroup(group);

                if (!TabGroupManager.TabGroups.Any())
                {
                    // Si no quedan grupos, crea uno por defecto y una pestaña
                    AddNewTabGroup();
                    AddNewTab("about:blank");
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
                // Aquí podrías añadir lógica para aplicar CSS o manipular el DOM
                // para una experiencia de modo lector real.
                // Por ejemplo:
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
                    // Captura la imagen del WebView2
                    using (var stream = new MemoryStream())
                    {
                        await SelectedTabItem.WebViewInstance.CoreWebView2.CapturePreview(CoreWebView2CapturePreviewImageFormat.Png, stream);
                        stream.Position = 0; // Reinicia la posición del stream
                        BitmapImage screenshot = new BitmapImage();
                        screenshot.BeginInit();
                        screenshot.CacheOption = BitmapCacheOption.OnLoad; // Carga la imagen completamente para liberar el stream
                        screenshot.StreamSource = stream;
                        screenshot.EndInit();
                        screenshot.Freeze(); // Congela el BitmapImage para que sea accesible desde otros hilos si es necesario

                        // Convierte a Base64 y almacena en CapturedData
                        SelectedTabItem.CapturedData.ScreenshotBase64 = ConvertBitmapImageToBase64(screenshot);

                        MessageBox.Show("Captura de pantalla guardada en el objeto de datos de la página.", "Captura Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al capturar la pantalla: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SuspendTab(TabItemData? tab)
        {
            if (tab != null && !tab.IsSuspended)
            {
                tab.LastSuspendedUrl = tab.Url;
                tab.IsSuspended = true;
                tab.WebViewInstance.CoreWebView2.Navigate("about:blank"); // Navega a una página en blanco para liberar recursos
                tab.WebViewInstance.Visibility = Visibility.Collapsed; // Oculta el WebView2
                // Puedes cambiar el Title y Favicon a algo que indique "suspendido"
                tab.Title = $"Suspendido: {tab.Title}";
                tab.Favicon = null; // O un icono de "suspendido"
            }
        }

        private void RestoreTab(TabItemData? tab)
        {
            if (tab != null && tab.IsSuspended && !string.IsNullOrEmpty(tab.LastSuspendedUrl))
            {
                tab.IsSuspended = false;
                tab.WebViewInstance.Visibility = Visibility.Visible; // Muestra el WebView2
                tab.WebViewInstance.CoreWebView2.Navigate(tab.LastSuspendedUrl); // Recarga la URL original
                tab.LastActivity = DateTime.Now; // Marcar actividad al restaurar
            }
        }

        private async Task ReadAloud()
        {
            if (SelectedTabItem?.WebViewInstance.CoreWebView2 != null)
            {
                try
                {
                    // Ejecuta JavaScript para obtener el texto del cuerpo de la página
                    // Esto es una simplificación, para un texto más limpio necesitarías un parser HTML
                    string pageText = await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.body.innerText;");
                    pageText = System.Text.Json.JsonSerializer.Deserialize<string>(pageText) ?? string.Empty; // Deserializar la cadena JSON

                    if (!string.IsNullOrEmpty(pageText))
                    {
                        // Limitar el texto para evitar sobrecargar el sintetizador de voz (opcional)
                        if (pageText.Length > 2000)
                        {
                            pageText = pageText.Substring(0, 2000) + "... (truncated)";
                        }

                        SpeechSynthesizer synth = new SpeechSynthesizer();
                        synth.SetOutputToDefaultAudioDevice();
                        synth.SpeakAsync(pageText); // Leer el texto de forma asíncrona

                        MessageBox.Show("Leyendo el contenido de la página...", "Lectura en Voz Alta", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("No se encontró texto para leer en esta página.", "Lectura en Voz Alta", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al leer en voz alta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Métodos auxiliares para la serialización de sesión
        private const string SessionFileName = "browserSession.json";

        private void SaveSession()
        {
            try
            {
                var sessionState = new SessionState
                {
                    SelectedTabGroupId = TabGroupManager.SelectedTabGroup?.GroupId,
                    TabGroupStates = TabGroupManager.TabGroups.Select(g => new TabGroupState
                    {
                        GroupId = g.GroupId,
                        GroupName = g.GroupName,
                        TabUrls = g.TabsInGroup.Select(t => t.Url).ToList(),
                        SelectedTabUrl = g.SelectedTabItem?.Url
                    }).ToList()
                };

                string jsonString = JsonSerializer.Serialize(sessionState, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SessionFileName, jsonString);
                Console.WriteLine("Sesión guardada exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar la sesión: {ex.Message}");
            }
        }

        private void LoadSession()
        {
            try
            {
                if (File.Exists(SessionFileName))
                {
                    string jsonString = File.ReadAllText(SessionFileName);
                    var sessionState = JsonSerializer.Deserialize<SessionState>(jsonString);

                    if (sessionState != null && sessionState.TabGroupStates != null)
                    {
                        TabGroupManager.TabGroups.Clear();
                        foreach (var groupState in sessionState.TabGroupStates)
                        {
                            var group = new TabGroup(groupState.GroupName);
                            group.GroupId = groupState.GroupId; // Asignar el GroupId deserializado
                            TabGroupManager.AddTabGroup(group);

                            foreach (var url in groupState.TabUrls)
                            {
                                if (!string.IsNullOrEmpty(url))
                                {
                                    var webView = new WebView2();
                                    var newTab = new TabItemData(webView);
                                    group.AddTab(newTab);
                                    // Inicializa el WebView2 y navega
                                    newTab.WebViewInstance.CoreWebView2InitializationCompleted += async (s, e) =>
                                    {
                                        await newTab.WebViewInstance.EnsureCoreWebView2Async();
                                        newTab.WebViewInstance.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                                        newTab.WebViewInstance.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                                        newTab.WebView2.CoreWebView2.NavigationStarting += (s, navArgs) => OnNavigationStarting(newTab, navArgs);
                                        newTab.WebViewInstance.CoreWebView2.NavigationCompleted += (s, navArgs) => OnNavigationCompleted(newTab, navArgs);
                                        newTab.WebViewInstance.CoreWebView2.SourceChanged += (s, srcArgs) => OnSourceChanged(newTab, srcArgs);
                                        newTab.WebViewInstance.CoreWebView2.ContentLoading += (s, clArgs) => OnContentLoading(newTab, clArgs);
                                        newTab.WebViewInstance.CoreWebView2.HistoryChanged += (s, hArgs) => OnHistoryChanged(newTab, hArgs);
                                        newTab.WebViewInstance.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
                                        newTab.WebViewInstance.CoreWebView2.FaviconChanged += async (s, e) => await OnFaviconChanged(newTab);

                                        newTab.WebViewInstance.CoreWebView2.Navigate(url);
                                        newTab.Url = url;
                                        newTab.Title = url; // Título temporal hasta que cargue
                                        newTab.LastActivity = DateTime.Now;
                                    };
                                }
                            }
                            if (!string.IsNullOrEmpty(groupState.SelectedTabUrl))
                            {
                                group.SelectedTabItem = group.TabsInGroup.FirstOrDefault(t => t.Url == groupState.SelectedTabUrl);
                            }
                        }

                        if (sessionState.SelectedTabGroupId != null)
                        {
                            TabGroupManager.SelectedTabGroup = TabGroupManager.TabGroups.FirstOrDefault(g => g.GroupId == sessionState.SelectedTabGroupId);
                        }
                    }
                    Console.WriteLine("Sesión cargada exitosamente.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar la sesión: {ex.Message}");
            }
        }

        // Métodos de eventos de WebView2
        private void OnNavigationCompleted(TabItemData tab, CoreWebView2NavigationCompletedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                tab.IsLoading = false;
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoForward));
                tab.LastActivity = DateTime.Now; // Marcar actividad
            });
        }

        private async void OnFaviconChanged(TabItemData tab)
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
                                favicon.CacheOption = BitmapCacheOption.OnLoad;
                                favicon.StreamSource = stream;
                                favicon.EndInit();
                                favicon.Freeze();
                                tab.Favicon = favicon;
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al obtener el favicon: {ex.Message}");
                }
            }
        }


        private async void OnSourceChanged(TabItemData tab, CoreWebView2SourceChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                tab.Url = tab.WebViewInstance.Source.OriginalString;
                OnPropertyChanged(nameof(CurrentUrl));
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoForward));
                tab.LastActivity = DateTime.Now; // Marcar actividad
            });

            // Actualizar título y favicon después de un SourceChanged si el título no se ha actualizado
            // Esto es importante porque SourceChanged ocurre antes de que el título esté disponible.
            // Puedes añadir un pequeño retraso o esperar a un evento como DOMContentLoaded.
            // Para simplicidad, actualizamos el título y favicon aquí de nuevo,
            // pero lo ideal es hacerlo en NavigationCompleted o TitleChanged.
            try
            {
                if (tab.WebViewInstance.CoreWebView2 != null)
                {
                    string title = await tab.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.title");
                    title = System.Text.Json.JsonSerializer.Deserialize<string>(title) ?? "New Tab";
                    Application.Current.Dispatcher.Invoke(() => tab.Title = title);
                }
                await OnFaviconChanged(tab); // Re-obtener el favicon
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar título/favicon en SourceChanged: {ex.Message}");
            }
        }


        private void OnContentLoading(TabItemData tab, CoreWebView2ContentLoadingEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                tab.IsLoading = true;
                tab.LastActivity = DateTime.Now; // Marcar actividad
            });
        }

        private void OnHistoryChanged(TabItemData tab, object e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoForward));
                tab.LastActivity = DateTime.Now; // Marcar actividad
            });
        }

        private async void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            // Ocultar la barra de progreso al inicio de la descarga
            Application.Current.Dispatcher.Invoke(() => DownloadProgressBarVisibility = Visibility.Visible);

            e.Cancel = true; // Cancela la descarga por defecto para manejarla manualmente

            // Mostrar un cuadro de diálogo para guardar el archivo
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = e.ResultFilePath, // Nombre de archivo por defecto
                Filter = "All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                e.ResultFilePath = saveFileDialog.FileName; // Establece la ruta de destino elegida por el usuario
                e.Cancel = false; // Permite que WebView2 continúe con la descarga
                e.Handled = true; // Indica que hemos manejado el evento

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
                            MessageBox.Show($"Descarga completa: {e.ResultFilePath}", "Descarga", MessageBoxButton.OK, MessageBoxImage.Information);
                            DownloadProgressBarVisibility = Visibility.Collapsed;
                            DownloadProgress = 0;
                        }
                        else if (e.DownloadOperation.State == CoreWebView2DownloadState.Interrupted)
                        {
                            MessageBox.Show($"Descarga interrumpida: {e.ResultFilePath}\nMotivo: {e.DownloadOperation.InterruptReason}", "Descarga Fallida", MessageBoxButton.OK, MessageBoxImage.Error);
                            DownloadProgressBarVisibility = Visibility.Collapsed;
                            DownloadProgress = 0;
                        }
                    });
                };

                // Si no se usa await directamente en este método, la advertencia CS1998 aparecerá.
                // La operación de descarga es manejada por los eventos de DownloadOperation.
            }
            else
            {
                // El usuario canceló el diálogo de guardar, por lo tanto cancela la descarga en WebView2
                e.Cancel = true;
                e.Handled = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DownloadProgressBarVisibility = Visibility.Collapsed;
                    DownloadProgress = 0;
                });
            }
        }


        // Métodos auxiliares para convertir imágenes (pueden ser movidos a una clase utilitaria)
        private BitmapImage? ConvertBase64ToBitmapImage(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return null;

            try
            {
                byte[] binaryData = Convert.FromBase64String(base64String);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(binaryData);
                bi.EndInit();
                return bi;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al convertir Base64 a BitmapImage: {ex.Message}");
                return null;
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

        // Implementación de INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Clases internas para serialización de sesión
        private class SessionState
        {
            public string? SelectedTabGroupId { get; set; }
            public List<TabGroupState> TabGroupStates { get; set; } = new List<TabGroupState>();
        }

    }
}
