using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace NavegadorWeb
{
    public partial class GeminiDataViewerWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<CapturedPageData> _capturedData;
        public ObservableCollection<CapturedPageData> CapturedData
        {
            get => _capturedData;
            set
            {
                if (_capturedData != value)
                {
                    _capturedData = value;
                    OnPropertyChanged(nameof(CapturedData));
                }
            }
        }

        private string _userQuestion;
        public string UserQuestion
        {
            get => _userQuestion;
            set
            {
                if (_userQuestion != value)
                {
                    _userQuestion = value;
                    OnPropertyChanged(nameof(UserQuestion));
                }
            }
        }

        public bool ShouldRestoreSession { get; private set; }

        // Implementación explícita del evento PropertyChanged para INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public GeminiDataViewerWindow(ObservableCollection<BrowserTabItem> browserTabs)
        {
            InitializeComponent();
            this.DataContext = this;
            CapturedData = new ObservableCollection<CapturedPageData>();
            UserQuestionTextBox.Text = ""; // Inicializar el TextBox de la pregunta del usuario

            // Llenar CapturedData con los datos de las pestañas seleccionadas
            // Esta parte se llenará cuando el usuario seleccione las pestañas en la ventana principal
            // y haga clic en "Enviar a Gemini"
        }

        public GeminiDataViewerWindow(string userQuestion, ObservableCollection<CapturedPageData> capturedData)
        {
            InitializeComponent();
            this.DataContext = this;
            UserQuestion = userQuestion;
            CapturedData = capturedData;
            UserQuestionTextBox.Text = userQuestion; // Mostrar la pregunta del usuario
            // Asegúrate de que CapturedDataPanel se vincule correctamente a CapturedData en XAML
        }


        private void SendToGeminiButton_Click(object sender, RoutedEventArgs e)
        {
            // Aquí se enviaría la información a Gemini.
            // La lógica para la llamada a la API de Gemini se manejaría en MainWindow.xaml.cs
            // Esta ventana solo se encarga de mostrar los datos a enviar y obtener la pregunta del usuario.
            this.DialogResult = true; // Indica que el usuario hizo clic en "Enviar"
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indica que el usuario hizo clic en "Cancelar"
            this.Close();
        }
    }
}
