using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json; // Necesario para la serialización JSON
using System.Windows; // Para MessageBox

namespace NavegadorWeb
{
    /// <summary>
    /// Gestiona la carga y guardado de los marcadores en un archivo JSON.
    /// </summary>
    public static class BookmarkManager
    {
        private static readonly string BookmarksFilePath; // Ruta donde se guardarán los marcadores

        static BookmarkManager()
        {
            // Define la ruta del archivo de marcadores dentro de la carpeta de datos de la aplicación.
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string browserFolder = Path.Combine(appDataFolder, "MiNavegadorWeb");
            Directory.CreateDirectory(browserFolder); // Asegura que la carpeta exista
            BookmarksFilePath = Path.Combine(browserFolder, "bookmarks.json");
        }

        /// <summary>
        /// Carga la lista de marcadores desde el archivo JSON.
        /// </summary>
        /// <returns>Una lista de BookmarkEntry, ordenada por fecha de adición descendente.</returns>
        public static List<BookmarkEntry> LoadBookmarks()
        {
            if (!File.Exists(BookmarksFilePath))
            {
                return new List<BookmarkEntry>(); // Retorna una lista vacía si el archivo no existe
            }

            try
            {
                string jsonString = File.ReadAllText(BookmarksFilePath);
                // Deserializa la lista de BookmarkEntry desde el JSON.
                var bookmarks = JsonSerializer.Deserialize<List<BookmarkEntry>>(jsonString);
                // Asegura que la lista no sea nula y la ordena por fecha descendente.
                return bookmarks?.OrderByDescending(e => e.DateAdded).ToList() ?? new List<BookmarkEntry>();
            }
            catch (Exception ex)
            {
                // Manejo de errores si el archivo está corrupto o hay problemas de lectura.
                MessageBox.Show($"Error al cargar los marcadores: {ex.Message}", "Error de Marcadores", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<BookmarkEntry>();
            }
        }

        /// <summary>
        /// Guarda la lista de marcadores en el archivo JSON.
        /// </summary>
        /// <param name="bookmarks">La lista de BookmarkEntry a guardar.</param>
        public static void SaveBookmarks(List<BookmarkEntry> bookmarks)
        {
            try
            {
                // Serializa la lista de BookmarkEntry a formato JSON con opciones de formato.
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(bookmarks, options);
                File.WriteAllText(BookmarksFilePath, jsonString);
            }
            catch (Exception ex)
            {
                // Manejo de errores si hay problemas de escritura.
                MessageBox.Show($"Error al guardar los marcadores: {ex.Message}", "Error de Marcadores", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Añade una nueva entrada de marcador y lo guarda.
        /// </summary>
        /// <param name="url">La URL de la página a marcar.</param>
        /// <param name="title">El título de la página a marcar.</param>
        public static void AddBookmark(string url, string title)
        {
            var bookmarks = LoadBookmarks(); // Carga los marcadores actuales

            // Opcional: Evitar duplicados. Puedes decidir si permites múltiples marcadores para la misma URL.
            // Por ahora, permitiremos duplicados para simplificar. Si quieres evitar, añade una comprobación aquí.
            if (!bookmarks.Any(b => b.Url == url)) // Ejemplo para evitar duplicados por URL
            {
                bookmarks.Insert(0, new BookmarkEntry(url, title)); // Añade el nuevo marcador al principio
                SaveBookmarks(bookmarks); // Guarda la lista actualizada
                MessageBox.Show($"'{title}' añadido a marcadores.", "Marcador Añadido", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"'{title}' ya existe en tus marcadores.", "Marcador Existente", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Elimina un marcador específico de la lista y lo guarda.
        /// </summary>
        /// <param name="bookmarkToRemove">El BookmarkEntry a eliminar.</param>
        public static void RemoveBookmark(BookmarkEntry bookmarkToRemove)
        {
            var bookmarks = LoadBookmarks();
            // Elimina el marcador que coincide con la URL y la fecha de adición
            bookmarks.RemoveAll(b => b.Url == bookmarkToRemove.Url && b.DateAdded == bookmarkToRemove.DateAdded);
            SaveBookmarks(bookmarks);
        }

        /// <summary>
        /// Borra todos los marcadores.
        /// </summary>
        public static void ClearAllBookmarks()
        {
            try
            {
                if (File.Exists(BookmarksFilePath))
                {
                    File.Delete(BookmarksFilePath);
                }
                MessageBox.Show("Todos los marcadores han sido borrados con éxito.", "Marcadores Borrados", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al borrar los marcadores: {ex.Message}", "Error de Marcadores", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
