using System;

namespace NavegadorWeb.Windows.Classes
{
    /// <summary>
    /// Representa una entrada individual en el historial de navegación.
    /// </summary>
    public class HistoryEntry
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public DateTime Timestamp { get; set; }

        public HistoryEntry()
        {
        }

        public HistoryEntry(string url, string title)
        {
            Url = url;
            Title = title;
            Timestamp = DateTime.Now;
        }
    }
}
