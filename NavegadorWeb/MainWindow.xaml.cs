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

// Asegúrate de que estas directivas 'using' estén presentes para las clases auxiliares y de servicios
using NavegadorWeb.Classes;
using NavegadorWeb.Extensions;
using NavegadorWeb.Services;
// Agrega los usings para tus ventanas si están en una carpeta 'Windows' o similar
// Si aún no tienes estas ventanas creadas, deberás crearlas o comentar las líneas que las invocan.
using NavegadorWeb.Windows; // ASUMIENDO QUE TUS VENTANAS ESTÁN EN ESTE ESPACIO DE NOMBRES/CARPETA

namespace NavegadorWeb
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // ... (Tu código existente) ...

        // Propiedades públicas con notificación de cambio (INotifyPropertyChanged)
        // ... (Tus propiedades existentes) ...

        // Constructor
        public MainWindow()
        {
            InitializeComponent();
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

        // ... (Métodos StartTabSuspensionTimer, StopTabSuspensionTimer, MainWindow_UserActivity, ActivateTab) ...

        // Carga la configuración de la aplicación desde ConfigurationManager
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

            // Corrección para el error CS0266 (double? a double)
            if (double.TryParse(ConfigurationManager.AppSettings[WindowWidthSettingKey], out double width))
            {
                this.Width = Math.Max(width, MIN_WIDTH);
            }
            else
            {
                this.Width = MIN_WIDTH; // Establecer un valor predeterminado si no se puede analizar
            }

            if (double.TryParse(ConfigurationManager.AppSettings[WindowHeightSettingKey], out double height))
            {
                this.Height = Math.Max(height, MIN_HEIGHT);
            }
            else
            {
                this.Height = MIN_HEIGHT; // Establecer un valor predeterminado si no se puede analizar
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

        // ... (Métodos SaveSettings, UpdateAppSetting, SaveCurrentSession) ...

        // Restaurar la última sesión de navegación desde la configuración
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
                                    // Asegúrate de que CoreWebView2 esté inicializado para cada pestaña restaurada
                                    // Este await es necesario y resuelve CS1998 en RestoreLastSession
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
                            // Asegúrate de que BrowserTabs.ItemsSource se actualice en el hilo de UI
                            Dispatcher.Invoke(() => BrowserTabs.ItemsSource = restoredSelectedGroup.TabsInGroup);
                            SelectedTabItem = restoredSelectedGroup.SelectedTabItem;
                        }
                        else
                        {
                            TabGroupManager.SelectedTabGroup = TabGroupManager.GetDefaultGroup();
                            Dispatcher.Invoke(() => BrowserTabs.ItemsSource = TabGroupManager.GetDefaultGroup().TabsInGroup);
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

        // Aplica la configuración del bloqueador de anuncios a todas las instancias de WebView2
        private async void ApplyAdBlockerSettings()
        {
            // Este método también necesita ser asíncrono si va a interactuar con CoreWebView2
            // de forma asíncrona.
            foreach (var group in TabGroupManager.TabGroups)
            {
                foreach (var tab in group.TabsInGroup)
                {
                    if (tab.WebViewInstance != null)
                    {
                        // Asegura que CoreWebView2 está inicializado antes de acceder a él
                        await tab.WebViewInstance.EnsureCoreWebView2Async(null);
                        if (tab.WebViewInstance.CoreWebView2 != null)
                        {
                            // Aquí iría la lógica para aplicar el bloqueador de anuncios.
                            // Por ejemplo, agregar o remover reglas de red.
                            // Si no hay lógica asíncrona real aquí, puedes remover 'async' y 'await'.
                            Console.WriteLine($"Bloqueador de anuncios aplicado a: {tab.Title}");
                        }
                    }
                }
            }
        }


        // Métodos de navegación y gestión de pestañas
        public async void AddNewTab(string url = "about:blank", bool activate = true)
        {
            // Crea una nueva instancia de WebView2
            var newWebView = new WebView2();

            // Asegura que CoreWebView2 esté inicializado
            await newWebView.EnsureCoreWebView2Async(null);

            // Suscribe eventos para la nueva pestaña
            newWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            newWebView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            newWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            newWebView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            newWebView.CoreWebView2.FaviconChanged += CoreWebView2_FaviconChanged;
            newWebView.CoreWebView2.ContainsFullScreenElementChanged += CoreWebView2_ContainsFullScreenElementChanged;
            newWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            newWebView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;
            newWebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting; // CORREGIDO: Evento en CoreWebView2
            newWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded; // Nuevo evento para Gemini y extracción de texto

            // Suscribe eventos de WebView2 (nivel de control WPF)
            newWebView.Loaded += WebView_Loaded;
            newWebView.Unloaded += WebView_Unloaded;

            // Aplica la configuración del bloqueador de anuncios a la nueva pestaña
            if (IsAdBlockerEnabled)
            {
                // Lógica específica para el bloqueador de anuncios en la nueva pestaña
                // Por ejemplo:
                // await newWebView.CoreWebView2.AddWebResourceRequestedFilterAsync("*", CoreWebView2WebResourceContext.Document);
            }

            var newTab = new TabItemData(newWebView)
            {
                Url = url,
                Title = "Cargando..." // Título inicial
            };

            // Añade la nueva pestaña al grupo seleccionado actualmente
            TabGroupManager.SelectedTabGroup?.AddTab(newTab);

            // Navega a la URL especificada
            newTab.WebViewInstance.Source = new Uri(url);

            if (activate)
            {
                SelectedTabItem = newTab;
            }
        }

        private TabItemData CreateNewTabItem(string url)
        {
            var newWebView = new WebView2();
            newWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            newWebView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            newWebView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            newWebView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            newWebView.CoreWebView2.FaviconChanged += CoreWebView2_FaviconChanged;
            newWebView.CoreWebView2.ContainsFullScreenElementChanged += CoreWebView2_ContainsFullScreenElementChanged;
            newWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            newWebView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;
            newWebView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting; // CORREGIDO
            newWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded; // Nuevo evento

            newWebView.Loaded += WebView_Loaded;
            newWebView.Unloaded += WebView_Unloaded;

            if (IsAdBlockerEnabled)
            {
                // Lógica para el bloqueador de anuncios
            }

            return new TabItemData(newWebView)
            {
                Url = url,
                Title = "Cargando..."
            };
        }

        // Manejador del evento DownloadStarting (ahora correctamente en CoreWebView2)
        private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            // Puedes mostrar un diálogo de guardar archivo o usar una ubicación predeterminada.
            // Para este ejemplo, solo cancelamos la descarga para evitar el diálogo predeterminado del navegador.
            // e.Cancel = true;

            // Implementa tu lógica de descarga aquí
            // Por ejemplo, para mostrar el progreso:
            DownloadProgressBarVisibility = Visibility.Visible;
            DownloadProgress = 0; // Reiniciar progreso

            e.DownloadOperation.ProgressChanged += (s, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    // Progreso de 0 a 100
                    DownloadProgress = (double)e.DownloadOperation.BytesReceived / e.DownloadOperation.TotalBytesToReceive * 100.0;
                });
            };

            e.DownloadOperation.StateChanged += (s, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (e.DownloadOperation.State == CoreWebView2DownloadState.Completed)
                    {
                        DownloadProgressBarVisibility = Visibility.Collapsed;
                        MessageBox.Show($"Descarga completada: {e.DownloadOperation.ResultFilePath}", "Descarga", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (e.DownloadOperation.State == CoreWebView2DownloadState.Interrupted)
                    {
                        DownloadProgressBarVisibility = Visibility.Collapsed;
                        MessageBox.Show($"Descarga interrumpida: {e.DownloadOperation.ResultFilePath}", "Descarga Fallida", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            };

            // Opcional: Establecer una ruta de descarga predeterminada o solicitarla al usuario
            // e.ResultFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Downloads), e.DownloadOperation.EstimatedFileName);
        }

        // ... (otros manejadores de eventos CoreWebView2) ...

        // Lógica de búsqueda en la página
        private async void FindInPage(string searchText)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    // Detiene la búsqueda y limpia los resultados si el texto está vacío
                    SelectedTabItem.WebViewInstance.CoreWebView2.StopFindInPage(CoreWebView2StopFindInPageKind.ClearSelection); // CORREGIDO
                    FindResultsText = "";
                    return;
                }

                // Inicia la búsqueda
                // Los parámetros son: searchText, findNext, matchCase, wholeWord
                var result = await SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(
                    searchText,
                    false, // findNext (false para la primera búsqueda)
                    false, // matchCase
                    false  // wholeWord
                );

                FindResultsText = $"{result.Matches} resultados";
            }
        }

        private async void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null && !string.IsNullOrEmpty(FindSearchText))
            {
                var result = await SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(
                    FindSearchText,
                    false, // findNext (false para Previous, se maneja con CoreWebView2FindInPageKind.FindPrevious)
                    false, // matchCase
                    false, // wholeWord
                    CoreWebView2FindInPageKind.FindPrevious // CORREGIDO
                );
                FindResultsText = $"{result.Matches} resultados";
            }
        }

        private async void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null && !string.IsNullOrEmpty(FindSearchText))
            {
                var result = await SelectedTabItem.WebViewInstance.CoreWebView2.FindInPage(
                    FindSearchText,
                    true, // findNext (true para Next)
                    false, // matchCase
                    false, // wholeWord
                    CoreWebView2FindInPageKind.FindNext // CORREGIDO
                );
                FindResultsText = $"{result.Matches} resultados";
            }
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            IsFindBarVisible = false;
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                SelectedTabItem.WebViewInstance.CoreWebView2.StopFindInPage(CoreWebView2StopFindInPageKind.ClearSelection); // CORREGIDO
                FindResultsText = "";
            }
        }

        // Manejadores de eventos de UI para botones de la barra de herramientas

        // Abre la ventana de Historial
        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que HistoryWindow exista en el espacio de nombres NavegadorWeb.Windows
            HistoryWindow historyWindow = new HistoryWindow();
            historyWindow.ShowDialog();
        }

        // Abre la ventana de Marcadores
        private void BookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que BookmarksWindow exista en el espacio de nombres NavegadorWeb.Windows
            BookmarksWindow bookmarksWindow = new BookmarksWindow();
            bookmarksWindow.ShowDialog();
        }

        // Abre la ventana del Administrador de Contraseñas
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que PasswordManagerWindow exista en el espacio de nombres NavegadorWeb.Windows
            PasswordManagerWindow passwordManagerWindow = new PasswordManagerWindow();
            passwordManagerWindow.ShowDialog();
        }

        // Abre la ventana de Extracción de Datos para IA (si aplica)
        private async void ExtractDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                // Este método necesita ser asíncrono si interactúa con CoreWebView2
                // Por ejemplo, para ejecutar JavaScript:
                var html = await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML;");
                var text = await SelectedTabItem.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.body.innerText;");

                // Asumo que CapturedPageData ya tiene propiedades para el texto extraído.
                SelectedTabItem.CapturedData.Url = SelectedTabItem.Url;
                SelectedTabItem.CapturedData.Title = SelectedTabItem.Title;
                SelectedTabItem.CapturedData.ExtractedText = text;

                // Captura de pantalla:
                var screenshotStream = new MemoryStream();
                await SelectedTabItem.WebViewInstance.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, screenshotStream);
                screenshotStream.Position = 0; // Reiniciar la posición del stream
                BitmapImage screenshotImage = new BitmapImage();
                screenshotImage.BeginInit();
                screenshotImage.CacheOption = BitmapCacheOption.OnLoad;
                screenshotImage.StreamSource = screenshotStream;
                screenshotImage.EndInit();

                // Convertir a Base64
                SelectedTabItem.CapturedData.ScreenshotBase64 = ConvertBitmapImageToBase64(screenshotImage);

                // Abre la ventana GeminiDataViewerWindow con los datos capturados
                // Asegúrate de que DataExtractionWindow exista en el espacio de nombres NavegadorWeb.Windows
                var dataExtractionWindow = new DataExtractionWindow(SelectedTabItem.CapturedData); // Pasa los datos capturados
                dataExtractionWindow.ShowDialog();
            }
        }


        // Abre la ventana de Configuración
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que SettingsWindow exista en el espacio de nombres NavegadorWeb.Windows
            SettingsWindow settingsWindow = new SettingsWindow();
            // Pasa la instancia de MainWindow si SettingsWindow necesita interactuar con ella
            // settingsWindow.Owner = this;
            settingsWindow.ShowDialog();

            // Vuelve a cargar y aplicar la configuración después de que se cierre la ventana de configuración
            LoadSettings();
            ApplyAdBlockerSettings();
            ApplyTheme();
        }


        // Funcionalidad de Picture-in-Picture (PiP)
        private async void PipButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                // Abre una nueva ventana para PiP
                // Asegúrate de que PipWindow exista en el espacio de nombres NavegadorWeb.Windows
                var pipWindow = new PipWindow(SelectedTabItem.WebViewInstance.Source.ToString()); // Pasa la URL actual
                pipWindow.Show();
                // Opcional: puedes intentar ocultar la pestaña original o redirigirla
            }
        }

        // ... (otros métodos como ScreenshotButton_Click, SplitViewButton_Click, GeminiButton_Click) ...

        // Abre la ventana de Extensiones
        private void ExtensionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegúrate de que ExtensionsWindow exista en el espacio de nombres NavegadorWeb.Windows
            ExtensionsWindow extensionsWindow = new ExtensionsWindow(ExtensionManager); // Pasa tu ExtensionManager
            extensionsWindow.ShowDialog();
        }

        // Lógica para guardar la contraseña (si tienes un administrador de contraseñas)
        private void SavePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            // Necesitas una referencia a PasswordManager y la lógica para obtener la URL y las credenciales
            // Por ejemplo:
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                // Este await es para la advertencia CS1998, asumiendo que PasswordManager.SavePasswordAsync es asíncrono
                // Si PasswordManager no existe, o SavePassword no es asíncrono, ajusta esta línea.
                // await PasswordManager.SavePasswordAsync(SelectedTabItem.Url, "username", "password"); // Ejemplo
            }
            MessageBox.Show("Funcionalidad de guardar contraseña no implementada completamente o no conectada.");
        }

        // Método para capturar el texto y la captura de pantalla para Gemini
        private async Task CapturePageDataForGemini()
        {
            if (SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
            {
                var currentTab = SelectedTabItem;

                // Extrae texto y HTML
                var html = await currentTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML;");
                var text = await currentTab.WebViewInstance.CoreWebView2.ExecuteScriptAsync("document.body.innerText;");

                currentTab.CapturedData.Url = currentTab.Url;
                currentTab.CapturedData.Title = currentTab.Title;
                currentTab.CapturedData.ExtractedText = text; // Asigna el texto extraído

                // Captura de pantalla
                try
                {
                    using (var screenshotStream = new MemoryStream())
                    {
                        await currentTab.WebViewInstance.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, screenshotStream);
                        screenshotStream.Position = 0; // Reiniciar la posición del stream

                        // Crear BitmapImage a partir del stream
                        BitmapImage screenshotImage = new BitmapImage();
                        screenshotImage.BeginInit();
                        screenshotImage.CacheOption = BitmapCacheOption.OnLoad;
                        screenshotImage.StreamSource = screenshotStream;
                        screenshotImage.EndInit();

                        // Convertir a Base64 y asignar a CapturedData
                        currentTab.CapturedData.ScreenshotBase64 = ConvertBitmapImageToBase64(screenshotImage);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al capturar la vista previa: {ex.Message}");
                    currentTab.CapturedData.ScreenshotBase64 = string.Empty; // Asegurar que no hay datos parciales
                }

                // Captura el favicon (si es posible)
                try
                {
                    // Esto es más complejo. Normalmente, WebView2 no expone directamente el favicon.
                    // Podrías necesitar buscar el favicon en el HTML o en la caché.
                    // Por ahora, lo dejaremos vacío o buscar una URL de favicon.
                    // Para un favicon real, es posible que necesites una lógica más avanzada.
                    // currentTab.CapturedData.FaviconBase64 = await GetFaviconBase64(currentTab.Url); // Función hipotética
                    currentTab.CapturedData.FaviconBase64 = string.Empty; // Placeholder
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al capturar el favicon: {ex.Message}");
                    currentTab.CapturedData.FaviconBase64 = string.Empty;
                }

                // Añadir los datos capturados a la colección observable (si aún no está)
                if (!CapturedPagesForGemini.Contains(currentTab.CapturedData))
                {
                    CapturedPagesForGemini.Add(currentTab.CapturedData);
                }

                Console.WriteLine($"Datos capturados para Gemini: {currentTab.Url}");

                // Abre la ventana de visualización de datos de Gemini
                // Asegúrate de que GeminiDataViewerWindow exista en el espacio de nombres NavegadorWeb.Windows
                GeminiDataViewerWindow geminiViewer = new GeminiDataViewerWindow(CapturedPagesForGemini); // Pasa la colección
                geminiViewer.ShowDialog();
            }
        }
    }
}
