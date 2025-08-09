using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace NavegadorWeb.Windows.Classes
{
    public class MainViewModel : BaseViewModel
    {
        private ObservableCollection<string> _history;
        public ObservableCollection<string> History
        {
            get => _history;
            set => SetProperty(ref _history, value);
        }

        private ObservableCollection<string> _bookmarks;
        public ObservableCollection<string> Bookmarks
        {
            get => _bookmarks;
            set => SetProperty(ref _bookmarks, value);
        }

        private ObservableCollection<string> _downloadHistory;
        public ObservableCollection<string> DownloadHistory
        {
            get => _downloadHistory;
            set => SetProperty(ref _downloadHistory, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private string _currentPageUrl;
        public string CurrentPageUrl
        {
            get => _currentPageUrl;
            set => SetProperty(ref _currentPageUrl, value);
        }

        private bool _isNavigating;
        public bool IsNavigating
        {
            get => _isNavigating;
            set => SetProperty(ref _isNavigating, value);
        }

        private string _browserStatus;
        public string BrowserStatus
        {
            get => _browserStatus;
            set => SetProperty(ref _browserStatus, value);
        }

        private int _zoomLevel;
        public int ZoomLevel
        {
            get => _zoomLevel;
            set => SetProperty(ref _zoomLevel, value);
        }

        private bool _isAdBlockerEnabled;
        public bool IsAdBlockerEnabled
        {
            get => _isAdBlockerEnabled;
            set => SetProperty(ref _isAdBlockerEnabled, value);
        }

        private bool _isIncognitoMode;
        public bool IsIncognitoMode
        {
            get => _isIncognitoMode;
            set => SetProperty(ref _isIncognitoMode, value);
        }

        private Color _currentPageColor;
        public Color CurrentPageColor
        {
            get => _currentPageColor;
            set => SetProperty(ref _currentPageColor, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand GoForwardCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddBookmarkCommand { get; }
        public ICommand OpenHistoryWindowCommand { get; }
        public ICommand OpenBookmarksWindowCommand { get; }
        public ICommand OpenDownloadsWindowCommand { get; }
        public ICommand OpenSettingsWindowCommand { get; }

        public MainViewModel()
        {
            History = new ObservableCollection<string>();
            Bookmarks = new ObservableCollection<string>();
            DownloadHistory = new ObservableCollection<string>();
            CurrentPageUrl = "about:blank";
            BrowserStatus = "Listo";
            ZoomLevel = 100;

            NavigateCommand = new RelayCommand(url => Navigate(url?.ToString()));
            GoBackCommand = new RelayCommand(_ => GoBack(), _ => CanGoBack());
            GoForwardCommand = new RelayCommand(_ => GoForward(), _ => CanGoForward());
            RefreshCommand = new RelayCommand(_ => Refresh());
            AddBookmarkCommand = new RelayCommand(_ => AddBookmark());
            OpenHistoryWindowCommand = new RelayCommand(_ => OpenHistoryWindow());
            OpenBookmarksWindowCommand = new RelayCommand(_ => OpenBookmarksWindow());
            OpenDownloadsWindowCommand = new RelayCommand(_ => OpenDownloadsWindow());
            OpenSettingsWindowCommand = new RelayCommand(_ => OpenSettingsWindow());
        }

        // Métodos de comando
        private void Navigate(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "http://" + url;
            }

            CurrentPageUrl = url;
            IsNavigating = true;
            BrowserStatus = "Navegando a " + CurrentPageUrl;
            History.Add(CurrentPageUrl);
        }

        private bool CanGoBack()
        {
            // Lógica para determinar si se puede ir hacia atrás
            return History.Count > 1;
        }

        private void GoBack()
        {
            // Lógica para ir a la página anterior
        }

        private bool CanGoForward()
        {
            // Lógica para determinar si se puede ir hacia adelante
            return false; // Implementación pendiente
        }

        private void GoForward()
        {
            // Lógica para ir a la página siguiente
        }

        private void Refresh()
        {
            // Lógica para refrescar la página
        }

        private void AddBookmark()
        {
            // Lógica para añadir un marcador
            if (!Bookmarks.Contains(CurrentPageUrl))
            {
                Bookmarks.Add(CurrentPageUrl);
                BrowserStatus = "Marcador añadido: " + CurrentPageUrl;
            }
        }

        private void OpenHistoryWindow()
        {
            // Lógica para abrir la ventana de historial
        }

        private void OpenBookmarksWindow()
        {
            // Lógica para abrir la ventana de marcadores
        }

        private void OpenDownloadsWindow()
        {
            // Lógica para abrir la ventana de descargas
        }

        private void OpenSettingsWindow()
        {
            // Lógica para abrir la ventana de configuración
        }
    }
}
