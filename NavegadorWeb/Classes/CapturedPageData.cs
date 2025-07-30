using System.ComponentModel;

namespace NavegadorWeb.Classes
{
    public class CapturedPageData : INotifyPropertyChanged
    {
        private string _url = string.Empty;
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

        private string _title = string.Empty;
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

        private string _extractedText = string.Empty;
        public string ExtractedText
        {
            get => _extractedText;
            set
            {
                if (_extractedText != value)
                {
                    _extractedText = value;
                    OnPropertyChanged(nameof(ExtractedText));
                }
            }
        }

        private string _screenshotBase64 = string.Empty;
        public string ScreenshotBase64
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

        private string _faviconBase64 = string.Empty;
        public string FaviconBase64
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
