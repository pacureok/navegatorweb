using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Windows;

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para PdfViewerWindow.xaml
    /// </summary>
    public partial class PdfViewerWindow : Window
    {
        private string _pdfUrl;
        private CoreWebView2Environment _environment; // Para usar el mismo entorno que el navegador principal

        public PdfViewerWindow(string pdfUrl, CoreWebView2Environment environment)
        {
            InitializeComponent();
            _pdfUrl = pdfUrl;
            _environment = environment;
        }

        /// <summary>
        /// Se ejecuta cuando la ventana se ha cargado.
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Asegurarse de que el WebView2 esté inicializado con el entorno proporcionado
                await PdfWebView.EnsureCoreWebView2Async(_environment);

                // Configurar ajustes para una mejor experiencia de PDF (zoom, etc.)
                PdfWebView.CoreWebView2.Settings.IsPinchZoomEnabled = true;
                PdfWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;
                PdfWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false; // Deshabilitar atajos de teclado del navegador si interfieren

                // Navegar a la URL del PDF
                PdfWebView.Source = new Uri(_pdfUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el PDF: {ex.Message}", "Error del Visor de PDF", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close(); // Cerrar la ventana si hay un error
            }
        }

        /// <summary>
        /// Maneja el evento de cierre de la ventana.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Desechar el WebView2 para liberar recursos
            PdfWebView.Dispose();
        }
    }
}
