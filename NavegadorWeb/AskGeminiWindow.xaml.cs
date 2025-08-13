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

            // Asegurarse de que el icono de Gemini se establezca en todas las pestañas para que pueda ser referenciado.
            // La visibilidad se controlará por IsSelectedForGemini en MainWindow.
            foreach (var tab in _allBrowserTabs)
            {
                if (tab.GeminiFeatureIcon == null)
                {
                    // This is a simplified way to create the icon. The actual icon source and its properties
                    // need to be handled correctly in your project.
                    // For now, this placeholder ensures the code compiles.
                    tab.GeminiFeatureIcon = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 5, 0) };
                    // It's assumed that GeminiIconSource is a property on BrowserTabItem that provides the image source.
                    tab.GeminiFeatureIcon.Source = tab.GeminiIconSource; 
                    tab.GeminiFeatureIcon.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SendToGeminiButton_Click(object sender, RoutedEventArgs e)
        {
            // ... (rest of the logic) ...
            string summaryMessage = $"Se ha preparado la siguiente información para enviar a Gemini:\n\n" +
                                    $"Pregunta: \"{UserQuestion}\"\n\n" +
                                    $"Páginas seleccionadas:\n";
            foreach (var data in CapturedData)
            {
                summaryMessage += $"- {data.Item1} (Contenido: {data.Item3?.Length} caracteres, Captura: {data.Item2?.Length} bytes)\n";
            }
            summaryMessage += "\nEsta aplicación de demostración no tiene acceso directo a la API de Gemini para enviar y procesar esta información. En una aplicación real, estos datos se enviarían a la API de Gemini para obtener una respuesta.";

            MessageBox.Show(summaryMessage, "Datos Listos para Gemini (Simulación)", MessageBoxButton.OK, MessageBoxImage.Information);

            this.DialogResult = true; // Indicar que se ha "enviado" (simulado)
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indicar cancelación
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Limpiar las selecciones al cerrar la ventana
            foreach (var tab in _allBrowserTabs)
            {
                tab.IsSelectedForGemini = false; // Esto también ocultará el icono de Gemini
            }
        }
    }
}
