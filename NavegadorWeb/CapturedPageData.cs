using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace NavegadorWeb
{
    // Clase para almacenar los datos de una página capturada para Gemini
    public class CapturedPageData : INotifyPropertyChanged
    {
        private string _url;
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

        private string _title;
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

        private string? _screenshotBase64; // Puede ser nulo
        public string? ScreenshotBase64
        {
            get => _screenshotBase64;
            set
            {
                if (_screenshotBase64 != value)
                {
                    _screenshotBase64 = value;
                    OnPropertyChanged(nameof(ScreenshotBase64));
                }
            }
        }

        private string? _pageText; // Puede ser nulo
        public string? PageText
        {
            get => _pageText;
            set
            {
                if (_pageText != value)
                {
                    _pageText = value;
                    OnPropertyChanged(nameof(PageText));
                }
            }
        }

        private string? _faviconBase64; // Puede ser nulo
        public string? FaviconBase64
        {
            get => _faviconBase64;
            set
            {
                if (_faviconBase64 != value)
                {
                    _faviconBase64 = value;
                    OnPropertyChanged(nameof(FaviconBase64));
                }
            }
        }

        // Implementación explícita del evento PropertyChanged para INotifyPropertyChanged
        // Se usa '?' para indicar que el evento puede ser nulo, resolviendo advertencias de nulabilidad.
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
