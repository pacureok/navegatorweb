using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NavegadorWeb.Classes; // Assuming this is where PasswordManager and PasswordEntry are

namespace NavegadorWeb
{
    /// <summary>
    /// Lógica de interacción para PasswordManagerWindow.xaml
    /// </summary>
    public partial class PasswordManagerWindow : Window
    {
        public PasswordManagerWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Se ejecuta cuando la ventana se ha cargado.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPasswordsList();
        }

        /// <summary>
        /// Carga la lista de contraseñas y las muestra en la ListView.
        /// </summary>
        private void LoadPasswordsList()
        {
            // Assumed that PasswordManager exists and is correctly implemented
            var passwords = PasswordManager.GetAllPasswords();
            // Para mostrar la contraseña descifrada en el ToolTip, necesitamos una propiedad auxiliar
            var displayList = new List<PasswordDisplayEntry>();
            foreach (var p in passwords)
            {
                displayList.Add(new PasswordDisplayEntry
                {
                    Url = p.Url,
                    Username = p.Username,
                    DecryptedPassword = PasswordManager.DecryptPassword(p.EncryptedPassword),
                    OriginalEntry = p // Guardar la referencia a la entrada original para eliminar
                });
            }
            PasswordsListView.ItemsSource = displayList;
        }

        /// <summary>
        /// Maneja el clic en el botón de eliminar.
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entryToDelete = button?.Tag as PasswordDisplayEntry;

            if (entryToDelete != null)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"¿Estás seguro de que quieres eliminar la contraseña para '{entryToDelete.Username}' en '{entryToDelete.Url}'?",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    PasswordManager.DeletePassword(entryToDelete.OriginalEntry);
                    LoadPasswordsList(); // Recargar la lista
                    MessageBox.Show("Contraseña eliminada con éxito.", "Eliminación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Cierra la ventana.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Clase auxiliar para la visualización en la ListView (para el ToolTip de la contraseña)
        private class PasswordDisplayEntry
        {
            public string Url { get; set; }
            public string Username { get; set; }
            public string DecryptedPassword { get; set; } // Contraseña descifrada para el ToolTip
            public PasswordEntry OriginalEntry { get; set; } // Referencia a la entrada original para eliminar
        }
    }
}
