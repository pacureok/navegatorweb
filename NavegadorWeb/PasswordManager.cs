using NavegadorWeb.Classes;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NavegadorWeb.Services
{
    public static class PasswordManager
    {
        private static readonly string _passwordFilePath = "passwords.json";
        private static List<PasswordEntry> _passwords = new();

        static PasswordManager()
        {
            LoadPasswords();
        }

        public static void LoadPasswords()
        {
            if (File.Exists(_passwordFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_passwordFilePath);
                    _passwords = JsonSerializer.Deserialize<List<PasswordEntry>>(json) ?? new List<PasswordEntry>();
                }
                catch
                {
                    // Manejar errores de deserialización o archivos corruptos
                    _passwords = new List<PasswordEntry>();
                }
            }
        }

        public static void SavePasswords()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_passwords, options);
            File.WriteAllText(_passwordFilePath, json);
        }

        public static List<PasswordEntry> GetPasswords()
        {
            return _passwords;
        }

        public static void AddPassword(PasswordEntry entry)
        {
            _passwords.Add(entry);
            SavePasswords();
        }

        public static void RemovePassword(PasswordEntry entry)
        {
            _passwords.RemoveAll(p => p.Url == entry.Url && p.Username == entry.Username);
            SavePasswords();
        }

        public static PasswordEntry? FindPassword(string url, string username)
        {
            return _passwords.Find(p => p.Url == url && p.Username == username);
        }
    }
}
