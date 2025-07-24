using System;
using System.Windows.Controls; // Necesario para TabItem

namespace NavegadorWeb
{
    /// <summary>
    /// Clase auxiliar para representar la información de rendimiento de una pestaña
    /// en el Monitor de Rendimiento.
    /// </summary>
    public class TabPerformanceInfo
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsIncognito { get; set; }
        public bool IsSuspended { get; set; }
        public string Status => IsSuspended ? "Suspendida" : "Activa";
        public TabItem AssociatedTabItem { get; set; } // Referencia al TabItem real

        public TabPerformanceInfo(string title, string url, bool isIncognito, bool isSuspended, TabItem associatedTabItem)
        {
            Title = title;
            Url = url;
            IsIncognito = isIncognito;
            IsSuspended = isSuspended;
            AssociatedTabItem = associatedTabItem;
        }
    }
}
