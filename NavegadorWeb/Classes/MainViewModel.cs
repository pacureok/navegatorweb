// NavegadorWeb/Classes/MainViewModel.cs (o ViewModels/MainViewModel.cs)
using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input; // Para ICommand
using Microsoft.Web.WebView2.Wpf; // Para WebView2
using NavegadorWeb.Services; // Para HistoryManager, SettingsManager, etc.
using System.Linq; // Para FirstOrDefault

namespace NavegadorWeb.Classes
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Evento PropertyChanged para la notificación de cambios en las propiedades
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Propiedades enlazables
        private TabGroupManager _tabGroupManager;
        public TabGroupManager TabGroupManager
        {
            get => _tabGroupManager;
            set
            {
                if (_tabGroupManager != value)
                {
                    _tabGroupManager = value;
                    OnPropertyChanged(nameof(TabGroupManager));
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
                    // Aquí puedes añadir lógica si necesitas hacer algo cuando la pestaña seleccionada cambia
                    // Por ejemplo, cargar el historial de navegación para esa pestaña o actualizar la URL en la barra de direcciones.
                }
            }
        }

        private string _downloadProgressText = "";
        public string DownloadProgressText
        {
            get => _downloadProgressText;
            set
            {
                if (_downloadProgressText != value)
                {
                    _downloadProgressText = value;
                    OnPropertyChanged(nameof(DownloadProgressText));
                }
            }
        }

        private double _downloadProgress = 0;
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
        public ICommand NavigateCommand { get; private set; }
        public ICommand GoBackCommand { get; private set; }
        public ICommand GoForwardCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand NewTabCommand { get; private set; }
        public ICommand CloseTabCommand { get; private set; }
        public ICommand AddNewTabGroupCommand { get; private set; }
        public ICommand RemoveSelectedTabGroupCommand { get; private set; }

        public MainViewModel()
        {
            TabGroupManager = new TabGroupManager();
            InitializeCommands();
            LoadSession(); // Cargar la sesión al iniciar el ViewModel
        }

        private void InitializeCommands()
        {
            NavigateCommand = new RelayCommand(ExecuteNavigate);
            GoBackCommand = new RelayCommand(ExecuteGoBack, CanExecuteGoBack);
            GoForwardCommand = new RelayCommand(ExecuteGoForward, CanExecuteGoForward);
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            NewTabCommand = new RelayCommand(ExecuteNewTab);
            CloseTabCommand = new RelayCommand(ExecuteCloseTab);
            AddNewTabGroupCommand = new RelayCommand(ExecuteAddNewTabGroup);
            RemoveSelectedTabGroupCommand = new RelayCommand(ExecuteRemoveSelectedTabGroup, CanExecuteRemoveSelectedTabGroup);
        }

        public void InitializeBrowser()
        {
            // Lógica de inicialización del navegador, como abrir la primera pestaña si no hay ninguna.
            if (!TabGroupManager.TabGroups.Any() || !TabGroupManager.SelectedTabGroup.TabsInGroup.Any())
            {
                ExecuteNewTab(null); // Abre una pestaña nueva si no hay ninguna
            }
        }

        private void ExecuteNavigate(object? url)
        {
            string urlString = url?.ToString() ?? "about:blank";
            if (!urlString.StartsWith("http://") && !urlString.StartsWith("https://") && !urlString.StartsWith("about:"))
            {
                // Intenta prefijar con HTTPS si no tiene protocolo
                urlString = "https://" + urlString;
            }

            if (SelectedTabItem != null)
            {
                SelectedTabItem.Url = urlString;
                SelectedTabItem.WebViewInstance.Source = new Uri(urlString);
                // Asegúrate de añadir a la historia aquí
                HistoryManager.AddHistoryEntry(SelectedTabItem.Title, SelectedTabItem.Url);
            }
        }

        private bool CanExecuteGoBack(object? parameter)
        {
            return SelectedTabItem?.WebViewInstance?.CoreWebView2?.CanGoBack ?? false;
        }

        private void ExecuteGoBack(object? parameter)
        {
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.GoBack();
        }

        private bool CanExecuteGoForward(object? parameter)
        {
            return SelectedTabItem?.WebViewInstance?.CoreWebView2?.CanGoForward ?? false;
        }

        private void ExecuteGoForward(object? parameter)
        {
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.GoForward();
        }

        private void ExecuteRefresh(object? parameter)
        {
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.Reload();
        }

        private async void ExecuteNewTab(object? parameter)
        {
            var newWebView = new WebView2();
            // Asegúrate de inicializar CoreWebView2 antes de añadir la pestaña
            // Es crucial esperar a que CoreWebView2 se inicialice para evitar errores
            await newWebView.EnsureCoreWebView2Async(null);

            var newTab = new TabItemData(newWebView)
            {
                Title = "Nueva Pestaña",
                Url = "about:blank" // URL por defecto
            };

            // Suscribirse a eventos del WebView2 para actualizar la UI (título, URL, favicon)
            newWebView.CoreWebView2.DocumentTitleChanged += (s, e) => newTab.Title = newWebView.CoreWebView2.DocumentTitle;
            newWebView.CoreWebView2.SourceChanged += (s, e) => {
                newTab.Url = newWebView.CoreWebView2.Source;
                HistoryManager.AddHistoryEntry(newTab.Title, newTab.Url); // Añadir al historial
            };
            newWebView.CoreWebView2.FaviconChanged += async (s, e) =>
            {
                try
                {
                    using (var stream = await newWebView.CoreWebView2.GetFaviconStreamAsync())
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Congelar para que pueda ser accedida desde cualquier hilo
                        newTab.Favicon = bitmap;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al obtener favicon: {ex.Message}");
                    newTab.Favicon = null; // O establece un icono por defecto
                }
            };

            // Manejar eventos de descarga
            newWebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;


            TabGroupManager.SelectedTabGroup?.AddTab(newTab);
            SelectedTabItem = newTab; // Seleccionar la nueva pestaña
            newWebView.Source = new Uri("https://www.google.com"); // Cargar una URL por defecto
        }

        private void ExecuteCloseTab(object? tabToClose)
        {
            if (tabToClose is TabItemData tab)
            {
                TabGroupManager.SelectedTabGroup?.RemoveTab(tab);
                // Dispose del WebView2 para liberar recursos
                tab.WebViewInstance.Dispose();

                // Si no quedan pestañas en el grupo, cerrar el grupo o añadir una nueva pestaña
                if (!TabGroupManager.SelectedTabGroup.TabsInGroup.Any())
                {
                    if (TabGroupManager.TabGroups.Count > 1) // Si hay otros grupos, elimina este
                    {
                        TabGroupManager.TabGroups.Remove(TabGroupManager.SelectedTabGroup);
                    }
                    else // Si es el último grupo, abre una nueva pestaña
                    {
                        ExecuteNewTab(null);
                    }
                }
            }
        }

        private void ExecuteAddNewTabGroup(object? parameter)
        {
            var newGroup = new TabGroup($"Grupo {TabGroupManager.TabGroups.Count + 1}");
            TabGroupManager.TabGroups.Add(newGroup);
            TabGroupManager.SelectedTabGroup = newGroup;
            ExecuteNewTab(null); // Abre una pestaña por defecto en el nuevo grupo
        }

        private bool CanExecuteRemoveSelectedTabGroup(object? parameter)
        {
            return TabGroupManager.TabGroups.Count > 1; // Siempre debe haber al menos un grupo
        }

        private void ExecuteRemoveSelectedTabGroup(object? parameter)
        {
            if (TabGroupManager.SelectedTabGroup != null && TabGroupManager.TabGroups.Count > 1)
            {
                TabGroupManager.TabGroups.Remove(TabGroupManager.SelectedTabGroup);
                // Selecciona el primer grupo disponible después de eliminar
                TabGroupManager.SelectedTabGroup = TabGroupManager.TabGroups.FirstOrDefault();
            }
        }


        // Implementación del manejador de eventos para las descargas
        private async void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            e.Cancel = true; // Cancela la descarga por defecto para manejarla manualmente

            // Configurar la ruta de descarga (puedes hacer que esto sea configurable por el usuario)
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!Directory.Exists(downloadsPath))
            {
                Directory.CreateDirectory(downloadsPath);
            }

            // Nombre de archivo con marca de tiempo para evitar sobrescribir
            string suggestedFileName = e.DownloadOperation.ResultFilePath;
            string fileName = Path.GetFileName(suggestedFileName);
            string filePath = Path.Combine(downloadsPath, fileName);

            // Asegurarse de que el nombre del archivo sea único si ya existe
            int count = 1;
            string baseFileName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(downloadsPath, $"{baseFileName} ({count}){extension}");
                count++;
            }

            e.ResultFilePath = filePath; // Establece la ruta de descarga final

            // Iniciar la operación de descarga
            e.DownloadOperation.StateChanged += (s, args) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    switch (e.DownloadOperation.State)
                    {
                        case CoreWebView2DownloadState.InProgress:
                            DownloadProgressBarVisibility = Visibility.Visible;
                            DownloadProgress = e.DownloadOperation.BytesReceived / (double)e.DownloadOperation.TotalBytesToReceive * 100;
                            DownloadProgressText = $"Descargando {e.DownloadOperation.BytesReceived / (1024.0 * 1024.0):F2} MB de {e.DownloadOperation.TotalBytesToReceive / (1024.0 * 1024.0):F2} MB";
                            break;
                        case CoreWebView2DownloadState.Completed:
                            DownloadProgressBarVisibility = Visibility.Collapsed;
                            DownloadProgress = 0;
                            DownloadProgressText = "";
                            MessageBox.Show($"Descarga completada: {e.ResultFilePath}", "Descarga", MessageBoxButton.OK, MessageBoxImage.Information);
                            // Opcional: Abrir la carpeta de descargas o el archivo
                            // Process.Start(new ProcessStartInfo(downloadsPath) { UseShellExecute = true });
                            break;
                        case CoreWebView2DownloadState.Interrupted:
                            DownloadProgressBarVisibility = Visibility.Collapsed;
                            DownloadProgress = 0;
                            DownloadProgressText = "";
                            MessageBox.Show($"Descarga interrumpida: {e.ResultFilePath}. Razón: {e.DownloadOperation.InterruptReason}", "Descarga", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                });
            };

            // Iniciar la descarga
            e.Handled = true; // Indica que estamos manejando la descarga
        }

        // Lógica para guardar y restaurar la sesión (ejemplo)
        public void SaveSession()
        {
            var sessionState = new List<TabGroupState>();
            foreach (var group in TabGroupManager.TabGroups)
            {
                sessionState.Add(new TabGroupState
                {
                    GroupId = group.GroupId,
                    GroupName = group.GroupName,
                    TabUrls = group.TabsInGroup.Select(t => t.Url).ToList(),
                    SelectedTabUrl = group.SelectedTabItem?.Url
                });
            }

            try
            {
                string jsonString = JsonSerializer.Serialize(sessionState);
                File.WriteAllText("session.json", jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar la sesión: {ex.Message}");
            }
        }

        public async void LoadSession()
        {
            if (File.Exists("session.json"))
            {
                try
                {
                    string jsonString = File.ReadAllText("session.json");
                    var sessionState = JsonSerializer.Deserialize<List<TabGroupState>>(jsonString);

                    TabGroupManager.TabGroups.Clear(); // Limpiar grupos existentes

                    foreach (var groupState in sessionState ?? new List<TabGroupState>())
                    {
                        var group = new TabGroup(groupState.GroupName) { GroupId = groupState.GroupId };
                        TabGroupManager.TabGroups.Add(group);

                        foreach (var url in groupState.TabUrls ?? new List<string?>())
                        {
                            if (url != null)
                            {
                                var newWebView = new WebView2();
                                await newWebView.EnsureCoreWebView2Async(null); // Asegúrate de inicializar
                                var newTab = new TabItemData(newWebView) { Url = url };

                                // Re-suscribir eventos del WebView2
                                newWebView.CoreWebView2.DocumentTitleChanged += (s, e) => newTab.Title = newWebView.CoreWebView2.DocumentTitle;
                                newWebView.CoreWebView2.SourceChanged += (s, e) => newTab.Url = newWebView.CoreWebView2.Source;
                                newWebView.CoreWebView2.FaviconChanged += async (s, e) =>
                                {
                                    try
                                    {
                                        using (var stream = await newWebView.CoreWebView2.GetFaviconStreamAsync())
                                        {
                                            var bitmap = new BitmapImage();
                                            bitmap.BeginInit();
                                            bitmap.StreamSource = stream;
                                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                            bitmap.EndInit();
                                            bitmap.Freeze();
                                            newTab.Favicon = bitmap;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error al obtener favicon: {ex.Message}");
                                        newTab.Favicon = null;
                                    }
                                };
                                newWebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;


                                group.AddTab(newTab);
                                // Navegar a la URL
                                newWebView.Source = new Uri(url);
                            }
                        }

                        // Restaurar la pestaña seleccionada del grupo
                        group.SelectedTabItem = group.TabsInGroup.FirstOrDefault(t => t.Url == groupState.SelectedTabUrl);
                    }

                    // Seleccionar el grupo que estaba seleccionado
                    TabGroupManager.SelectedTabGroup = TabGroupManager.TabGroups.FirstOrDefault(g => g.GroupName == sessionState?.FirstOrDefault(gs => gs.SelectedTabUrl != null)?.GroupName); // Esto es un poco simplificado, mejor usar GroupId

                    if (TabGroupManager.SelectedTabGroup == null && TabGroupManager.TabGroups.Any())
                    {
                        TabGroupManager.SelectedTabGroup = TabGroupManager.TabGroups.First();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cargar la sesión: {ex.Message}");
                    // Si falla la carga, inicializa con una nueva pestaña
                    ExecuteNewTab(null);
                }
            }
            else
            {
                ExecuteNewTab(null); // Si no hay archivo de sesión, abre una nueva pestaña
            }
        }
    }
}
