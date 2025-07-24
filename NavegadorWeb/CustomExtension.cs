using System;
using System.IO;
using System.ComponentModel; // Para INotifyPropertyChanged

namespace NavegadorWeb
{
    /// <summary>
    /// Representa una extensión personalizada para el navegador.
    /// Contiene metadatos y la ruta al script JavaScript de la extensión.
    /// </summary>
    public class CustomExtension : INotifyPropertyChanged
    {
        public string Id { get; set; } // Identificador único de la extensión
        public string Name { get; set; }
        public string Description { get; set; }
        public string ScriptFilePath { get; set; } // Ruta al archivo .js de la extensión

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public CustomExtension(string id, string name, string description, string scriptFilePath, bool isEnabled = true)
        {
            Id = id;
            Name = name;
            Description = description;
            ScriptFilePath = scriptFilePath;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Carga el contenido del script JavaScript de la extensión.
        /// </summary>
        public string LoadScriptContent()
        {
            if (File.Exists(ScriptFilePath))
            {
                return File.ReadAllText(ScriptFilePath);
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
