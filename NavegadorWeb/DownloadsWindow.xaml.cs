using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics; // Para abrir archivos y carpetas
using System.IO; // Para Path.GetDirectoryName

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para DownloadsWindow.xaml
    /// </summary>
    public partial class DownloadsWindow : Window
    {
        public DownloadsWindow()
        {
            InitializeComponent();
            DownloadManager.DownloadsUpdated += DownloadManager_DownloadsUpdated; // Suscribirse a eventos de actualización
            LoadDownloadsData(); // Carga los datos de descargas al iniciar la ventana
        }

        /// <summary>
        /// Carga los datos de descargas desde DownloadManager y los muestra en la ListView.
        /// </summary>
        private void LoadDownloadsData()
        {
            // Asegúrate de que la ListView se actualice en el hilo de la UI
            Dispatcher.Invoke(() =>
            {
                DownloadsListView.ItemsSource = null; // Limpiar para forzar la actualización
                DownloadsListView.ItemsSource = DownloadManager.GetDownloads();
            });
        }

        /// <summary>
        /// Maneja el evento de actualización de descargas del DownloadManager.
        /// </summary>
        private void DownloadManager_DownloadsUpdated(object sender, System.EventArgs e)
        {
            LoadDownloadsData(); // Recarga los datos cuando hay una actualización
        }

        /// <summary>
        /// Maneja el doble clic en un elemento de descarga para abrir el archivo.
        /// </summary>
        private void DownloadsListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DownloadsListView.SelectedItem is DownloadEntry selectedEntry)
            {
                if (selectedEntry.State == Microsoft.Web.WebView2.Core.CoreWebView2DownloadState.Completed)
                {
                    try
                    {
                        // Abre el archivo usando la aplicación predeterminada del sistema
                        Process.Start(new ProcessStartInfo(selectedEntry.TargetPath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No se pudo abrir el archivo: {ex.Message}", "Error al Abrir Archivo", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("La descarga no ha finalizado o está interrumpida.", "Descarga No Lista", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Abrir Carpeta".
        /// </summary>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadsListView.SelectedItem is DownloadEntry selectedEntry)
            {
                try
                {
                    string directory = Path.GetDirectoryName(selectedEntry.TargetPath);
                    if (Directory.Exists(directory))
                    {
                        Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("La carpeta de descarga no existe.", "Carpeta No Encontrada", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo abrir la carpeta: {ex.Message}", "Error al Abrir Carpeta", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Por favor, selecciona una descarga para abrir su carpeta.", "Ninguna Descarga Seleccionada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Eliminar Descarga".
        /// </summary>
        private void RemoveDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadsListView.SelectedItem is DownloadEntry selectedEntry)
            {
                MessageBoxResult result = MessageBox.Show($"¿Estás seguro de que quieres eliminar la descarga de '{selectedEntry.FileName}' de la lista? (Esto no borra el archivo físico)", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    DownloadManager.RemoveDownload(selectedEntry); // Elimina la descarga
                    LoadDownloadsData(); // Recarga la ListView
                }
            }
            else
            {
                MessageBox.Show("Por favor, selecciona una descarga para eliminar.", "Ninguna Descarga Seleccionada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Borrar Completadas".
        /// </summary>
        private void ClearCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres borrar las descargas completadas/interrumpidas de la lista?", "Confirmar Borrado", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                DownloadManager.ClearCompletedDownloads();
                LoadDownloadsData();
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Cerrar".
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Cierra la ventana
        }

        /// <summary>
        /// Se desuscribe del evento al cerrar la ventana para evitar fugas de memoria.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            DownloadManager.DownloadsUpdated -= DownloadManager_DownloadsUpdated;
        }
    }
}
