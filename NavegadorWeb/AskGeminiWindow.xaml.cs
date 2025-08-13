using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using System.IO;
using NavegadorWeb.Classes;

namespace NavegadorWeb.Windows
{
    public partial class AskGeminiWindow : Window
    {
        private ObservableCollection<BrowserTabItem> _allBrowserTabs;
        public List<Tuple<string, byte[], string>> CapturedData { get; private set; } // URL, ScreenshotBytes, PageText
        public string UserQuestion { get; private set; } = string.Empty;

        public AskGeminiWindow(ObservableCollection<BrowserTabItem> allBrowserTabs)
        {
            InitializeComponent();
            _allBrowserTabs = allBrowserTabs;
            PagesListView.ItemsSource = _allBrowserTabs;
        }

        private void SendToGeminiButton_Click(object sender, RoutedEventArgs e)
        {
            // Lógica de simulación para enviar a Gemini
            string summaryMessage = $"Se ha preparado la siguiente información para enviar a Gemini:\n\n" +
                                    $"Pregunta: \"{UserQuestion}\"\n\n" +
                                    $"Páginas seleccionadas:\n";
            // ... (resto de la lógica) ...
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            foreach (var tab in _allBrowserTabs)
            {
                tab.IsSelectedForGemini = false;
            }
        }
    }
}
