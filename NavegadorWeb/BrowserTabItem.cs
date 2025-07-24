using Microsoft.Web.WebView2.Wpf;
using System.Windows.Controls;
using System.Windows.Media; // Para ImageSource
using System.Windows.Media.Imaging; // Para BitmapImage
using System;
using System.IO;

namespace NavegadorWeb
{
    /// <summary>
    /// Clase auxiliar para gestionar la información de cada pestaña del navegador,
    /// incluyendo si está en modo dividido, referencias a ambos WebView2,
    /// y propiedades para el favicon y el estado de audio.
    /// </summary>
    public class BrowserTabItem : DependencyObject // Heredar de DependencyObject para usar Dependency Properties si es necesario, o simplemente de object
    {
        public TabItem Tab { get; set; } // El control TabItem de WPF
        public WebView2 LeftWebView { get; set; } // La instancia de WebView2 del panel izquierdo (principal)
        public WebView2 RightWebView { get; set; } // La instancia de WebView2 del panel derecho (puede ser null)
        public TextBlock HeaderTextBlock { get; set; } // El TextBlock que muestra el título en el encabezado de la pestaña
        public Image FaviconImage { get; set; } // El control Image para el favicon
        public Image AudioIconImage { get; set; } // El control Image para el icono de audio

        public bool IsIncognito { get; set; } // Indica si la pestaña está en modo incógnito
        public bool IsSplit { get; set; } // Indica si la pestaña está en modo dividido

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
                    // Actualizar la visibilidad del icono de audio en la UI
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
                    // Actualizar la fuente de la imagen del favicon en la UI
                    if (FaviconImage != null)
                    {
                        FaviconImage.Source = value;
                    }
                }
            }
        }

        // Constructor
        public BrowserTabItem()
        {
            // Inicializar con el icono de globo por defecto
            FaviconSource = GetDefaultGlobeIcon();
            IsAudioPlaying = false; // Por defecto, no hay audio reproduciéndose
        }

        /// <summary>
        /// Carga el icono de globo terráqueo por defecto.
        /// </summary>
        private ImageSource GetDefaultGlobeIcon()
        {
            try
            {
                // Asegúrate de que este archivo exista en la carpeta Resources de tu proyecto
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
            // Fallback a un icono genérico si no se encuentra el archivo
            return new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/globe_icon.png")); // Fallback para recursos embebidos
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
            // Fallback
            return new BitmapImage(new Uri("pack://application:,,,/NavegadorWeb;component/Resources/audio_playing_icon.png")); // Fallback para recursos embebidos
        }
    }
}
