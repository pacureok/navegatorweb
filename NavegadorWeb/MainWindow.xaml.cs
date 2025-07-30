using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using System.Text.Json;
using System.Speech.Synthesis;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Timers;

using NavegadorWeb.Classes;
using NavegadorWeb.Extensions;
using NavegadorWeb.Services;
using NavegadorWeb.Windows; // ¡MUY IMPORTANTE! Asegúrate de que esta línea esté presente.
using NavegadorWeb.Converters; // ¡MUY IMPORTANTE! Asegúrate de que esta línea esté presente.

namespace NavegadorWeb
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.InitializeBrowser();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ViewModel.SaveSessionCommand.Execute(null);
        }

        // Importaciones necesarias para manipular la ventana sin el estilo de borde por defecto.
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        private const int WM_SYSCOMMAND = 0x112;
        private const uint SC_RESTORE = 0xF120;
        private const uint SC_MOVE = 0xF010;
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            IntPtr sysMenu = GetSystemMenu(handle, false);
            if (sysMenu != IntPtr.Zero)
            {
                EnableMenuItem(sysMenu, SC_MOVE, MF_BYCOMMAND | MF_GRAYED);
            }
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeButton.Visibility = Visibility.Collapsed;
                RestoreButton.Visibility = Visibility.Visible;
                this.BorderThickness = new Thickness(0); // Eliminar el borde en maximizado
            }
            else
            {
                MaximizeButton.Visibility = Visibility.Visible;
                RestoreButton.Visibility = Visibility.Collapsed;
                this.BorderThickness = new Thickness(1); // Restaurar borde normal
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;
                }
            }
            else if (e.LeftButton == MouseButtonState.Pressed && this.WindowState == WindowState.Normal)
            {
                this.DragMove();
            }
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.NavigateCommand.Execute(AddressBar.Text);
                Keyboard.ClearFocus(); // Quita el foco de la barra de direcciones
            }
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.FindNextCommand.Execute(null); // O FindCommand.Execute(FindTextBox.Text)
            }
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // La lógica de búsqueda se maneja en el ViewModel a través del binding TwoWay de FindSearchText
            // y el comando FindCommand.
            // No es necesario llamar a FindCommand aquí, ya que el binding TwoWay con UpdateSourceTrigger=PropertyChanged
            // en el XAML de FindTextBox ya lo maneja al actualizar FindSearchText en el ViewModel.
            // ViewModel.FindCommand.Execute(FindTextBox.Text); // Esta línea es redundante si el binding es correcto.
        }

        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
            // El SelectedItem del TabControl ya está enlazado TwoWay a SelectedTabItem en el ViewModel.
            // Esta función se puede usar para lógica de UI específica que no se maneje en el ViewModel.
            // Por ejemplo, si quieres que la barra de direcciones se actualice inmediatamente al cambiar de pestaña. 
            if (ViewModel.SelectedTabItem != null)
            {
                AddressBar.Text = ViewModel.SelectedTabItem.Url;
            }
        }
    }
}
