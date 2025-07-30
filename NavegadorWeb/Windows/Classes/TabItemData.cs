using Microsoft.Web.WebView2.Wpf;
using System;
using System.ComponentModel;
using System.Windows.Media.Imaging; // Required for BitmapImage
using NavegadorWeb.Classes; // For CapturedPageData

namespace NavegadorWeb.Classes
{
    /// <summary>
    /// Represents the data and state for a single browser tab.
    /// Implements INotifyPropertyChanged to enable data binding in the UI.
    /// </summary>
    public class TabItemData : INotifyPropertyChanged
    {
        // Private fields for tab properties
        private string _title = "Nueva Pestaña";
        private string _url = "about:blank";
        private BitmapImage? _favicon;
        private bool _isLoading;
        private bool _isReaderMode;
        private bool _isSuspended;
        private DateTime _lastActivity = DateTime.Now; // New property to track last activity for suspension

        // Public properties with change notification
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

        public DateTime LastActivity // Property for tracking user activity
        {
            get => _lastActivity;
            set
            {
                if (_lastActivity != value)
                {
                    _lastActivity = value;
                    OnPropertyChanged(nameof(LastActivity));
                }
            }
        }

        public string? LastSuspendedUrl { get; set; } // Stores URL when tab is suspended

        public WebView2 WebViewInstance { get; } // The actual WebView2 control for this tab

        public CapturedPageData CapturedData { get; set; } // Data captured from the page for AI features

        /// <summary>
        /// Initializes a new instance of the TabItemData class.
        /// </summary>
        /// <param name="webView">The WebView2 instance associated with this tab.</param>
        public TabItemData(WebView2 webView)
        {
            WebViewInstance = webView;
            CapturedData = new CapturedPageData(); // Initialize captured data
        }

        // Event for property change notification
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
