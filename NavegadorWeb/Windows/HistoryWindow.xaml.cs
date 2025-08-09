using NavegadorWeb.Classes;
using NavegadorWeb.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NavegadorWeb.Windows
{
    public partial class HistoryWindow : Window
    {
        public string? SelectedUrl { get; private set; }

        public HistoryWindow()
        {
            InitializeComponent();
            LoadHistoryData();
        }

        private void LoadHistoryData()
        {
            // Usamos GetHistory() para obtener la lista que ya ha sido cargada
            // al iniciar la aplicación.
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
            // Se asume que el método ClearHistory() existe en HistoryManager.
            HistoryManager.ClearHistory();
            LoadHistoryData();
        }
    }
}
