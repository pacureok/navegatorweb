using System;
using System.ComponentModel;
using System.IO;

namespace NavegadorWeb.Extensions
{
    public class CustomExtension : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ScriptPath { get; set; }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public CustomExtension(string id, string name, string description, string scriptPath)
        {
            Id = id;
            Name = name;
            Description = description;
            ScriptPath = scriptPath;
            IsEnabled = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string LoadScriptContent()
        {
            try
            {
                if (File.Exists(ScriptPath))
                {
                    return File.ReadAllText(ScriptPath);
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el script de la extensión '{Name}': {ex.Message}");
                return string.Empty;
            }
        }
    }
}
