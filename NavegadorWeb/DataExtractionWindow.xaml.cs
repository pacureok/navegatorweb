using Microsoft.Web.WebView2.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq; // Necesario para .Any()

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para DataExtractionWindow.xaml
    /// </summary>
    public partial class DataExtractionWindow : Window
    {
        // Delegado para obtener el WebView2 principal de la ventana principal
        public delegate WebView2 GetCurrentWebViewDelegate();
        private GetCurrentWebViewDelegate _getCurrentWebViewCallback;

        public DataExtractionWindow(GetCurrentWebViewDelegate getCurrentWebViewCallback)
        {
            InitializeComponent();
            _getCurrentWebViewCallback = getCurrentWebViewCallback;
        }

        /// <summary>
        /// Maneja el clic en el botón "Extraer Datos".
        /// </summary>
        private async void ExtractButton_Click(object sender, RoutedEventArgs e)
        {
            WebView2 currentWebView = _getCurrentWebViewCallback?.Invoke();
            if (currentWebView == null || currentWebView.CoreWebView2 == null)
            {
                MessageBox.Show("No hay una página activa para extraer datos.", "Error de Extracción", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string extractedData = string.Empty;
            string script = string.Empty;

            try
            {
                switch (ExtractionTypeComboBox.SelectedIndex)
                {
                    case 0: // Texto Principal
                        script = @"
                            (function() {
                                let text = '';
                                let mainContent = document.querySelector('article, main, .post-content, .entry-content, #content, #main');

                                if (mainContent) {
                                    text = mainContent.innerText || mainContent.textContent;
                                } else {
                                    // Fallback a todo el cuerpo si no se encuentra contenido principal
                                    text = document.body.innerText || document.body.textContent;
                                }

                                // Limpiar el texto (eliminar espacios en blanco excesivos, saltos de línea)
                                text = text.replace(/(\r\n|\n|\r)/gm, '\n').replace(/\s{2,}/g, ' ').trim();
                                return text;
                            })();
                        ";
                        break;
                    case 1: // Todos los Enlaces
                        script = @"
                            (function() {
                                let links = [];
                                document.querySelectorAll('a').forEach(link => {
                                    let href = link.href;
                                    let text = link.innerText || link.textContent;
                                    if (href && href.startsWith('http')) { // Solo enlaces HTTP/HTTPS válidos
                                        links.push(`${text.trim()} -> ${href}`);
                                    }
                                });
                                return links.join('\n');
                            })();
                        ";
                        break;
                    default:
                        MessageBox.Show("Tipo de extracción no válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                }

                // Ejecutar el script y obtener el resultado
                string jsonResult = await currentWebView.CoreWebView2.ExecuteScriptAsync(script);
                extractedData = System.Text.Json.JsonSerializer.Deserialize<string>(jsonResult);

                ExtractedDataTextBox.Text = extractedData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al extraer datos: {ex.Message}", "Error de Extracción", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Copia el texto extraído al portapapeles.
        /// </summary>
        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ExtractedDataTextBox.Text))
            {
                Clipboard.SetText(ExtractedDataTextBox.Text);
                MessageBox.Show("Datos copiados al portapapeles.", "Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No hay datos para copiar.", "Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Cierra la ventana.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
