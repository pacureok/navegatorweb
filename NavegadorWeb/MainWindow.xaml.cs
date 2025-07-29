using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace NavegadorWeb
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeRestoreButton.Content = "❐";
            }
            else
            {
                MaximizeRestoreButton.Content = "⬜";
            }
        }

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeRestoreButton_Click(sender, e);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TitleBarGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeRestoreButton_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string url = AddressBar.Text;
                Keyboard.ClearFocus();
            }
        }

        private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
        {
            AddressBar.SelectAll();
        }

        private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        private void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = (FindBar.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            if (FindBar.Visibility == Visibility.Visible)
            {
                FindTextBox.Focus();
                FindTextBox.SelectAll();
            }
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNextButton_Click(sender, e);
            }
        }

        private void FindPreviousButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void CloseFindBarButton_Click(object sender, RoutedEventArgs e)
        {
            FindBar.Visibility = Visibility.Collapsed;
        }

        private void BrowserTabControl_SelectionChanged_Grouped(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
