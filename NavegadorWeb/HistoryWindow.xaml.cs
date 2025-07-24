using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics; // Para abrir URLs en el navegador principal

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow : Window
    {
        // Propiedad para que MainWindow pueda acceder a la URL seleccionada
        public string SelectedUrl { get; private set; }

        public HistoryWindow()
        {
            InitializeComponent();
            LoadHistoryData(); // Carga los datos del historial al iniciar la ventana
        }

        /// <summary>
        /// Carga los datos del historial desde HistoryManager y los muestra en la ListView.
        /// </summary>
        private void LoadHistoryData()
        {
            List<HistoryEntry> history = HistoryManager.LoadHistory();
            HistoryListView.ItemsSource = history; // Asigna la lista como fuente de datos para la ListView
        }

        /// <summary>
        /// Maneja el doble clic en un elemento del historial.
        /// </summary>
        private void HistoryListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (HistoryListView.SelectedItem is HistoryEntry selectedEntry)
            {
                SelectedUrl = selectedEntry.Url;
                this.DialogResult = true; // Indica que se seleccionó una URL
                this.Close(); // Cierra la ventana
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Borrar Historial".
        /// </summary>
        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres borrar todo el historial de navegación?", "Confirmar Borrado", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                HistoryManager.ClearHistory(); // Borra el historial del archivo
                LoadHistoryData(); // Recarga la ListView para mostrar el historial vacío
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Cerrar".
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indica que no se seleccionó ninguna URL
            this.Close(); // Cierra la ventana
        }
    }
}
