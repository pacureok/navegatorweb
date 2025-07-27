// This file contains the code-behind for the ExtensionsWindow.xaml.
// It manages the display and interaction with the browser extensions.

using System.Windows;
using NavegadorWeb.Extensions; // Required to access the ExtensionManager and CustomExtension classes

namespace NavegadorWeb
{
    /// <summary>
    /// Interaction logic for ExtensionsWindow.xaml
    /// </summary>
    public partial class ExtensionsWindow : Window
    {
        // Public property to hold the ExtensionManager instance, allowing data binding in XAML
        public ExtensionManager ExtensionManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the ExtensionsWindow class.
        /// </summary>
        /// <param name="extensionManager">The instance of ExtensionManager from the main window.</param>
        public ExtensionsWindow(ExtensionManager extensionManager)
        {
            InitializeComponent(); // Initializes the UI components defined in ExtensionsWindow.xaml
            ExtensionManager = extensionManager; // Assigns the passed ExtensionManager instance
            this.DataContext = this; // Sets the DataContext for data binding to this window's properties
        }

        /// <summary>
        /// Event handler for the "Close" button click.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Closes the Extensions window
        }
    }
}
