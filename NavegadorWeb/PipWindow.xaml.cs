using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Windows;
using System.Windows.Input;
using System.IO;

namespace NavegadorWeb
{
    public partial class PipWindow : Window
    {
        private string _videoUrl;
        private CoreWebView2Environment? _environment;

        public PipWindow(string videoUrl, CoreWebView2Environment? environment = null)
        {
            InitializeComponent();
            _videoUrl = videoUrl;
            _environment = environment;
            this.Topmost = true;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_environment != null)
                {
                    await pipWebView.EnsureCoreWebView2Async(_environment);
                }
                else
                {
                    string tempUserDataFolder = Path.Combine(Path.GetTempPath(), "AuroraBrowserPip", Guid.NewGuid().ToString());
                    CoreWebView2Environment newEnvironment = await CoreWebView2Environment.CreateAsync(null, tempUserDataFolder);
                    await pipWebView.EnsureCoreWebView2Async(newEnvironment);
                }

                if (pipWebView.CoreWebView2 != null)
                {
                    pipWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    pipWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                    pipWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                    pipWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                    pipWebView.CoreWebView2.Navigate(_videoUrl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el contenido en la ventana PIP: {ex.Message}", "Error PIP", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            pipWebView?.Dispose();
            this.Close();
        }
    }
}
