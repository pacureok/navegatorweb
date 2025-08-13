using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System;
using Microsoft.Web.WebView2.Wpf;
using NavegadorWeb.Classes;

namespace NavegadorWeb.Windows
{
    public partial class PerformanceMonitorWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<BrowserTabItem> _browserTabs;
        public ObservableCollection<BrowserTabItem> BrowserTabs
        {
            get => _browserTabs;
            set
            {
                if (_browserTabs != value)
                {
                    _browserTabs = value;
                    OnPropertyChanged(nameof(BrowserTabs));
                }
            }
        }

        private DispatcherTimer _updateTimer;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PerformanceMonitorWindow(ObservableCollection<BrowserTabItem> tabs)
        {
            InitializeComponent();
            this.DataContext = this;
            BrowserTabs = tabs;

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            foreach (var tab in BrowserTabs)
            {
                if (tab.LeftWebView != null && tab.LeftWebView.CoreWebView2 != null)
                {
                    tab.LastActivity = DateTime.Now;
                }
            }
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            _updateTimer.Stop();
        }
    }
}
