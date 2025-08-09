using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Wpf;
using NavegadorWeb.Services;
using NavegadorWeb.Windows;
using System.Collections.ObjectModel;
using System.Windows;

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
            // El ViewModel no debe crear pestañas al inicio. Esa lógica va en MainWindow.xaml.cs
            // La primera pestaña se creará cuando la ventana principal se cargue.
        }

        [RelayCommand]
        public void CloseTab(TabItemData? tabToClose)
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
                    SelectedTabItem = Tabs[^1];
                }
                else
                {
                    SelectedTabItem = Tabs[index];
                }
            }
        }

        [RelayCommand]
        public void GoBack()
        {
            SelectedTabItem?.WebViewInstance?.GoBack();
        }

        [RelayCommand]
        public void GoForward()
        {
            SelectedTabItem?.WebViewInstance?.GoForward();
        }

        [RelayCommand]
        public void Refresh()
        {
            SelectedTabItem?.WebViewInstance?.Reload();
        }
        
        [RelayCommand]
        public void Navigate(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.Navigate(url);
        }

        [RelayCommand]
        public void OpenHistoryWindow()
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
    }
}
