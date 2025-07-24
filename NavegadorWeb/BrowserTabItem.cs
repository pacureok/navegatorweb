using Microsoft.Web.WebView2.Wpf;
using System.Windows.Controls;
using System.Windows.Media; // Para ImageSource
using System.Windows.Media.Imaging; // Para BitmapImage
using System;
using System.IO;
using System.ComponentModel; // Para INotifyPropertyChanged

namespace NavegadorWeb
{
    /// <summary>
    /// Clase auxiliar para gestionar la información de cada pestaña del navegador,
    /// incluyendo si está en modo dividido, referencias a ambos WebView2,
    /// y propiedades para el favicon y el estado de audio.
    /// </summary>
    public class BrowserTabItem : DependencyObject, INotifyPropertyChanged
    {
        public TabItem Tab { get; set; } // El control TabItem de WPF
        public WebView2 LeftWebView { get; set; } // La instancia de WebView2 del panel izquierdo (principal)
        public WebView2 RightWebView { get; set; } // La instancia de WebView2 del panel derecho (puede ser null)
        public TextBlock HeaderTextBlock { get; set; } // El TextBlock que muestra el título en el encabezado de la pestaña
        public Image FaviconImage { get; set; } // El control Image para el favicon
        public Image AudioIconImage { get; set; } // El control Image para el icono de audio
        public Image ExtensionIconImage { get; set; } // NUEVO: Control Image para el icono de extensión
        public Image BlockedIconImage { get; set; } // NUEVO: Control Image para el icono de bloqueo

        public bool IsIncognito { get; set; } // Indica si la pestaña está en modo incógnito
        public bool IsSplit { get; set; } // Indica si la pestaña está en modo dividido

        // Referencia al grupo al que pertenece esta pestaña
        public TabGroup ParentGroup { get; set; }

        // Propiedades de estado para la UI de la pestaña
        private bool _isAudioPlaying;
        public bool IsAudioPlaying
        {
            get { return _isAudioPlaying; }
            set
            {
                if (_isAudioPlaying != value)
                {
                    _isAudioPlaying = value;
                    OnPropertyChanged(nameof(IsAudioPlaying)); // Notificar cambio
                    if (AudioIconImage != null)
                    {
                        AudioIconImage.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    }
                }
            }
        }

        private ImageSource _faviconSource;
        public ImageSource FaviconSource
        {
            get { return _faviconSource; }
            set
            {
                if (_faviconSource != value)
                {
                    _faviconSource = value;
                    OnPropertyChanged(nameof(FaviconSource)); // Notificar cambio
                    if (FaviconImage != null)
                    {
                        FaviconImage.Source = value;
                    }
                }
            }
        }

        private bool _isExtensionActive; // NUEVO: Indica si una extensión personalizada está activa en esta pestaña
        public bool IsExtensionActive
        {
            get { return _isExtensionActive; }
            set
            {
                if (_isExtensionActive != value)
                {
                    _isExtensionActive = value;
                    OnPropertyChanged(nameof(IsExtensionActive));
                    if (ExtensionIconImage != null)
                    {
                        ExtensionIconImage.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    }
                }
            }
        }

        private bool _isSiteBlocked; // NUEVO: Indica si el AdBlocker/TrackerBlocker bloqueó algo en esta pestaña
        public bool IsSiteBlocked
        {
            get { return _isSiteBlocked; }
            set
            {
                if (_isSiteBlocked != value)
                {
                    _isSiteBlocked = value;
                    OnPropertyChanged(nameof(IsSiteBlocked));
                    if (BlockedIconImage != null)
                    {
                        BlockedIconImage.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    }
                }
            }
        }

        // Constructor
        public BrowserTabItem()
        {
            FaviconSource = GetDefaultGlobeIcon();
            IsAudioPlaying = false;
            IsExtensionActive = false; // Por defecto no hay extensiones activas
            IsSiteBlocked = false;     // Por defecto no hay bloqueo
        }

        /// <summary>
        /// Carga el icono de globo terráqueo por defecto.
        /// </summary>
        public ImageSource GetDefaultGlobeIcon() // Cambiado a public para que pueda ser llamado desde MainWindow
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "globe_icon.png");
                if (File.Exists(iconPath))
                {
                    return new BitmapImage(new Uri(iconPath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar el icono de globo: {ex.Message}");
            }
            return new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/globe_icon.png"));
        }

        /// <summary>
        /// Carga el icono de audio reproduciéndose.
        /// </summary>
        public static ImageSource GetAudioPlayingIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "audio_playing_icon.png");
                if (File.Exists(iconPath))
                {
                    return new BitmapImage(new Uri(iconPath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar el icono de audio: {ex.Message}");
            }
            return new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/audio_playing_icon.png"));
        }

        /// <summary>
        /// NUEVO: Carga el icono de extensión activa.
        /// </summary>
        public static ImageSource GetExtensionActiveIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "extension_icon.png");
                if (File.Exists(iconPath))
                {
                    return new BitmapImage(new Uri(iconPath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar el icono de extensión: {ex.Message}");
            }
            return new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/extension_icon.png"));
        }

        /// <summary>
        /// NUEVO: Carga el icono de sitio bloqueado.
        /// </summary>
        public static ImageSource GetSiteBlockedIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "blocked_icon.png");
                if (File.Exists(iconPath))
                {
                    return new BitmapImage(new Uri(iconPath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar el icono de bloqueo: {ex.Message}");
            }
            return new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/blocked_icon.png"));
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
