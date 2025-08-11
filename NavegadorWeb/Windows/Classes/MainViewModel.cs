using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NavegadorWeb.Windows; // Para acceder a HistoryWindow
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using NavegadorWeb; // Para acceder a HistoryManager

namespace NavegadorWeb.Classes
{
    // La clase debe ser 'partial' y heredar de 'ObservableObject'
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentUrl))]
        [NotifyPropertyChangedFor(nameof(IsBackEnabled))]
        [NotifyPropertyChangedFor(nameof(IsForwardEnabled))]
        private TabItemData? _selectedTabItem;

        [ObservableProperty]
        private ObservableCollection<TabItemData> _tabs = new();

        public string CurrentUrl => SelectedTabItem?.WebViewInstance?.CoreWebView2?.Source ?? "about:blank";
        public bool IsBackEnabled => SelectedTabItem?.WebViewInstance?.CoreWebView2?.CanGoBack ?? false;
        public bool IsForwardEnabled => SelectedTabItem?.WebViewInstance?.CoreWebView2?.CanGoForward ?? false;

        public MainViewModel()
        {
            AddNewTab();
        }

        [RelayCommand]
        public void AddNewTab()
        {
            var newTab = new TabItemData();
            Tabs.Add(newTab);
            SelectedTabItem = newTab;
        }

        [RelayCommand]
        public void CloseTab(TabItemData tabToClose)
        {
            if (Tabs.Contains(tabToClose))
            {
                int index = Tabs.IndexOf(tabToClose);
                Tabs.Remove(tabToClose);

                // Si se cierra la última pestaña, se abre una nueva
                if (Tabs.Count == 0)
                {
                    AddNewTab();
                }
                else if (SelectedTabItem == tabToClose)
                {
                    // Si se cierra la pestaña seleccionada, se selecciona la siguiente (o la anterior)
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
        }
        
        [RelayCommand]
        public void GoBack()
        {
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.GoBack();
        }

        [RelayCommand]
        public void GoForward()
        {
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.GoForward();
        }

        [RelayCommand]
        public void Refresh()
        {
            SelectedTabItem?.WebViewInstance?.CoreWebView2?.Reload();
        }

        [RelayCommand]
        public void OpenHistoryWindow()
        {
            var historyWindow = new HistoryWindow();
            if (historyWindow.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(historyWindow.SelectedUrl) && SelectedTabItem?.WebViewInstance?.CoreWebView2 != null)
                {
                    SelectedTabItem.WebViewInstance.CoreWebView2.Navigate(historyWindow.SelectedUrl);
                }
            }
        }

        [RelayCommand]
        public void OpenBookmarksWindow()
        {
            var bookmarksWindow = new BookmarksWindow();
            bookmarksWindow.ShowDialog();
        }

        [RelayCommand]
        public void Navigate(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            // Agregar la URL y el título al historial antes de navegar
            HistoryManager.AddHistoryEntry(url, url); // Título temporal, se actualizará después

            SelectedTabItem?.WebViewInstance?.CoreWebView2?.Navigate(url);
        }
    }
}

