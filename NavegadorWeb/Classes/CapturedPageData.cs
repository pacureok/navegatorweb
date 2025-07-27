using System.ComponentModel;

namespace NavegadorWeb.Classes
{
    public class CapturedPageData : INotifyPropertyChanged
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public string ScreenshotBase64 { get; set; } = string.Empty;
        public string FaviconBase64 { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
