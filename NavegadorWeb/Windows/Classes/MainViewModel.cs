using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Core;
using NavegadorWeb.Services;
using NavegadorWeb.Classes;
using NavegadorWeb.Windows;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Speech.Synthesis;

namespace NavegadorWeb
{
    public partial class MainViewModel : ObservableObject
    {
        private TabItemData _selectedTabItem;
        public TabItemData SelectedTabItem
        {
            get => _selectedTabItem;
            set
            {
                if (SetProperty(ref _selectedTabItem, value))
                {
                    OnPropertyChanged(nameof(CurrentUrl));
                    OnPropertyChanged(nameof(IsBackEnabled));
                    OnPropertyChanged(nameof(IsForwardEnabled));
                }
            }
        }

        public string CurrentUrl => SelectedTabItem?.WebView?.CoreWebView2?.Source;
        public bool IsBackEnabled => SelectedTabItem?.WebView?.CoreWebView2?.CanGoBack ?? false;
        public bool IsForwardEnabled => SelectedTabItem?.WebView?.CoreWebView2?.CanGoForward ?? false;

        public ObservableCollection<TabItemData> Tabs { get; set; } = new ObservableCollection<TabItemData>();

        // Comandos con atributos
        public IRelayCommand NavigateCommand { get; }
        public IRelayCommand NewTabCommand { get; }
        public IRelayCommand CloseTabCommand { get; }
        public IRelayCommand GoBackCommand { get; }
        public IRelayCommand GoForwardCommand { get; }
        public IRelayCommand RefreshCommand { get; }
        public IRelayCommand OpenHistoryCommand { get; }
        public IRelayCommand OpenBookmarksCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(url => Navigate(url?.ToString()));
            NewTabCommand = new RelayCommand(AddNewTab);
            CloseTabCommand = new RelayCommand(tab => CloseTab((TabItemData)tab));
            GoBackCommand = new RelayCommand(() => GoBack());
            GoForwardCommand = new RelayCommand(() => GoForward());
            RefreshCommand = new RelayCommand(() => Refresh());
            OpenHistoryCommand = new RelayCommand(OpenHistoryWindow);
            OpenBookmarksCommand = new RelayCommand(OpenBookmarksWindow);

            AddNewTab();
        }

        private void Navigate(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            SelectedTabItem.WebView?.CoreWebView2?.Navigate(url);
        }

        public void AddNewTab()
        {
            var newTab = new TabItemData();
            Tabs.Add(newTab);
            SelectedTabItem = newTab;
        }

        private void CloseTab(TabItemData tab)
        {
            if (Tabs.Count == 1)
            {
                Application.Current.Shutdown();
                return;
            }
            int index = Tabs.IndexOf(tab);
            Tabs.Remove(tab);
            if (index > 0)
            {
                SelectedTabItem = Tabs[index - 1];
            }
            else
            {
                SelectedTabItem = Tabs[0];
            }
        }

        private void GoBack()
        {
            SelectedTabItem.WebView?.CoreWebView2?.GoBack();
        }

        private void GoForward()
        {
            SelectedTabItem.WebView?.CoreWebView2?.GoForward();
        }

        private void Refresh()
        {
            SelectedTabItem.WebView?.CoreWebView2?.Reload();
        }

        private void OpenHistoryWindow()
        {
            var historyWindow = new HistoryWindow();
            if (historyWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(historyWindow.SelectedUrl))
                {
                    SelectedTabItem.WebView?.CoreWebView2?.Navigate(historyWindow.SelectedUrl);
                }
            }
        }

        private void OpenBookmarksWindow()
        {
            var bookmarksWindow = new BookmarksWindow();
            bookmarksWindow.ShowDialog();
        }
    }
}
