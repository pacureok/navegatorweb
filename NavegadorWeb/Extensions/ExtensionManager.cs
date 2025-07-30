// This file defines the ExtensionManager class, which manages a collection of CustomExtension instances.
// It handles loading predefined extensions.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using NavegadorWeb.Extensions; // For CustomExtension

namespace NavegadorWeb.Extensions
{
    /// <summary>
    /// Manages a collection of CustomExtension instances.
    /// Implements INotifyPropertyChanged to enable data binding for the Extensions collection.
    /// </summary>
    public class ExtensionManager : INotifyPropertyChanged
    {
        // ObservableCollection to hold all loaded extensions, for UI binding
        public ObservableCollection<CustomExtension> Extensions { get; set; } = new ObservableCollection<CustomExtension>();

        /// <summary>
        /// Initializes a new instance of the ExtensionManager class and loads extensions.
        /// </summary>
        public ExtensionManager()
        {
            LoadExtensions();
        }

        /// <summary>
        /// Loads predefined extensions into the Extensions collection.
        /// Assumes extension JavaScript files are in the application's base directory.
        /// </summary>
        public void LoadExtensions()
        {
            Extensions.Clear(); // Clear existing extensions before loading

            // Add predefined extensions.
            // Ensure that the JS files for these extensions exist in the output directory (bin/Debug/...)
            Extensions.Add(new CustomExtension("text_extraction_extension", "Text Extractor", "Extracts the main text content from the page.", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TextExtractor.js")));
            Extensions.Add(new CustomExtension("ad_blocker_extension", "Ad Blocker", "Blocks ads and trackers.", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AdBlocker.js")));
            // Add more extensions here as needed
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
    }
}
