using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace NavegadorWeb.Classes
{
    // La clase ahora implementa IDisposable
    public partial class TabItemData : ObservableObject, IDisposable
    {
        public WebView2 WebViewInstance { get; }

        private string? _title;
        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public TabItemData(WebView2 webView)
        {
            WebViewInstance = webView;
        }

        // Implementación del método Dispose() para liberar los recursos del WebView
        public void Dispose()
        {
            WebViewInstance?.Dispose();
        }
    }
}
