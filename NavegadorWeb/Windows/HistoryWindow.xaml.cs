using NavegadorWeb.Classes;
using NavegadorWeb.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;

namespace NavegadorWeb.Windows
{
    public partial class HistoryWindow : Window
    {
        public string SelectedUrl { get; private set; }

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
            // La clase HistoryManager debe tener un método GetHistory()
            // que devuelva la lista de entradas cargadas.
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
                this.DialogResult = true; // Indica que una URL fue seleccionada
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
                LoadHistoryData(); // Recarga la ListView para mostrarla vacía
            }
        }

        /// <summary>
        /// Maneja el clic en el botón para cerrar la ventana sin seleccionar nada.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
