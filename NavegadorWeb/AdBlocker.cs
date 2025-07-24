using System.Collections.Generic;
using System.Linq;
using System.IO; // Para leer desde un archivo

namespace NavegadorWeb
{
    public static class AdBlocker
    {
        // Lista de dominios de prueba para bloquear.
        // En una aplicación real, esto provendría de una fuente externa o un archivo grande.
        private static HashSet<string> _blockedDomains = new HashSet<string>()
        {
            "doubleclick.net",
            "googlesyndication.com",
            "adservice.google.com",
            "pixel.adsafeprotected.com",
            "adnxs.com",
            // Puedes añadir más dominios aquí para probar
            // O mejor aún, cargar desde un archivo de texto
        };

        private static bool _isEnabled = false; // Estado del bloqueador

        public static bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }

        // Método para cargar dominios bloqueados desde un archivo (opcional, pero mejor práctica)
        public static void LoadBlockedDomainsFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#")) // Ignorar líneas vacías o comentarios
                    {
                        _blockedDomains.Add(line.Trim().ToLower());
                    }
                }
            }
        }

        public static bool IsBlocked(string url)
        {
            if (!_isEnabled) return false;

            try
            {
                Uri uri = new Uri(url);
                string host = uri.Host.ToLower();

                // Comprobar si el host está en la lista de dominios bloqueados
                if (_blockedDomains.Contains(host))
                {
                    return true;
                }

                // También podemos bloquear subdominios, por ejemplo, si bloqueamos "example.com", también bloquear "sub.example.com"
                foreach (var blockedDomain in _blockedDomains)
                {
                    if (host.EndsWith("." + blockedDomain) || host == blockedDomain)
                    {
                        return true;
                    }
                }
            }
            catch (UriFormatException)
            {
                // Ignorar URLs mal formadas
            }
            return false;
        }
    }
}
