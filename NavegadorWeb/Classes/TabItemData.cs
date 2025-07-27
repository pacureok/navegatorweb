using Microsoft.Web.WebView2.Wpf;
using System;
using System.ComponentModel;
using System.Windows.Media.Imaging; // Necesario para BitmapImage
using NavegadorWeb.Classes; // Para CapturedPageData

namespace NavegadorWeb.Classes
{
    public class TabItemData : INotifyPropertyChanged
    {
        private string _title = "Nueva Pestaña";
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        private string _url = "about:blank";
        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged(nameof(Url));
                }
            }
        }

        private BitmapImage? _favicon;
        public BitmapImage? Favicon
        {
            get => _favicon;
            set
            {
                if (_favicon != value)
                {
                    _favicon = value;
                    OnPropertyChanged(nameof(Favicon));
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        private bool _isReaderMode;
        public bool IsReaderMode
        {
            get => _isReaderMode;
            set
            {
                if (_isReaderMode != value)
                {
                    _isReaderMode = value;
                    OnPropertyChanged(nameof(IsReaderMode));
                }
            }
        }

        private bool _isSuspended;
        public bool IsSuspended
        {
            get => _isSuspended;
            set
            {
                if (_isSuspended != value)
                {
                    _isSuspended = value;
                    OnPropertyChanged(nameof(IsSuspended));
                }
            }
        }

        public string? LastSuspendedUrl { get; set; }

        public WebView2 WebViewInstance { get; }

        public CapturedPageData CapturedData { get; set; }

        public TabItemData(WebView2 webView)
        {
            WebViewInstance = webView;
            CapturedData = new CapturedPageData();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
