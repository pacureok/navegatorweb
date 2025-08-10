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
        // Se inicializa con una cadena vacía para evitar errores de nulidad (CS8618).
        public string SelectedUrl { get; private set; } = string.Empty;

        public HistoryWindow()
        {
            InitializeComponent();
            LoadHistoryData();
        }

        /// <summary>
        /// Carga los datos del historial y los muestra en la ListView.
        /// </summary>
        private void LoadHistoryData()
        {
            HistoryListView.ItemsSource = HistoryManager.GetHistory();
        }

        /// <summary>
        /// Maneja el doble clic en un elemento para seleccionar la URL y cerrar la ventana.
        /// </summary>
        private void HistoryListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HistoryListView.SelectedItem is HistoryEntry selectedEntry)
            {
                SelectedUrl = selectedEntry.Url;
                this.DialogResult = true;
                this.Close();
            }
        }

        /// <summary>
        /// Maneja el clic en el botón para borrar todo el historial.
        /// </summary>
        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres borrar todo el historial de navegación?", "Confirmar Borrado", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                HistoryManager.ClearHistory();
                LoadHistoryData();
            }
        }
        
        /// <summary>
        /// Maneja el clic en el botón para cerrar la ventana.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
