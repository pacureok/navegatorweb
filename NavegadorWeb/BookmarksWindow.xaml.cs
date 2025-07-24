using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para BookmarksWindow.xaml
    /// </summary>
    public partial class BookmarksWindow : Window
    {
        // Propiedad para que MainWindow pueda acceder a la URL seleccionada
        public string SelectedUrl { get; private set; }

        public BookmarksWindow()
        {
            InitializeComponent();
            LoadBookmarksData(); // Carga los datos de marcadores al iniciar la ventana
        }

        /// <summary>
        /// Carga los datos de marcadores desde BookmarkManager y los muestra en la ListView.
        /// </summary>
        private void LoadBookmarksData()
        {
            List<BookmarkEntry> bookmarks = BookmarkManager.LoadBookmarks();
            BookmarksListView.ItemsSource = bookmarks; // Asigna la lista como fuente de datos para la ListView
        }

        /// <summary>
        /// Maneja el doble clic en un elemento de marcador.
        /// </summary>
        private void BookmarksListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (BookmarksListView.SelectedItem is BookmarkEntry selectedEntry)
            {
                SelectedUrl = selectedEntry.Url;
                this.DialogResult = true; // Indica que se seleccionó una URL
                this.Close(); // Cierra la ventana
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Eliminar Marcador".
        /// </summary>
        private void DeleteBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (BookmarksListView.SelectedItem is BookmarkEntry selectedEntry)
            {
                MessageBoxResult result = MessageBox.Show($"¿Estás seguro de que quieres eliminar '{selectedEntry.Title}' de tus marcadores?", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    BookmarkManager.RemoveBookmark(selectedEntry); // Elimina el marcador
                    LoadBookmarksData(); // Recarga la ListView para reflejar el cambio
                }
            }
            else
            {
                MessageBox.Show("Por favor, selecciona un marcador para eliminar.", "Ningún Marcador Seleccionado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Maneja el clic en el botón "Borrar Todos".
        /// </summary>
        private void ClearAllBookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres borrar TODOS tus marcadores?", "Confirmar Borrado Total", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                BookmarkManager.ClearAllBookmarks(); // Borra todos los marcadores
                LoadBookmarksData(); // Recarga la ListView para mostrar la lista vacía
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
