using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel; // Asegúrate de incluirlo
using System.Runtime.CompilerServices; // Asegúrate de incluirlo

namespace NavegadorWeb
{
    public class BrowserTabItem : INotifyPropertyChanged
    {
        public TabItem Tab { get; set; }
        public WebView2 LeftWebView { get; set; }
        public WebView2 RightWebView { get; set; } // Para pantalla dividida
        public bool IsIncognito { get; set; }
        public bool IsSplit { get; set; } // Indica si la pestaña está en modo pantalla dividida
        public TabGroup ParentGroup { get; set; }
        public TextBlock HeaderTextBlock { get; set; } // Para actualizar el texto del header
        public Image FaviconImage { get; set; } // Para mostrar el favicon
        public Image AudioIconImage { get; set; } // Para el icono de audio
        public Image ExtensionIconImage { get; set; } // Para el icono de extensión activa
        public Image BlockedIconImage { get; set; } // Para el icono de bloqueo de anuncios/trackers


        private ImageSource _faviconSource;
        public ImageSource FaviconSource
        {
            get => _faviconSource;
            set
            {
                if (_faviconSource != value)
                {
                    _faviconSource = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isAudioPlaying;
        public bool IsAudioPlaying
        {
            get => _isAudioPlaying;
            set
            {
                if (_isAudioPlaying != value)
                {
                    _isAudioPlaying = value;
                    OnPropertyChanged();
                    UpdateAudioIcon();
                }
            }
        }

        private bool _isExtensionActive;
        public bool IsExtensionActive
        {
            get => _isExtensionActive;
            set
            {
                if (_isExtensionActive != value)
                {
                    _isExtensionActive = value;
                    OnPropertyChanged();
                    UpdateExtensionIcon();
                }
            }
        }

        private bool _isSiteBlocked;
        public bool IsSiteBlocked
        {
            get => _isSiteBlocked;
            set
            {
                if (_isSiteBlocked != value)
                {
                    _isSiteBlocked = value;
                    OnPropertyChanged();
                    UpdateBlockedIcon();
                }
            }
        }

        // NUEVO: Propiedades para la función "Preguntar a Gemini"
        private bool _isSelectedForGemini;
        public bool IsSelectedForGemini
        {
            get => _isSelectedForGemini;
            set
            {
                if (_isSelectedForGemini != value)
                {
                    _isSelectedForGemini = value;
                    OnPropertyChanged();
                    UpdateGeminiIconVisibility(); // Llama a este método para controlar la visibilidad del icono
                }
            }
        }

        private ImageSource _geminiIconSource;
        public ImageSource GeminiIconSource // Icono fijo para Gemini
        {
            get => _geminiIconSource;
            set
            {
                if (_geminiIconSource != value)
                {
                    _geminiIconSource = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // NUEVO: Control para el icono de Gemini
        public Image GeminiFeatureIcon { get; set; }


        public BrowserTabItem()
        {
            FaviconSource = GetDefaultGlobeIcon();
            // Cargar el icono de Gemini una vez
            GeminiIconSource = new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/gemini_icon.ico"));
        }

        public ImageSource GetDefaultGlobeIcon()
        {
            return new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/globe.png"));
        }

        private void UpdateAudioIcon()
        {
            if (AudioIconImage != null)
            {
                AudioIconImage.Source = new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/audio.png"));
            }
        }

        private void UpdateExtensionIcon()
        {
            if (ExtensionIconImage != null)
            {
                ExtensionIconImage.Source = new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/puzzle.png"));
            }
        }

        private void UpdateBlockedIcon()
        {
            if (BlockedIconImage != null)
            {
                BlockedIconImage.Source = new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/blocked.png"));
            }
        }

        // NUEVO: Método para controlar la visibilidad del icono de Gemini
        private void UpdateGeminiIconVisibility()
        {
            if (GeminiFeatureIcon != null)
            {
                GeminiFeatureIcon.Visibility = IsSelectedForGemini ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
