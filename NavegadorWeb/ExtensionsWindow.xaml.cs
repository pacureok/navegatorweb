using System.Windows;
using System.Windows.Controls;

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para ExtensionsWindow.xaml
    /// </summary>
    public partial class ExtensionsWindow : Window
    {
        private ExtensionManager _extensionManager;

        public ExtensionsWindow(ExtensionManager extensionManager)
        {
            InitializeComponent();
            _extensionManager = extensionManager;
            this.DataContext = _extensionManager; // Enlazar la lista de extensiones
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // No hay lógica específica al cargar, el DataContext ya está establecido
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Guardar el estado de las extensiones al cerrar la ventana
            _extensionManager.SaveExtensionsState();
        }
    }
}
