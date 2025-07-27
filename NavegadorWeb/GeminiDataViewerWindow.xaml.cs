using System.Configuration;
using System.Windows;
using System.Collections.ObjectModel;
using NavegadorWeb.Classes; // Para CapturedPageData
using NavegadorWeb.Services; // Para LanguageService

namespace NavegadorWeb
{
    public partial class GeminiDataViewerWindow : Window
    {
        public ObservableCollection<CapturedPageData> CapturedData { get; set; }
        public string UserQuestion { get; set; } = string.Empty;

        public GeminiDataViewerWindow(ObservableCollection<CapturedPageData> capturedData)
        {
            InitializeComponent();
            CapturedData = capturedData;
            this.DataContext = this;
        }

        private async void SendToGeminiButton_Click(object sender, RoutedEventArgs e)
        {
            string preferredLang = ConfigurationManager.AppSettings["UserPreferredLanguage"] ?? "es";

            foreach (var data in CapturedData)
            {
                string originalLang = await LanguageService.DetectPageLanguageAsync(data.WebView.CoreWebView2);
                if (originalLang != preferredLang)
                {
                    data.ExtractedText = await LanguageService.TranslateTextAsync(data.ExtractedText, preferredLang);
                }
            }

            if (preferredLang != "en")
            {
                UserQuestion = await LanguageService.TranslateTextAsync(UserQuestion, "en");
            }

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
