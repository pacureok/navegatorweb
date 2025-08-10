using NavegadorWeb.Classes;
using NavegadorWeb.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NavegadorWeb.Windows
{
    /// <summary>
    /// Lógica de interacción para HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow : Window
    {
        // Propiedad para que MainWindow pueda acceder a la URL seleccionada
        // Se inicializa con una cadena vacía para evitar errores de nulidad (CS8618).
        public string SelectedUrl { get; private set; } = string.Empty;

        public HistoryWindow()
        {
            // Este método se genera automáticamente y enlaza el archivo XAML con el código.
            // Es crucial que la clase sea 'partial' para que esto funcione.
            InitializeComponent();
            LoadHistoryData();
        }

        /// <summary>
        /// Carga los datos del historial y los muestra en el control ListView.
        /// </summary>
        private void LoadHistoryData()
        {
            // Se usa el método GetHistory() de la clase HistoryManager para obtener la lista.
            HistoryListView.ItemsSource = HistoryManager.GetHistory();
        }

        /// <summary>
        /// Maneja el evento de doble clic en un elemento de la lista para seleccionar una URL.
        /// </summary>
        private void HistoryListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HistoryListView.SelectedItem is HistoryEntry selectedEntry)
            {
                SelectedUrl = selectedEntry.Url;
                this.DialogResult = true; // Indica que se seleccionó una URL
                this.Close();
            }
        }
        
        /// <summary>
        /// Maneja el evento de clic en el botón para borrar todo el historial.
        /// </summary>
        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres borrar todo el historial de navegación?", "Confirmar Borrado", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                HistoryManager.ClearHistory();
                LoadHistoryData(); // Recarga la ListView para mostrar la lista vacía
            }
        }

        /// <summary>
        /// Maneja el evento de clic en el botón para cerrar la ventana.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
