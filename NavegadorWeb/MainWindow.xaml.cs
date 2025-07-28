using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text.Json; // Para JsonSerializer
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // Para Popup
using System.Windows.Media;
using System.Windows.Media.Imaging; // Para BitmapImage
using HtmlAgilityPack; // Asegúrate de haberlo instalado vía NuGet
using Microsoft.Web.WebView2.Wpf; // Para WebView2
using Microsoft.Web.WebView2.Core; // Para CoreWebView2CapturePreviewImageFormat
using NavegadorWeb.Windows; // Si usas SettingsWindow, etc.

namespace NavegadorWeb
{
    public partial class MainWindow : Window
    {
        // Declara una instancia de HttpClient para hacer las peticiones web
        private readonly HttpClient _http = new HttpClient();
        // Declara un Popup flotante que usaremos para el tooltip de la URL
        private readonly Popup _urlHoverTooltip = new Popup { AllowsTransparency = true };
        // Declara el WebView2 oculto que usaremos para generar miniaturas
        private WebView2 _hiddenWebView;

        public MainWindow()
        {
            InitializeComponent();

            // Asigna el WebView2 oculto declarado en XAML a la variable
            _hiddenWebView = this.HiddenWebView;

            // Opcional: Establecer una ruta de usuario para el WebView2 oculto para evitar conflictos con el principal
            // string userDataFolder = Path.Combine(Path.GetTempPath(), "WebView2HiddenData");
            // Directory.CreateDirectory(userDataFolder); // Asegúrate de que la carpeta exista
            // await _hiddenWebView.EnsureCoreWebView2Async(null, userDataFolder); // Puedes añadir esto en un handler si necesitas inicializarlo antes.
            // Para FetchPreviewAsync, EnsureCoreWebView2Async() se llama internamente.

            // Aquí puedes establecer el DataContext para tu ViewModel si usas MVVM.
            // Por ejemplo: this.DataContext = new MainViewModel();
        }

        // --- MANEJADORES DE EVENTOS DE LA VENTANA ---
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lógica que se ejecuta una vez que la ventana y su contenido han sido cargados.
            // Por ejemplo, cargar una página de inicio en la primera pestaña o añadir una pestaña por defecto.
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Lógica para manejar el cierre de la ventana.
            // Aquí podrías guardar el estado de la sesión, confirmar con el usuario, etc.
            _http.Dispose(); // Libera los recursos del HttpClient
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Ajustes que se realizan después de que la ventana ha sido inicializada,
            // útil para operaciones relacionadas con la ventana nativa (manejo de esquinas, sombras, etc.).
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Lógica para responder a cambios en el estado de la ventana (Minimizado, Maximizado, Normal).
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Permite arrastrar la ventana desde la barra de título personalizada.
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // --- MANEJADORES DE EVENTOS DE LA BARRA DE TÍTULO / CONTROLES DE LA VENTANA ---
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
            => WindowState = (WindowState == WindowState.Maximized)
                ? WindowState.Normal
                : WindowState.Maximized;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Close(); // Cierra la ventana.

        // --- MANEJADORES DE EVENTOS DE LA BARRA DE DIRECCIONES Y NAVEGACIÓN ---
        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Lógica para navegar a la URL en AddressBar.Text
                // Asumiendo que TabGroupManager.SelectedTabItem es la pestaña activa
                // y que tiene una propiedad WebView para el control WebView2.
                // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.CoreWebView2.Navigate(AddressBar.Text);
            }
        }

        // --- MANEJADORES DE EVENTOS DE LA BARRA DE BÚSQUEDA (FindBar) ---
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = (FindBar.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            if (FindBar.Visibility == Visibility.Visible)
            {
                FindTextBox.Focus();
                FindTextBox.SelectAll();
            }
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Lógica para iniciar la búsqueda en la página activa
                // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.CoreWebView2.Find(FindTextBox.Text, CoreWebView2FindMatchOptions.None);
            }
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica para actualizar la búsqueda en tiempo real o preparar la siguiente búsqueda
            // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.CoreWebView2.Find(FindTextBox.Text, CoreWebView2FindMatchOptions.None);
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para buscar la ocurrencia anterior
            // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.CoreWebView2.FindNext(CoreWebView2FindMatchOptions.None); // O buscar hacia atrás
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para buscar la ocurrencia siguiente
            // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.CoreWebView2.FindNext(CoreWebView2FindMatchOptions.None);
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Collapsed;
            FindTextBox.Clear();
            // También detener la búsqueda activa en WebView2
            // Ejemplo: (BrowserTabs.SelectedItem as BrowserTabItem)?.WebView.CoreWebView2.StopFind(CoreWebView2StopFindAction.ClearSelection);
        }

        // --- MANEJADORES DE EVENTOS DE CARACTERÍSTICAS ADICIONALES ---
        private void GeminiButton_Click(object sender, RoutedEventArgs e) { /* Lógica para Gemini */ }
        private void PipButton_Click(object sender, RoutedEventArgs e) { /* Lógica para Picture-in-Picture */ }
        private void ReadAloudButton_Click(object sender, RoutedEventArgs e) { /* Lógica para lectura en voz alta */ }
        private void ReaderModeButton_Click(object sender, RoutedEventArgs e) { /* Lógica para modo lectura */ }
        private void IncognitoButton_Click(object sender, RoutedEventArgs e) { /* Lógica para modo incógnito */ }
        private void HistoryButton_Click(object sender, RoutedEventArgs e) { /* Lógica para Historial */ }
        private void BookmarksButton_Click(object sender, RoutedEventArgs e) { /* Lógica para Marcadores */ }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e) { /* Lógica para Gestor de Contraseñas */ }
        private void DataExtractionButton_Click(object sender, RoutedEventArgs e) { /* Lógica para Extracción de Datos */ }
        private void ExtensionMenuItem_Click(object sender, RoutedEventArgs e) { /* Lógica para Extensiones */ }
        private void ManageExtensionsButton_Click(object sender, RoutedEventArgs e) { /* Lógica para Gestor de Extensiones */ }
        private void SettingsButton_Click(object sender, RoutedEventArgs e) { /* Lógica para Configuración */ }

        // --- MANEJADORES DE EVENTOS DE PESTAÑAS (DEBES ADAPTAR ESTO A TU GESTIÓN DE PESTAÑAS) ---
        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // Lógica para manejar el cambio de selección de pestaña.
            // Esto debería actualizar la barra de direcciones y otros elementos de UI
            // para reflejar la pestaña seleccionada actualmente.
            // Ejemplo: if (BrowserTabs.SelectedItem is BrowserTabItem selectedTab) { AddressBar.Text = selectedTab.Url; }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            Button closeButton = sender as Button;
            if (closeButton != null && closeButton.Tag != null)
            {
                // Asume que el Tag del botón es el objeto de la pestaña (ej. tu 'BrowserTabItem' o 'TabItemData')
                // if (closeButton.Tag is BrowserTabItem tabToClose)
                // {
                //     // Aquí debes tener la lógica para remover 'tabToClose' de la colección
                //     // a la que está enlazado 'BrowserTabs.ItemsSource'.
                //     // Por ejemplo: (this.DataContext as MainViewModel)?.TabGroupManager.SelectedTabGroup.TabsInGroup.Remove(tabToClose);
                // }
            }
        }

        // --- NUEVA LÓGICA PARA HOVER Y CLIC DERECHO + SHIFT ---

        // Manejador para el evento CoreWebView2InitializationCompleted de CADA WebView2 de una pestaña
        private async void BrowserWebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            WebView2 currentWebView = sender as WebView2;
            if (currentWebView?.CoreWebView2 == null) return;

            // Inyecta el script para capturar mouseover y contextmenu en cada documento creado por este WebView2
            await currentWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                window.chrome.webview.addEventListener('message', event => {}); // Placeholder (si no lo necesitas, puedes omitirlo)

                document.addEventListener('mouseover', ev => {
                    let a = ev.target.closest('a'); // Encuentra el 'a' más cercano en el DOM
                    if (a && a.href) {
                        // Envía un mensaje al host (C#) con la URL y las coordenadas
                        window.chrome.webview.postMessage(JSON.stringify({
                            type: 'hover',
                            href: a.href,
                            x: ev.clientX,
                            y: ev.clientY
                        }));
                    }
                });

                document.addEventListener('contextmenu', ev => {
                    let a = ev.target.closest('a');
                    if (a && a.href && ev.shiftKey) { // Solo si Shift está presionado
                        ev.preventDefault(); // Previene el menú contextual predeterminado del navegador
                        window.chrome.webview.postMessage(JSON.stringify({
                            type: 'shiftContext',
                            href: a.href,
                            x: ev.clientX,
                            y: ev.clientY
                        }));
                    }
                });
            ");
        }

        // Manejador para la recepción de mensajes del JavaScript
        private async void BrowserWebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                // Deserializa el mensaje JSON a nuestro modelo JsMessage
                var msg = JsonSerializer.Deserialize<JsMessage>(args.WebMessageAsJson);

                if (msg == null) return;

                switch (msg.Type)
                {
                    case "hover":
                        // Muestra el ToolTip al hacer hover
                        await ShowTooltipAsync(msg.Href, msg.X, msg.Y);
                        break;
                    case "shiftContext":
                        // Muestra el Popup al hacer clic derecho + Shift
                        await ShowPopupAsync(msg.Href, msg.X, msg.Y);
                        break;
                }
            }
            catch (JsonException ex)
            {
                // Manejar errores de deserialización JSON
                MessageBox.Show($"Error al deserializar mensaje JS: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Otros errores inesperados
                MessageBox.Show($"Error en WebMessageReceived: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Modelo para deserializar los mensajes JSON enviados desde JavaScript.
        /// </summary>
        private class JsMessage
        {
            public string Type { get; set; } = string.Empty; // "hover" o "shiftContext"
            public string Href { get; set; } = string.Empty; // La URL
            public int X { get; set; } // Coordenada X del ratón
            public int Y { get; set; } // Coordenada Y del ratón
        }

        /// <summary>
        /// Modelo para almacenar los datos de previsualización de la página.
        /// </summary>
        private class PagePreview
        {
            public string Title { get; set; } = "Cargando...";
            public string Description { get; set; } = "Cargando descripción...";
            public BitmapImage? Thumbnail { get; set; } // La miniatura de la página
        }

        /// <summary>
        /// Obtiene el título, la meta descripción y una captura de pantalla de una URL.
        /// </summary>
        /// <param name="url">La URL de la página a previsualizar.</param>
        /// <returns>Un objeto PagePreview con los datos.</returns>
        private async Task<PagePreview> FetchPreviewAsync(string url)
        {
            var preview = new PagePreview();

            try
            {
                // 1. Obtener Metadatos (Título y Descripción) usando HttpClient y HtmlAgilityPack
                var html = await _http.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                preview.Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? url; // Si no hay título, usa la URL
                var descNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
                preview.Description = descNode?.GetAttributeValue("content", "") ?? "No hay descripción disponible.";

                // 2. Obtener Screenshot con el WebView2 oculto
                // Asegura que el CoreWebView2 esté inicializado para el WebView2 oculto
                await _hiddenWebView.EnsureCoreWebView2Async();
                _hiddenWebView.CoreWebView2.Navigate(url);

                // Espera un tiempo para que la página cargue los elementos básicos.
                // Podrías implementar una lógica más robusta para esperar que el DOM esté listo.
                await Task.Delay(2000);

                using (var ms = new MemoryStream())
                {
                    // Captura la previsualización y la guarda en el MemoryStream
                    await _hiddenWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
                    ms.Position = 0; // Reinicia la posición del stream

                    // Convierte el MemoryStream a BitmapImage para WPF
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = ms;
                    bmp.CacheOption = BitmapCacheOption.OnLoad; // Carga la imagen inmediatamente
                    bmp.EndInit();
                    bmp.Freeze(); // Hace el BitmapImage inmutable (útil para threading)
                    preview.Thumbnail = bmp;
                }
            }
            catch (HttpRequestException httpEx)
            {
                preview.Title = "Error de Red";
                preview.Description = $"No se pudo cargar la URL: {httpEx.Message}";
                preview.Thumbnail = null;
            }
            catch (Exception ex)
            {
                preview.Title = "Error de Carga";
                preview.Description = $"Ocurrió un error al procesar la página: {ex.Message}";
                preview.Thumbnail = null;
            }
            return preview;
        }

        /// <summary>
        /// Muestra un ToolTip flotante con la previsualización de la página.
        /// </summary>
        /// <param name="url">La URL de la página.</param>
        /// <param name="clientX">Coordenada X del ratón en el WebView2.</param>
        /// <param name="clientY">Coordenada Y del ratón en el WebView2.</param>
        private async Task ShowTooltipAsync(string url, int clientX, int clientY)
        {
            // Cierra cualquier ToolTip anterior
            _urlHoverTooltip.IsOpen = false;

            // Obtén la previsualización (puedes añadir un caché aquí para evitar recargar lo mismo)
            var preview = await FetchPreviewAsync(url);

            // Crea el contenido del ToolTip
            var contentStack = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = preview.Title, FontWeight = FontWeights.Bold, FontSize = 12, Foreground = Brushes.Black },
                    new TextBlock { Text = preview.Description, TextWrapping = TextWrapping.Wrap, MaxWidth = 250, Foreground = Brushes.DimGray, Margin = new Thickness(0,2,0,5) }
                }
            };
            if (preview.Thumbnail != null)
            {
                contentStack.Children.Add(new Image { Source = preview.Thumbnail, Width = 250, Height = 150, Stretch = Stretch.UniformToFill });
            }

            var border = new Border
            {
                Background = Brushes.White,
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(6),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Child = contentStack
            };

            _urlHoverTooltip.Child = border;

            // Posiciona el ToolTip relativo a la ventana principal
            // Convierte las coordenadas del WebView2 a coordenadas de pantalla de WPF
            // Necesitamos la posición del WebView2 en pantalla para esto.
            // Asume que el WebView2 que envió el mensaje es el 'sender' en WebMessageReceived.
            // Para posicionar el tooltip, necesitamos las coordenadas relativas a la pantalla del Monitor, no del WebView2.
            // Obtenemos la posición de la ventana principal en la pantalla
            Point windowTopLeft = new Point(this.Left, this.Top);

            // Convertimos las coordenadas del evento dentro del WebView2 a coordenadas de pantalla
            // Esto es complicado porque el WebView2 puede no estar en 0,0 de la ventana.
            // Para simplificar, asumimos que cx, cy son relativas a la pantalla para el tooltip,
            // o que necesitas convertir las coordenadas del ratón a coordenadas de pantalla de WPF.

            // Mejor enfoque: Las coordenadas X, Y del JavaScript ya son relativas al viewport del documento.
            // Para convertirlas a coordenadas de pantalla de WPF, necesitamos:
            // 1. La posición del WebView2 en la ventana de WPF.
            // 2. La posición de la ventana de WPF en la pantalla.
            // Simplificando por ahora para que el tooltip aparezca cerca del cursor del mouse en pantalla:
            // Obtener la posición del ratón en la pantalla
            Point screenMousePos = Mouse.GetPosition(this); // Esto da la posición relativa a la ventana.
            screenMousePos.X += this.Left;
            screenMousePos.Y += this.Top;


            _urlHoverTooltip.HorizontalOffset = screenMousePos.X + 10; // Un pequeño offset para que no cubra el cursor
            _urlHoverTooltip.VerticalOffset = screenMousePos.Y + 10;
            _urlHoverTooltip.Placement = PlacementMode.AbsolutePoint; // Posiciona por coordenadas absolutas de pantalla
            _urlHoverTooltip.StaysOpen = true; // Permanece abierto mientras el ratón esté sobre la URL

            _urlHoverTooltip.IsOpen = true;

            // Cerrar tras 2s si no hay más actividad de hover (se puede mejorar para seguir el ratón)
            // Considera un temporizador que se reinicia con cada evento 'hover' y cierra el tooltip si expira.
            await Task.Delay(2000); // Espera 2 segundos
            if (_urlHoverTooltip.IsOpen) // Si no se ha abierto un nuevo tooltip, ciérralo
            {
                 _urlHoverTooltip.IsOpen = false;
            }
        }

        /// <summary>
        /// Muestra un Popup (nueva ventana) al hacer clic derecho + Shift en una URL.
        /// </summary>
        /// <param name="url">La URL de la página.</param>
        /// <param name="clientX">Coordenada X del ratón en el WebView2.</param>
        /// <param name="clientY">Coordenada Y del ratón en el WebView2.</param>
        private async Task ShowPopupAsync(string url, int clientX, int clientY)
        {
            var preview = await FetchPreviewAsync(url);

            var win = new Window
            {
                Width = 350,
                Height = 300,
                WindowStyle = WindowStyle.None, // Sin barra de título estándar
                AllowsTransparency = true,
                Background = Brushes.Transparent, // Fondo transparente para esquinas redondeadas
                ResizeMode = ResizeMode.NoResize, // Para un popup, normalmente no se puede redimensionar
                ShowInTaskbar = false, // No mostrar en la barra de tareas
                Topmost = true, // Mantener encima de otras ventanas
                Content = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8), // Esquinas redondeadas
                    Padding = new Thickness(15),
                    BorderBrush = Brushes.DarkGray, // Un borde sutil
                    BorderThickness = new Thickness(1),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = preview.Title, FontWeight = FontWeights.Bold, FontSize = 16, Margin = new Thickness(0,0,0,5), TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Black },
                            new TextBlock { Text = preview.Description, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,10), MaxHeight = 60, TextTrimming = TextTrimming.WordEllipsis, Foreground = Brushes.Gray }
                        }
                    }
                }
            };

            // Añade la miniatura si está disponible
            if (preview.Thumbnail != null)
            {
                (win.Content as Border)?.Child.AddChild(new Image { Source = preview.Thumbnail, Stretch = Stretch.UniformToFill, MaxHeight = 150 });
            }

            // Para cerrar el popup al perder el foco (clic fuera)
            win.Deactivated += (s, e) => win.Close();

            // Posiciona el popup relativo a la ventana del navegador.
            // clientX, clientY son coordenadas dentro del WebView2. Necesitamos convertirlas a coordenadas de pantalla.
            // Primero, obtenemos la posición del WebView2 en la ventana.
            // Para simplificar, si 'BrowserWebView' es el que lanzó el mensaje:
            if (BrowserTabs.SelectedItem is TabItem tabItem && tabItem.Content is Grid tabContentGrid && tabContentGrid.FindName("BrowserWebView") is WebView2 activeWebView)
            {
                Point relativeToWindow = activeWebView.PointToScreen(new Point(clientX, clientY)); // Convertir a coordenadas de pantalla
                win.Left = relativeToWindow.X + 5; // Un pequeño offset
                win.Top = relativeToWindow.Y + 5;
            }
            else
            {
                // Fallback si no podemos encontrar el WebView2 activo (ej. si no es una TabItem con WebView2)
                Point screenMousePos = Mouse.GetPosition(this);
                screenMousePos.X += this.Left;
                screenMousePos.Y += this.Top;
                win.Left = screenMousePos.X + 5;
                win.Top = screenMousePos.Y + 5;
            }

            win.Show();
        }
    }

    // Extensión para añadir elementos a un StackPanel dinámicamente
    // Si usas un StackPanel, puedes usar contentStack.Children.Add(element); directamente.
    // Esta extensión es solo un helper si quisieras una sintaxis más fluida.
    // public static class PanelExtensions
    // {
    //     public static T AddChild<T>(this Panel panel, T child) where T : UIElement
    //     {
    //         panel.Children.Add(child);
    //         return child;
    //     }
    // }
}
