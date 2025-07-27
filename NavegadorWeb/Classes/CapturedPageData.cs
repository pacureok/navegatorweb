// This file defines a class to hold captured data from a web page,
// intended for use with AI features like Gemini.

using System.ComponentModel;

namespace NavegadorWeb.Classes
{
    /// <summary>
    /// Represents data captured from a web page, including URL, title,
    /// extracted text, and Base64 encoded screenshots/favicons.
    /// Implements INotifyPropertyChanged for UI updates.
    /// </summary>
    public class CapturedPageData : INotifyPropertyChanged
    {
        // Public properties for captured page data
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public string ScreenshotBase64 { get; set; } = string.Empty; // Base64 encoded image data
        public string FaviconBase64 { get; set; } = string.Empty;   // Base64 encoded favicon data

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
