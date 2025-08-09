using NavegadorWeb.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NavegadorWeb.Windows
{
    public partial class BookmarksWindow : Window
    {
        public string? SelectedUrl { get; private set; }

        public BookmarksWindow()
        {
            InitializeComponent();
            LoadBookmarksData();
        }

        private void LoadBookmarksData()
        {
            // Asume que tienes una clase BookmarkManager en el namespace NavegadorWeb.Services
            List<BookmarkEntry> bookmarks = BookmarkManager.LoadBookmarks();
            BookmarksListView.ItemsSource = bookmarks;
        }

        private void BookmarksListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BookmarksListView.SelectedItem is BookmarkEntry selectedEntry)
            {
                SelectedUrl = selectedEntry.Url;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void RemoveBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (BookmarksListView.SelectedItem is BookmarkEntry selectedEntry)
            {
                MessageBoxResult result = MessageBox.Show($"¿Estás seguro de que quieres eliminar el marcador de '{selectedEntry.Title}'?", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    BookmarkManager.RemoveBookmark(selectedEntry);
                    LoadBookmarksData();
                }
            }
            else
            {
                MessageBox.Show("Por favor, selecciona un marcador para eliminar.", "Ningún Marcador Seleccionado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearAllBookmarksButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Estás seguro de que quieres borrar TODOS tus marcadores?", "Confirmar Borrado Total", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                BookmarkManager.ClearAllBookmarks();
                LoadBookmarksData();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
