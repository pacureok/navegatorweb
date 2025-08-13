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
        // Delegado para obtener la lista de pestañas de MainWindow
        public delegate List<BrowserTabItem> GetTabsDelegate();
        // Delegado para cerrar una pestaña específica en MainWindow
        public delegate void CloseTabDelegate(TabItem tabToClose);
        // Delegado para obtener la pestaña activa de MainWindow
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

        /// <summary>
        /// Carga las pestañas cuando la ventana se ha cargado.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshTabsList();
        }

        /// <summary>
        /// Actualiza la lista de pestañas mostrada en la ListView.
        /// </summary>
        private void RefreshTabsList()
        {
            TabsListView.ItemsSource = _getTabsCallback();
        }

        /// <summary>
        /// Cierra la pestaña seleccionada.
        /// </summary>
        private void CloseSelectedTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (TabsListView.SelectedItem is BrowserTabItem selectedTab)
            {
                _closeTabCallback?.Invoke(selectedTab.Tab);
                RefreshTabsList(); // Actualizar la lista después de cerrar
            }
            else
            {
                MessageBox.Show("Por favor, selecciona una pestaña para cerrar.", "Ninguna Pestaña Seleccionada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Cierra todas las pestañas que no están activas y no están en modo dividido.
        /// </summary>
        private void CloseAllInactiveTabsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres cerrar todas las pestañas inactivas (no la actual)?", "Confirmar Cierre", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                TabItem activeTab = _getActiveTabCallback();
                List<BrowserTabItem> tabsToClose = _getTabsCallback()
                    .Where(t => t.Tab != activeTab && !t.IsSplit) // No cerrar la activa ni las divididas
                    .ToList();

                foreach (var tab in tabsToClose)
                {
                    _closeTabCallback?.Invoke(tab.Tab);
                }
                RefreshTabsList(); // Actualizar la lista después de cerrar
            }
        }

        /// <summary>
        /// Cierra la ventana del administrador de pestañas.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
