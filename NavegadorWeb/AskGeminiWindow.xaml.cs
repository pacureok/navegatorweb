using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System.IO;

namespace NavegadorWeb
{
    public partial class AskGeminiWindow : Window
    {
        private ObservableCollection<BrowserTabItem> _allBrowserTabs;
        public List<Tuple<string, byte[], string>> CapturedData { get; private set; } // URL, ScreenshotBytes, PageText
        public string UserQuestion { get; private set; }

        public AskGeminiWindow(ObservableCollection<BrowserTabItem> allBrowserTabs)
        {
            InitializeComponent();
            _allBrowserTabs = allBrowserTabs;
            PagesListView.ItemsSource = _allBrowserTabs;

            // Asegurarse de que el icono de Gemini se establezca en todas las pestañas para que pueda ser referenciado.
            // La visibilidad se controlará por IsSelectedForGemini en MainWindow.
            foreach (var tab in _allBrowserTabs)
            {
                if (tab.GeminiFeatureIcon == null)
                {
                    tab.GeminiFeatureIcon = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0) };
                    tab.GeminiFeatureIcon.Source = tab.GeminiIconSource; // Asignar la fuente del icono fijo
                    tab.GeminiFeatureIcon.Visibility = Visibility.Collapsed; // Por defecto oculto
                    // Asumiendo que el Header es un DockPanel, insertamos el icono después del favicon y audio
                    DockPanel headerPanel = tab.Tab.Header as DockPanel;
                    if (headerPanel != null && headerPanel.Children.Count >= 2) // Si hay favicon y audio icon
                    {
                        headerPanel.Children.Insert(2, tab.GeminiFeatureIcon); // Insertar después del favicon y audio icon
                    }
                    else if (headerPanel != null && headerPanel.Children.Count >= 1) // Si solo hay favicon
                    {
                        headerPanel.Children.Insert(1, tab.GeminiFeatureIcon); // Insertar después del favicon
                    }
                    else if (headerPanel != null) // Si no hay iconos, agregarlo al principio
                    {
                        headerPanel.Children.Insert(0, tab.GeminiFeatureIcon);
                    }
                }
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            UserQuestion = QuestionTextBox.Text;
            CapturedData = new List<Tuple<string, byte[], string>>();
            bool anyPageSelected = false;

            foreach (var tab in _allBrowserTabs)
            {
                if (tab.IsSelectedForGemini)
                {
                    anyPageSelected = true;
                    if (tab.LeftWebView != null && tab.LeftWebView.CoreWebView2 != null)
                    {
                        string url = tab.LeftWebView.Source;
                        byte[] screenshotBytes = null;
                        string pageText = null;

                        try
                        {
                            // Captura de pantalla
                            using (MemoryStream stream = new MemoryStream())
                            {
                                await tab.LeftWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
                                screenshotBytes = stream.ToArray();
                            }

                            // Extracción de texto
                            string script = @"document.body.innerText || document.body.textContent;";
                            string textResultJson = await tab.LeftWebView.CoreWebView2.ExecuteScriptAsync(script);
                            pageText = System.Text.Json.JsonSerializer.Deserialize<string>(textResultJson);

                            CapturedData.Add(Tuple.Create(url, screenshotBytes, pageText));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error al capturar datos de la página {url}: {ex.Message}", "Error de Captura", MessageBoxButton.OK, MessageBoxImage.Error);
                            // Considerar si queremos continuar o abortar si falla una captura.
                        }
                    }
                    else
                    {
                        MessageBox.Show($"La pestaña '{tab.HeaderTextBlock.Text}' no está completamente cargada o es una pestaña suspendida/dividida y no se pueden capturar datos.", "Pestaña No Disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }

            if (!anyPageSelected)
            {
                MessageBox.Show("Por favor, selecciona al menos una página para enviar a Gemini.", "Páginas No Seleccionadas", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(UserQuestion))
            {
                MessageBox.Show("Por favor, escribe tu pregunta para Gemini.", "Pregunta Vacía", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // SIMULACIÓN DE ENVÍO A GEMINI
            string summaryMessage = $"Se ha preparado la siguiente información para enviar a Gemini:\n\n" +
                                    $"Pregunta: \"{UserQuestion}\"\n\n" +
                                    $"Páginas seleccionadas:\n";
            foreach (var data in CapturedData)
            {
                summaryMessage += $"- {data.Item1} (Contenido: {data.Item3?.Length} caracteres, Captura: {data.Item2?.Length} bytes)\n";
            }
            summaryMessage += "\nEsta aplicación de demostración no tiene acceso directo a la API de Gemini para enviar y procesar esta información. En una aplicación real, estos datos se enviarían a la API de Gemini para obtener una respuesta.";

            MessageBox.Show(summaryMessage, "Datos Listos para Gemini (Simulación)", MessageBoxButton.OK, MessageBoxImage.Information);

            this.DialogResult = true; // Indicar que se ha "enviado" (simulado)
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indicar cancelación
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Limpiar las selecciones al cerrar la ventana
            foreach (var tab in _allBrowserTabs)
            {
                tab.IsSelectedForGemini = false; // Esto también ocultará el icono de Gemini
            }
        }
    }
}
