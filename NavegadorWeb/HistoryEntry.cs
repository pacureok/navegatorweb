using System;

namespace NavegadorWeb.Classes
{
    public class HistoryEntry
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        public HistoryEntry() { }

        public HistoryEntry(string url, string title)
        {
            Url = url;
            Title = title;
            Timestamp = DateTime.Now;
        }
    }
}
