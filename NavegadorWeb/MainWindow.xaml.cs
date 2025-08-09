using NavegadorWeb.Classes;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
using NavegadorWeb.Windows;

namespace NavegadorWeb
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Crea la primera pestaña al iniciar
            ViewModel.NewTabCommand.Execute(null);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            ViewModel.SaveSessionCommand.Execute(null);
        }

        private void BrowserTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem selectedTabItem)
            {
                if (selectedTabItem.DataContext is TabItemData tabData)
                {
                    ViewModel.SelectedTabItem = tabData;
                }
            }
        }
    }
}
