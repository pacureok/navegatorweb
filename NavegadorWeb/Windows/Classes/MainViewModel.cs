using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NavegadorWeb.Classes;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System;
using System.IO;
using System.Text.Json;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows.Input;

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
            // The constructor is clean. The first tab is created in the view (MainWindow.xaml.cs).
        }
        
        [RelayCommand]
        private void AddNewTab()
        {
            // The logic to create the WebView2 instance is handled in the view's code-behind.
            // This command exists so the UI can request a new tab.
            // The view (MainWindow.xaml.cs) subscribes to the event and handles the creation of the UI and the TabItemData object.
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
    }
}
