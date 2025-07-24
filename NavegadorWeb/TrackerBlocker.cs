using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows; // Para MessageBox

namespace NavegadorWeb
{
    /// <summary>
    /// Clase estática para gestionar el bloqueo de rastreadores.
    /// Carga una lista de dominios de rastreo conocidos y los bloquea.
    /// </summary>
    public static class TrackerBlocker
    {
        private static HashSet<string> _blockedTrackerDomains = new HashSet<string>();
        public static bool IsEnabled { get; set; } = false; // Estado inicial: deshabilitado

        /// <summary>
        /// Carga los dominios de rastreo desde un archivo de texto.
        /// Cada línea del archivo debe contener un dominio.
        /// </summary>
        /// <param name="filePath">Ruta al archivo de dominios de rastreo.</param>
        public static void LoadBlockedTrackerDomainsFromFile(string filePath)
        {
            _blockedTrackerDomains.Clear(); // Limpiar la lista existente
            if (File.Exists(filePath))
            {
                try
                {
                    // Leer todas las líneas del archivo
                    string[] domains = File.ReadAllLines(filePath);
                    foreach (string domain in domains)
                    {
                        // Limpiar y añadir el dominio si no está vacío
                        string trimmedDomain = domain.Trim().ToLower();
                        if (!string.IsNullOrWhiteSpace(trimmedDomain) && !trimmedDomain.StartsWith("#")) // Ignorar comentarios
                        {
                            _blockedTrackerDomains.Add(trimmedDomain);
                        }
                    }
                    // MessageBox.Show($"Se cargaron {_blockedTrackerDomains.Count} dominios de rastreo.", "Tracker Blocker", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la lista de dominios de rastreo: {ex.Message}", "Error de Tracker Blocker", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"Advertencia: El archivo '{Path.GetFileName(filePath)}' no se encontró. La protección contra rastreadores no funcionará.", "Archivo Faltante", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Comprueba si una URL debe ser bloqueada porque pertenece a un rastreador.
        /// </summary>
        /// <param name="url">La URL a comprobar.</param>
        /// <returns>True si la URL debe ser bloqueada, False en caso contrario.</returns>
        public static bool IsBlocked(string url)
        {
            if (!IsEnabled) return false; // Si el bloqueador está deshabilitado, no bloquear nada

            try
            {
                Uri uri = new Uri(url);
                string host = uri.Host.ToLower();

                // Comprobar si el host o cualquier subdominio está en la lista de bloqueo
                foreach (string blockedDomain in _blockedTrackerDomains)
                {
                    if (host == blockedDomain || host.EndsWith("." + blockedDomain))
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
