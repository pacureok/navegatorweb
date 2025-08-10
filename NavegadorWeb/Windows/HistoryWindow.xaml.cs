using NavegadorWeb.Classes;
using NavegadorWeb.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic; // Asegúrate de que esta línea esté presente

namespace NavegadorWeb.Windows
{
    /// <summary>
    /// Lógica de interacción para HistoryWindow.xaml
    /// </summary>
    // Es CRUCIAL que la clase sea 'partial'
    public partial class HistoryWindow : Window
    {
        // Propiedad para que MainWindow pueda acceder a la URL seleccionada
        public string SelectedUrl { get; private set; } = string.Empty;

        public HistoryWindow()
        {
            // El compilador ahora reconocerá InitializeComponent()
            InitializeComponent();
            LoadHistoryData();
        }

        /// <summary>
        /// Carga los datos del historial y los muestra en el control ListView.
        /// </summary>
        private void LoadHistoryData()
        {
            // Asegúrate de que HistoryManager.GetHistory() devuelva una lista de HistoryEntry
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
