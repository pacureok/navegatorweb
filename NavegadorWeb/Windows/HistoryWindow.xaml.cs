using NavegadorWeb.Classes;
using NavegadorWeb.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NavegadorWeb.Windows
{
    // La clase debe ser 'partial'
    public partial class HistoryWindow : Window
    {
        // Se inicializa con una cadena vacía para evitar el error de nulidad.
        public string SelectedUrl { get; private set; } = string.Empty;

        public HistoryWindow()
        {
            InitializeComponent();
            LoadHistoryData();
        }

        private void LoadHistoryData()
        {
            HistoryListView.ItemsSource = HistoryManager.GetHistory();
        }

        private void HistoryListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HistoryListView.SelectedItem is HistoryEntry selectedEntry)
            {
                SelectedUrl = selectedEntry.Url;
                this.DialogResult = true;
                this.Close();
            }
        }
        
        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres borrar todo el historial de navegación?", "Confirmar Borrado", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                HistoryManager.ClearHistory();
                LoadHistoryData();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
