using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Wpf;
using NavegadorWeb.Services;
using NavegadorWeb.Windows;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;

namespace NavegadorWeb.Classes
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentUrl))]
        [NotifyPropertyChangedFor(nameof(IsBackEnabled))]
        [NotifyPropertyChangedFor(nameof(IsForwardEnabled))]
        private TabItemData? _selectedTabItem;

        [ObservableProperty]
        private ObservableCollection<TabItemData> _tabs = new();

        public string CurrentUrl => SelectedTabItem?.Url ?? "about:blank";
        public bool IsBackEnabled => SelectedTabItem?.WebViewInstance?.CoreWebView2?.CanGoBack ?? false;
        public bool IsForwardEnabled => SelectedTabItem?.WebViewInstance?.CoreWebView2?.CanGoForward ?? false;

        public MainViewModel()
        {
            // El constructor está limpio. La primera pestaña se crea en la vista (MainWindow.xaml.cs).
        }
        
        [RelayCommand]
        private void AddNewTab()
        {
            // La lógica para crear la instancia de WebView2 se maneja en el code-behind de la vista.
            // Este comando existe para que la UI pueda solicitar una nueva pestaña.
            // La vista (MainWindow.xaml.cs) se suscribe al evento y maneja la creación de la UI y el objeto TabItemData.
        }

        [RelayCommand]
        private void CloseTab(TabItemData? tabToClose)
        {
            if (tabToClose == null) return;

            int index = Tabs.IndexOf(tabToClose);
            Tabs.Remove(tabToClose);
            tabToClose.Dispose();

            if (Tabs.Count == 0)
            {
                Application.Current.Shutdown();
            }
            else
            {
                if (index >= Tabs.Count)
                {
                    SelectedTabItem = Tabs.Last();
                }
                else
                {
                    SelectedTabItem = Tabs[index];
                }
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            SelectedTabItem?.WebViewInstance?.GoBack();
        }

        [RelayCommand]
        private void GoForward()
        {
            SelectedTabItem?.WebViewInstance?.GoForward();
        }

        [RelayCommand]
        private void Refresh()
        {
            SelectedTabItem?.WebViewInstance?.Reload();
        }
        
        [RelayCommand]
        private void Navigate(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.Navigate(url);
        }

        [RelayCommand]
        private void OpenHistoryWindow()
        {
            var historyWindow = new HistoryWindow();
            if (historyWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(historyWindow.SelectedUrl))
                {
                    SelectedTabItem?.WebViewInstance?.CoreWebView2?.Navigate(historyWindow.SelectedUrl);
                }
            }
        }

        // Puedes añadir más comandos aquí según las funcionalidades que necesites.
    }
}
