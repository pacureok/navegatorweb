using System;
using System.Windows.Controls; // Necesario para TabItem

namespace NavegadorWeb
{
    /// <summary>
    /// Clase auxiliar para representar la información de una pestaña en el Administrador de Pestañas.
    /// </summary>
    public class TabInfo
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsIncognito { get; set; }
        public bool IsSuspended { get; set; }
        public TabItem AssociatedTabItem { get; set; } // Referencia al TabItem real para poder cerrarlo

        public string DisplayType => IsIncognito ? "Incógnito" : "Normal";
        public string DisplayStatus => IsSuspended ? "Suspendida" : "Activa";

        public TabInfo(string title, string url, bool isIncognito, bool isSuspended, TabItem associatedTabItem)
        {
            Title = title;
            Url = url;
            IsIncognito = isIncognito;
            IsSuspended = isSuspended;
            AssociatedTabItem = associatedTabItem;
        }
    }
}
