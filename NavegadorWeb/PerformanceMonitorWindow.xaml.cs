using System;
using System.Collections.Generic;
using System.Diagnostics; // Necesario para Process
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para PerformanceMonitorWindow.xaml
    /// </summary>
    public partial class PerformanceMonitorWindow : Window
    {
        // Delegado para obtener la lista de pestañas de MainWindow
        public delegate List<MainWindow.BrowserTabItem> GetTabsDelegate();
        private GetTabsDelegate _getTabsCallback;

        public PerformanceMonitorWindow(GetTabsDelegate getTabsCallback)
        {
            InitializeComponent();
            _getTabsCallback = getTabsCallback;
        }

        /// <summary>
        /// Se ejecuta cuando la ventana se ha cargado.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshPerformanceInfo();
        }

        /// <summary>
        /// Actualiza la información de rendimiento mostrada.
        /// </summary>
        private void RefreshPerformanceInfo()
        {
            // Actualizar el uso total de memoria del navegador
            long totalMemoryBytes = Process.GetCurrentProcess().WorkingSet64;
            TotalMemoryUsageTextBlock.Text = FormatBytes(totalMemoryBytes);

            // Actualizar la lista de pestañas
            List<MainWindow.BrowserTabItem> browserTabs = _getTabsCallback?.Invoke();
            if (browserTabs != null)
            {
                var tabPerformanceInfos = new List<TabPerformanceInfo>();
                foreach (var tab in browserTabs)
                {
                    bool isSuspended = (tab.LeftWebView == null && !tab.IsSplit);
                    tabPerformanceInfos.Add(new TabPerformanceInfo(
                        tab.HeaderTextBlock.Text,
                        tab.LeftWebView?.Source?.OriginalString ?? "about:blank",
                        tab.IsIncognito,
                        isSuspended,
                        tab.Tab
                    ));
                }
                TabsPerformanceListView.ItemsSource = tabPerformanceInfos;
            }
        }

        /// <summary>
        /// Formatea un número de bytes a un formato legible (KB, MB, GB).
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = bytes;
            while (Math.Round(dblSByte / 1024) >= 1)
            {
                dblSByte /= 1024;
                i++;
            }
            return string.Format("{0:n1} {1}", dblSByte, Suffix[i]);
        }

        /// <summary>
        /// Maneja el clic en el botón "Actualizar".
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshPerformanceInfo();
        }

        /// <summary>
        /// Cierra la ventana del monitor de rendimiento.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
