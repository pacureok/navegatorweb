using Microsoft.Web.WebView2.Wpf;
using System;
using System.Windows;

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para PipWindow.xaml
    /// </summary>
    public partial class PipWindow : Window
    {
        private WebView2 _originalWebView; // Referencia al WebView2 original de donde se extrajo el video

        public PipWindow(string videoUrl, WebView2 originalWebView)
        {
            InitializeComponent();
            _originalWebView = originalWebView; // Guardar la referencia al WebView original
            InitializePipWebView(videoUrl);
        }

        private async void InitializePipWebView(string videoUrl)
        {
            // Asegurarse de que el WebView2 esté inicializado
            await PipWebView.EnsureCoreWebView2Async(null);

            // Navegar al video. Para YouTube, a menudo es mejor usar la URL de embebido.
            // Esto es una simplificación; en un caso real, necesitarías parsear la URL
            // para obtener la URL de embebido correcta para diferentes plataformas.
            if (videoUrl.Contains("youtube.com/watch?v="))
            {
                string videoId = videoUrl.Split(new string[] { "v=" }, StringSplitOptions.None)[1].Split('&')[0];
                PipWebView.Source = new Uri($"https://www.youtube.com/embed/{videoId}?autoplay=1&controls=1");
            }
            else
            {
                // Para otros videos, intentar navegar directamente.
                // Esto podría no funcionar para todos los reproductores web.
                PipWebView.Source = new Uri(videoUrl);
            }

            // Opcional: Deshabilitar algunas interacciones en el WebView2 PIP si no son necesarias
            PipWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            PipWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            PipWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            PipWebView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
            PipWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            PipWebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            PipWebView.CoreWebView2.Settings.IsPinchZoomEnabled = false;
            PipWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        }

        /// <summary>
        /// Maneja el evento de cierre de la ventana PIP.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Opcional: Reactivar el video en la pestaña original si se cerró el PIP
            // Esto requeriría que el JavaScript en la página original sepa cómo manejarlo.
            // Por simplicidad, solo pausamos el video en la página original al abrir el PIP.
            // Aquí, simplemente nos aseguramos de que el WebView2 se deseche correctamente.
            PipWebView.Dispose();
        }
    }
}
