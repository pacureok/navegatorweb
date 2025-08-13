using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NavegadorWeb.Classes;

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPasswordsList();
        }

        private void LoadPasswordsList()
        {
            var passwords = PasswordManager.GetAllPasswords();
            var displayList = new List<PasswordDisplayEntry>();
            foreach (var p in passwords)
            {
                displayList.Add(new PasswordDisplayEntry
                {
                    Url = p.Url ?? string.Empty,
                    Username = p.Username ?? string.Empty,
                    DecryptedPassword = PasswordManager.DecryptPassword(p.EncryptedPassword ?? string.Empty) ?? string.Empty,
                    OriginalEntry = p
                });
            }
            PasswordsListView.ItemsSource = displayList;
        }

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
                    LoadPasswordsList();
                    MessageBox.Show("Contraseña eliminada con éxito.", "Eliminación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private class PasswordDisplayEntry
        {
            public string Url { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string DecryptedPassword { get; set; } = string.Empty;
            public PasswordEntry OriginalEntry { get; set; } = new PasswordEntry();
        }
    }
}
