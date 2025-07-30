// This file defines the CustomExtension class, representing a browser extension.
// It holds metadata about the extension and provides a method to load its script content.

using System;
using System.ComponentModel;
using System.IO;

namespace NavegadorWeb.Extensions
{
    /// <summary>
    /// Represents a custom browser extension.
    /// Implements INotifyPropertyChanged to enable data binding for its IsEnabled property.
    /// </summary>
    public class CustomExtension : INotifyPropertyChanged
    {
        public string Id { get; set; } // Unique ID for the extension
        public string Name { get; set; } // Display name of the extension
        public string Description { get; set; } // Description of the extension
        public string ScriptPath { get; set; } // File path to the extension's JavaScript script

        private bool _isEnabled;

        // Public property to toggle the extension's enabled state
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled)); // Notify UI of change
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the CustomExtension class.
        /// </summary>
        /// <param name="id">The unique ID of the extension.</param>
        /// <param name="name">The display name of the extension.</param>
        /// <param name="description">The description of the extension.</param>
        /// <param name="scriptPath">The file path to the extension's JavaScript script.</param>
        public CustomExtension(string id, string name, string description, string scriptPath)
        {
            Id = id;
            Name = name;
            Description = description;
            ScriptPath = scriptPath;
            IsEnabled = false; // Extensions are disabled by default
        }

        // Event for property change notification
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Loads the JavaScript content of the extension from its script file.
        /// </summary>
        /// <returns>The content of the JavaScript file as a string, or an empty string if an error occurs.</returns>
        public string LoadScriptContent()
        {
            try
            {
                if (File.Exists(ScriptPath))
                {
                    return File.ReadAllText(ScriptPath); // Read the script file
                }
                return string.Empty; // Return empty if file not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading script for extension '{Name}': {ex.Message}");
                return string.Empty;
            }
        }
    }
}
