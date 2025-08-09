using System;

namespace NavegadorWeb.Classes
{
    public class DownloadEntry
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
        public long ReceivedBytes { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
    }
}
