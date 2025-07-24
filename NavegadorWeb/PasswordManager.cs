using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography; // Para ProtectedData
using System.Text;
using System.Text.Json; // Para JsonSerializer
using System.Windows; // Para MessageBox (solo para errores críticos)

namespace NavegadorWeb
{
    /// <summary>
    /// Clase estática para gestionar el guardado y la recuperación de contraseñas.
    /// Utiliza cifrado básico para proteger las contraseñas en el disco.
    /// </summary>
    public static class PasswordManager
    {
        private static List<PasswordEntry> _passwords = new List<PasswordEntry>();
        private static readonly string _passwordFilePath;
        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("MiNavegadorWebEntropy"); // Entropía para ProtectedData

        static PasswordManager()
        {
            // Ruta para el archivo de contraseñas dentro de la carpeta de datos de la aplicación local del usuario
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiNavegadorWeb");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            _passwordFilePath = Path.Combine(appDataPath, "passwords.dat");
            LoadPasswords();
        }

        /// <summary>
        /// Carga las contraseñas desde el archivo cifrado.
        /// </summary>
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
                    // Esto ocurre si el archivo se mueve a otra máquina o usuario, o si se corrompe.
                    MessageBox.Show($"Error al descifrar contraseñas. El archivo podría estar corrupto o no ser de esta máquina/usuario: {ex.Message}", "Error de Contraseñas", MessageBoxButton.OK, MessageBoxImage.Error);
                    _passwords = new List<PasswordEntry>(); // Limpiar la lista para evitar problemas
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar contraseñas: {ex.Message}", "Error de Contraseñas", MessageBoxButton.OK, MessageBoxImage.Error);
                    _passwords = new List<PasswordEntry>();
                }
            }
        }

        /// <summary>
        /// Guarda las contraseñas en el archivo cifrado.
        /// </summary>
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

        /// <summary>
        /// Añade o actualiza una entrada de contraseña.
        /// </summary>
        /// <param name="url">La URL del sitio.</param>
        /// <param name="username">El nombre de usuario.</param>
        /// <param name="password">La contraseña (sin cifrar).</param>
        public static void AddOrUpdatePassword(string url, string username, string password)
        {
            // Normalizar la URL para evitar duplicados por subdominios o rutas
            Uri uri = new Uri(url);
            string baseDomain = uri.Host; // Solo el dominio principal

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

        /// <summary>
        /// Obtiene una contraseña para una URL y nombre de usuario dados.
        /// </summary>
        /// <param name="url">La URL del sitio.</param>
        /// <param name="username">El nombre de usuario (opcional).</param>
        /// <returns>La contraseña descifrada o null si no se encuentra.</returns>
        public static string GetPassword(string url, string username = null)
        {
            Uri uri = new Uri(url);
            string baseDomain = uri.Host;

            PasswordEntry entry = _passwords.FirstOrDefault(p =>
                new Uri(p.Url).Host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase) &&
                (username == null || p.Username.Equals(username, StringComparison.OrdinalIgnoreCase)));

            if (entry != null)
            {
                // Actualizar la fecha de último uso
                entry.LastUsed = DateTime.Now;
                SavePasswords(); // Guardar para persistir la fecha de uso
                return DecryptPassword(entry.EncryptedPassword);
            }
            return null;
        }

        /// <summary>
        /// Obtiene todas las entradas de contraseña guardadas.
        /// </summary>
        /// <returns>Una lista de PasswordEntry.</returns>
        public static List<PasswordEntry> GetAllPasswords()
        {
            // Devolver una copia para evitar modificaciones directas de la lista interna
            return _passwords.OrderByDescending(p => p.LastUsed).ToList();
        }

        /// <summary>
        /// Elimina una entrada de contraseña.
        /// </summary>
        /// <param name="entryToRemove">La entrada de contraseña a eliminar.</param>
        public static void DeletePassword(PasswordEntry entryToRemove)
        {
            _passwords.RemoveAll(p => p.Url == entryToRemove.Url && p.Username == entryToRemove.Username);
            SavePasswords();
        }

        /// <summary>
        /// Cifra una contraseña usando ProtectedData.
        /// </summary>
        private static string EncryptPassword(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] encryptedBytes = ProtectedData.Protect(passwordBytes, _entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Descifra una contraseña usando ProtectedData.
        /// </summary>
        private static string DecryptPassword(string encryptedPassword)
        {
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
                byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (CryptographicException)
            {
                // Si falla el descifrado, es probable que la contraseña esté corrupta o no sea de esta máquina/usuario
                return "[Error de descifrado]";
            }
        }
    }
}
