using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NavegadorWeb.Classes;

namespace NavegadorWeb.Windows
{
    /// <summary>
    /// Lógica de interacción para TabManagerWindow.xaml
    /// </summary>
    public partial class TabManagerWindow : Window
    {
        public delegate List<BrowserTabItem> GetTabsDelegate();
        public delegate void CloseTabDelegate(TabItem tabToClose);
        public delegate TabItem GetActiveTabDelegate();

        private GetTabsDelegate _getTabsCallback;
        private CloseTabDelegate _closeTabCallback;
        private GetActiveTabDelegate _getActiveTabCallback;

        public TabManagerWindow(GetTabsDelegate getTabsCallback, CloseTabDelegate closeTabCallback, GetActiveTabDelegate getActiveTabCallback)
        {
            InitializeComponent();
            _getTabsCallback = getTabsCallback;
            _closeTabCallback = closeTabCallback;
            _getActiveTabCallback = getActiveTabCallback;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTabsList();
        }

        private void RefreshTabsList()
        {
            TabsListView.ItemsSource = _getTabsCallback();
        }

        private void CloseSelectedTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (TabsListView.SelectedItem is BrowserTabItem selectedTab)
            {
                _closeTabCallback?.Invoke(selectedTab.Tab);
                RefreshTabsList();
            }
            else
            {
                MessageBox.Show("Por favor, selecciona una pestaña para cerrar.", "Ninguna Pestaña Seleccionada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseAllInactiveTabsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres cerrar todas las pestañas inactivas (no la actual)?", "Confirmar Cierre", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                TabItem activeTab = _getActiveTabCallback?.Invoke();
                List<BrowserTabItem> tabsToClose = _getTabsCallback()
                    .Where(t => t.Tab != activeTab && !t.IsSplit)
                    .ToList();

                foreach (var tab in tabsToClose)
                {
                    _closeTabCallback?.Invoke(tab.Tab);
                }
                RefreshTabsList();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
