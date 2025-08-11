using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;

namespace NavegadorWeb.Classes
{
    public class TabItemData : INotifyPropertyChanged, IDisposable
    {
        public WebView2 WebViewInstance { get; set; }
        public string HeaderText { get; set; }
        private string _url;
        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public TabItemData()
        {
            HeaderText = "Nueva pestaña";
            _url = "about:blank";
        }
        
        // Propiedad y método Dispose
        public void Dispose()
        {
            // Libera los recursos del WebView2 si es necesario
            if (WebViewInstance != null)
            {
                WebViewInstance.Dispose();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

