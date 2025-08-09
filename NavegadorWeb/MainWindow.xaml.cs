using NavegadorWeb.Classes;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;

namespace NavegadorWeb.Windows
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }
        
        // Maneja la creación de una nueva pestaña desde la interfaz de usuario
        public void AddNewTab_Click(object sender, RoutedEventArgs e)
        {
            var newWebView = new WebView2();
            var newTabItemData = new TabItemData(newWebView);

            var newTabItem = new TabItem
            {
                Header = newTabItemData.Title,
                Content = newWebView,
                DataContext = newTabItemData
            };

            _viewModel.Tabs.Add(newTabItemData);
            _viewModel.SelectedTabItem = newTabItemData;

            // Asigna los eventos de navegación aquí
            newWebView.NavigationCompleted += (s, ev) =>
            {
                if (s is WebView2 webView)
                {
                    newTabItemData.Title = webView.CoreWebView2.DocumentTitle;
                    newTabItemData.Url = webView.Source;
                }
            };
        }

        // Maneja el cambio de pestaña para actualizar el ViewModel
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem tabItem)
            {
                _viewModel.SelectedTabItem = tabItem.DataContext as TabItemData;
            }
        }
    }
}
