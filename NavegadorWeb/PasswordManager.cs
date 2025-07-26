using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace NavegadorWeb
{
    public static class PasswordManager
    {
        private static List<PasswordEntry> _passwords = new List<PasswordEntry>();
        private static readonly string _passwordFilePath;
        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("MiNavegadorWebEntropy");

        static PasswordManager()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiNavegadorWeb");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            _passwordFilePath = Path.Combine(appDataPath, "passwords.dat");
            LoadPasswords();
        }

        private static void LoadPasswords()
        {
            if (File.Exists(_passwordFilePath))
            {
                try
                {
                    string encryptedJson = File.ReadAllText(_passwordFilePath);
                    byte[] encryptedBytes = Convert.FromBase64String(encryptedJson);
                    byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
                    string decryptedJson = Encoding.UTF8.GetString(decryptedBytes);
                    _passwords = JsonSerializer.Deserialize<List<PasswordEntry>>(decryptedJson) ?? new List<PasswordEntry>();
                }
                catch (CryptographicException ex)
                {
                    MessageBox.Show($"Error al descifrar contraseñas. El archivo podría estar corrupto o no ser de esta máquina/usuario: {ex.Message}", "Error de Contraseñas", MessageBoxButton.OK, MessageBoxImage.Error);
                    _passwords = new List<PasswordEntry>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar contraseñas: {ex.Message}", "Error de Contraseñas", MessageBoxButton.OK, MessageBoxImage.Error);
                    _passwords = new List<PasswordEntry>();
                }
            }
        }

        private static void SavePasswords()
        {
            try
            {
                string json = JsonSerializer.Serialize(_passwords);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                byte[] encryptedBytes = ProtectedData.Protect(jsonBytes, _entropy, DataProtectionScope.CurrentUser);
                string encryptedJson = Convert.ToBase64String(encryptedBytes);
                File.WriteAllText(_passwordFilePath, encryptedJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar contraseñas: {ex.Message}", "Error de Contraseñas", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void AddOrUpdatePassword(string url, string username, string password)
        {
            Uri uri = new Uri(url);
            string baseDomain = uri.Host;

            PasswordEntry existingEntry = _passwords.FirstOrDefault(p =>
                new Uri(p.Url).Host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase) &&
                p.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (existingEntry != null)
            {
                existingEntry.EncryptedPassword = EncryptPassword(password);
                existingEntry.LastUsed = DateTime.Now;
            }
            else
            {
                _passwords.Add(new PasswordEntry
                {
                    Url = url,
                    Username = username,
                    EncryptedPassword = EncryptPassword(password),
                    LastUsed = DateTime.Now
                });
            }
            SavePasswords();
        }

        public static string GetPassword(string url, string username = null)
        {
            Uri uri = new Uri(url);
            string baseDomain = uri.Host;

            PasswordEntry entry = _passwords.FirstOrDefault(p =>
                new Uri(p.Url).Host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase) &&
                (username == null || p.Username.Equals(username, StringComparison.OrdinalIgnoreCase)));

            if (entry != null)
            {
                entry.LastUsed = DateTime.Now;
                SavePasswords();
                return DecryptPassword(entry.EncryptedPassword);
            }
            return null;
        }

        public static List<PasswordEntry> GetAllPasswords()
        {
            return _passwords.OrderByDescending(p => p.LastUsed).ToList();
        }

        public static void DeletePassword(PasswordEntry entryToRemove)
        {
            _passwords.RemoveAll(p => p.Url == entryToRemove.Url && p.Username == entryToRemove.Username);
            SavePasswords();
        }

        private static string EncryptPassword(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] encryptedBytes = ProtectedData.Protect(passwordBytes, _entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        // Hacer este método público para que MainWindow pueda llamarlo
        public static string DecryptPassword(string encryptedPassword)
        {
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (CryptographicException)
            {
                return "[Error de descifrado]";
            }
        }
    }
}
