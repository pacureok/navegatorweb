using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace NavegadorWeb
{
    public partial class GeminiDataViewerWindow : Window
    {
        private List<AskGeminiWindow.CapturedPageData> _capturedPages;
        private string _userQuestion;
        private CoreWebView2Environment _environment;

        public GeminiDataViewerWindow(List<AskGeminiWindow.CapturedPageData> capturedPages, string userQuestion, CoreWebView2Environment environment)
        {
            InitializeComponent();
            _capturedPages = capturedPages;
            _userQuestion = userQuestion;
            _environment = environment; // Usar el mismo entorno que el navegador principal

            this.Loaded += GeminiDataViewerWindow_Loaded;
        }

        private async void GeminiDataViewerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Asegurar que WebView2 esté inicializado con el entorno adecuado
                await GeminiWebView.EnsureCoreWebView2Async(_environment);

                string geminiDisplayPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeminiIntegrationDisplay.html");
                if (File.Exists(geminiDisplayPath))
                {
                    GeminiWebView.CoreWebView2.Navigate($"file:///{geminiDisplayPath.Replace("\\", "/")}");

                    // Esperar un momento para que el HTML se cargue completamente
                    await Task.Delay(500);

                    // Preparar los datos para enviar al JavaScript de GeminiIntegrationDisplay.html
                    var dataToSend = new
                    {
                        type = "geminiPageData",
                        userQuestion = _userQuestion,
                        capturedPages = _capturedPages.Select(cp => new
                        {
                            url = cp.Url,
                            title = cp.Title,
                            screenshotBase64 = cp.ScreenshotBase64,
                            pageText = cp.PageText,
                            faviconBase64 = cp.FaviconBase64
                        }).ToList()
                    };

                    string jsonMessage = JsonSerializer.Serialize(dataToSend);
                    await GeminiWebView.CoreWebView2.PostWebMessageAsJson(jsonMessage);
                }
                else
                {
                    MessageBox.Show(this, "Error: El archivo 'GeminiIntegrationDisplay.html' no se encontró.", "Error de Archivo", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error al cargar los datos de Gemini: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Limpiar el WebView2 al cerrar la ventana
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            GeminiWebView.Dispose();
        }
    }
}
