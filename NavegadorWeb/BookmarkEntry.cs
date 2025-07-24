using System;

namespace NavegadorWeb
{
    /// <summary>
    /// Representa una entrada individual en la lista de marcadores del navegador.
    /// </summary>
    public class BookmarkEntry
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public DateTime DateAdded { get; set; } // Cuándo se añadió el marcador

        public BookmarkEntry()
        {
            // Constructor vacío para serialización/deserialización
        }

        public BookmarkEntry(string url, string title)
        {
            Url = url;
            Title = title;
            DateAdded = DateTime.Now; // Establece la fecha al momento de la creación
        }
    }
}
