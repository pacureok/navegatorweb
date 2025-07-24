using System;

namespace NavegadorWeb
{
    /// <summary>
    /// Clase para representar una entrada de contraseña guardada.
    /// </summary>
    public class PasswordEntry
    {
        public string Url { get; set; } // URL del sitio web
        public string Username { get; set; } // Nombre de usuario
        public string EncryptedPassword { get; set; } // Contraseña cifrada
        public DateTime LastUsed { get; set; } // Fecha de último uso para ordenar
    }
}
