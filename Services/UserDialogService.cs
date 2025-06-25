using System.Windows;
using System.Windows.Controls;
using UtilityApplication.Interfaces;

namespace UtilityApplication.Services
{
    public class UserDialogService : IUserDialogService
    {
        public void ShowMessage(string message, string title, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        public string? SelectFolder(string description)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false,
            };

            return dialog.ShowDialog() == true ? dialog.SelectedPath : null;
        }
        
        public string? SelectFile(string title, string filter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = false,
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowInputDialog(string message, string title, string defaultValue = "")
        {
            var window = new Window
            {
                Title = title,
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = Application.Current?.MainWindow,
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            var textBlock = new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 10) };
            var textBox = new TextBox { Text = defaultValue, Margin = new Thickness(0, 0, 0, 10) };
            textBox.Focus();
            textBox.SelectAll();

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 75, IsDefault = true };
            var cancelButton = new Button { Content = "Cancel", Width = 75, Margin = new Thickness(10, 0, 0, 0), IsCancel = true };

            okButton.Click += (_, _) => window.DialogResult = true;
            cancelButton.Click += (_, _) => window.DialogResult = false;

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            window.Content = stackPanel;

            bool? result = window.ShowDialog();
            if (result == true)
            {
                return textBox.Text;
            }
            else
            {
                return null;
            }
        }
    }
}
