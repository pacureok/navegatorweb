using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NavegadorWeb
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
            List<BrowserTabItem> browserTabs = _getTabsCallback?.Invoke();
            if (browserTabs != null)
            {
                var tabInfos = new List<TabInfo>();
                TabItem activeTab = _getActiveTabCallback?.Invoke();

                foreach (var tab in browserTabs)
                {
                    // Determinar si la pestaña está suspendida
                    bool isSuspended = (tab.LeftWebView == null && !tab.IsSplit); // Si no hay LeftWebView y no está dividida, está suspendida

                    // Si está dividida, el RightWebView también podría estar suspendido si se implementa
                    // Para esta versión, solo consideramos el LeftWebView para la suspensión general de la pestaña
                    // y el estado "Activa" si es la pestaña seleccionada en el TabControl.

                    tabInfos.Add(new TabInfo(
                        tab.HeaderTextBlock.Text, // Usar el título visible en el encabezado
                        tab.LeftWebView?.Source?.OriginalString ?? "about:blank", // URL del WebView izquierdo
                        tab.IsIncognito,
                        isSuspended,
                        tab.Tab
                    ));
                }
                TabsListView.ItemsSource = tabInfos;
            }
        }

        /// <summary>
        /// Cierra la pestaña seleccionada en la lista.
        /// </summary>
        private void CloseSelectedTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (TabsListView.SelectedItem is TabInfo selectedTabInfo)
            {
                // No permitir cerrar la última pestaña si es la única activa y no hay otras
                if (_getTabsCallback().Count == 1 && selectedTabInfo.AssociatedTabItem == _getActiveTabCallback())
                {
                    MessageBox.Show("No puedes cerrar la última pestaña activa. Abre una nueva pestaña primero si deseas cerrar esta.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _closeTabCallback?.Invoke(selectedTabInfo.AssociatedTabItem);
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
                TabItem activeTab = _getActiveTabCallback?.Invoke();
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
