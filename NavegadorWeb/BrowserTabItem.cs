using System.Windows.Controls;
using System.Windows.Media;

namespace NavegadorWeb.Classes
{
    public class BrowserTabItem : TabItem
    {
        public bool IsSuspended { get; set; }
        public bool IsAudioPlaying { get; set; }

        public BrowserTabItem()
        {
            // Propiedades personalizadas
        }

        public void UpdateTabState()
        {
            if (IsSuspended)
            {
                Header = new TextBlock { Text = "Pestaña suspendida", Foreground = Brushes.Gray };
            }
            else if (IsAudioPlaying)
            {
                Header = new TextBlock { Text = "▶️ " + Header.ToString() };
            }
        }
    }
}
