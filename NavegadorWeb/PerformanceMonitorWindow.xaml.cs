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

        // Implementación explícita del evento PropertyChanged para INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged; // Se añadió '?' para nulabilidad

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PerformanceMonitorWindow(ObservableCollection<BrowserTabItem> tabs)
        {
            InitializeComponent();
            this.DataContext = this;
            BrowserTabs = tabs; // Asigna la colección de pestañas pasada

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e) // Se añadió '?'
        {
            // Actualiza la información de rendimiento de cada pestaña
            foreach (var tab in BrowserTabs)
            {
                if (tab.LeftWebView != null && tab.LeftWebView.CoreWebView2 != null)
                {
                    // Ejemplo de cómo podrías obtener y actualizar datos de rendimiento
                    // Nota: WebView2 no expone directamente el uso de CPU/RAM de forma granular por pestaña.
                    // Esto es más un placeholder o para mostrar información general.
                    // Para datos reales, necesitarías monitorear procesos externos o usar APIs más avanzadas.
                    tab.LastActivity = DateTime.Now; // Simplemente actualiza la hora de actividad
                    // tab.CpuUsage = GetCpuUsageForWebView(tab.LeftWebView); // Esto requeriría lógica compleja
                    // tab.MemoryUsage = GetMemoryUsageForWebView(tab.LeftWebView); // Esto requeriría lógica compleja
                }
            }
        }

        private void Window_Closing(object? sender, CancelEventArgs e) // Se añadió '?'
        {
            _updateTimer.Stop();
        }
    }
}
