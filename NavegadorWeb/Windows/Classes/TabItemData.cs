using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace NavegadorWeb.Classes
{
    // Ahora hereda de ObservableObject e implementa IDisposable
    public partial class TabItemData : ObservableObject, IDisposable
    {
        [ObservableProperty]
        private string? _title = "Nueva Pestaña";

        [ObservableProperty]
        private string? _url = "about:blank";

        [ObservableProperty]
        private BitmapImage? _favicon;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isReaderMode;

        [ObservableProperty]
        private bool _isSuspended;

        [ObservableProperty]
        private DateTime _lastActivity = DateTime.Now;

        public string? LastSuspendedUrl { get; set; }

        public WebView2 WebViewInstance { get; }

        // El constructor requiere una instancia de WebView2
        public TabItemData(WebView2 webView)
        {
            WebViewInstance = webView;
        }

        // Método para liberar los recursos del WebView
        public void Dispose()
        {
            WebViewInstance?.Dispose();
        }
    }
}
