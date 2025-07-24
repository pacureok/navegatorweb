using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json; // Para serializar/deserializar el estado
using System.Windows; // Para MessageBox, aunque es mejor evitarlo en clases de lógica

namespace NavegadorWeb
{
    /// <summary>
    /// Gestiona la carga, almacenamiento y estado de las extensiones personalizadas.
    /// </summary>
    public class ExtensionManager
    {
        public ObservableCollection<CustomExtension> Extensions { get; set; }
        private const string ExtensionsConfigFileName = "extensions_config.json";

        public ExtensionManager()
        {
            Extensions = new ObservableCollection<CustomExtension>();
            LoadExtensions();
        }

        /// <summary>
        /// Carga las extensiones predefinidas y su estado guardado.
        /// </summary>
        private void LoadExtensions()
        {
            // Definir las extensiones disponibles (puedes añadir más aquí)
            // Asegúrate de que los archivos .js existan en la raíz de tu aplicación
            string highlighterScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HighlighterExtension.js");
            string exampleExtensionScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExampleExtension.js"); // Si creas otra

            Extensions.Add(new CustomExtension("highlighter", "Resaltador de Texto", "Resalta la palabra 'agua' en las páginas.", highlighterScriptPath));
            // Extensions.Add(new CustomExtension("example", "Ejemplo de Extensión", "Una extensión de ejemplo.", exampleExtensionScriptPath));

            // Cargar el estado guardado de las extensiones
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExtensionsConfigFileName);
            if (File.Exists(configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(configFilePath);
                    var savedStates = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);

                    foreach (var ext in Extensions)
                    {
                        if (savedStates.TryGetValue(ext.Id, out bool isEnabled))
                        {
                            ext.IsEnabled = isEnabled;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Error al cargar la configuración de extensiones: {ex.Message}", "Error de Configuración", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Guarda el estado actual (habilitado/deshabilitado) de las extensiones.
        /// </summary>
        public void SaveExtensionsState()
        {
            var statesToSave = Extensions.ToDictionary(ext => ext.Id, ext => ext.IsEnabled);
            string json = JsonSerializer.Serialize(statesToSave, new JsonSerializerOptions { WriteIndented = true });
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExtensionsConfigFileName);
            File.WriteAllText(configFilePath, json);
        }

        /// <summary>
        /// Obtiene una extensión por su ID.
        /// </summary>
        public CustomExtension GetExtensionById(string id)
        {
            return Extensions.FirstOrDefault(ext => ext.Id == id);
        }

        /// <summary>
        /// Retorna una lista de todas las extensiones habilitadas.
        /// </summary>
        public IEnumerable<CustomExtension> GetEnabledExtensions()
        {
            return Extensions.Where(ext => ext.IsEnabled);
        }
    }
}
