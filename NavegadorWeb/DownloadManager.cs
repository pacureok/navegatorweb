using NavegadorWeb.Classes;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NavegadorWeb.Services
{
    public static class DownloadManager
    {
        private static readonly string _downloadsFilePath = "downloads.json";
        private static List<DownloadEntry> _downloads = new();

        static DownloadManager()
        {
            LoadDownloads();
        }

        public static void LoadDownloads()
        {
            if (File.Exists(_downloadsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_downloadsFilePath);
                    _downloads = JsonSerializer.Deserialize<List<DownloadEntry>>(json) ?? new List<DownloadEntry>();
                }
                catch
                {
                    _downloads = new List<DownloadEntry>();
                }
            }
        }

        public static void SaveDownloads()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_downloads, options);
            File.WriteAllText(_downloadsFilePath, json);
        }

        public static List<DownloadEntry> GetDownloads()
        {
            return _downloads;
        }

        public static void AddDownload(DownloadEntry entry)
        {
            _downloads.Add(entry);
            SaveDownloads();
        }
    }
}
