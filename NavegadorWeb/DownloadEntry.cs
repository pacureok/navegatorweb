using System;
using Microsoft.Web.WebView2.Core; // Necesario para CoreWebView2DownloadState

namespace NavegadorWeb
{
    /// <summary>
    /// Representa una entrada individual en el gestor de descargas.
    /// </summary>
    public class DownloadEntry
    {
        public string Id { get; set; } // ID único para la descarga
        public string FileName { get; set; }
        public string Url { get; set; }
        public string TargetPath { get; set; } // Ruta completa donde se guarda el archivo
        public long TotalBytes { get; set; }
        public long ReceivedBytes { get; set; }
        public int Progress { get; set; } // Progreso de 0 a 100
        public CoreWebView2DownloadState State { get; set; } // Estado de la descarga (En curso, Completado, Cancelado, etc.)
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; } // Puede ser nulo si la descarga no ha terminado
        public bool IsActive { get; set; } // Indica si la descarga está activa o completada/cancelada

        // Propiedad para el estado legible (para la UI)
        public string StatusText
        {
            get
            {
                switch (State)
                {
                    case CoreWebView2DownloadState.InProgress:
                        return $"Descargando ({Progress}%)";
                    case CoreWebView2DownloadState.Completed:
                        return "Completado";
                    case CoreWebView2DownloadState.Interrupted:
                        return "Interrumpido";
                    default:
                        return "Desconocido";
                }
            }
        }

        public DownloadEntry()
        {
            // Constructor vacío para serialización/deserialización
            Id = Guid.NewGuid().ToString(); // Genera un ID único por defecto
            StartTime = DateTime.Now;
            IsActive = true;
        }
    }
}
