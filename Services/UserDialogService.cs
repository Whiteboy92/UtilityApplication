using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UtilityApplication.Interfaces;
using UtilityApplication.Views.Sections;

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
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                Owner = Application.Current?.MainWindow,
                Background = (Brush)new BrushConverter().ConvertFromString("#444444")!,
                AllowsTransparency = false,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
            };

            if (Application.Current != null)
            {
                foreach (var dict in Application.Current.Resources.MergedDictionaries)
                {
                    window.Resources.MergedDictionaries.Add(dict);
                }
            }

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            var titleBar = new TitleBar
            {
                Title = title,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Height = 30,
            };
            Grid.SetRow(titleBar, 0);
            
            var contentPanel = new StackPanel { Margin = new Thickness(16) };
            Grid.SetRow(contentPanel, 1);

            var messageText = new TextBlock
            {
                Text = message,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 14,
                Foreground = Brushes.White,
            };
            
            var inputBox = new TextBox
            {
                Text = defaultValue,
                Margin = new Thickness(0, 0, 0, 16),
                Style = (Style?)Application.Current?.FindResource("MaterialDesignOutlinedTextBox"),
                Foreground = Brushes.White,
                FontSize = 14,
                Background = (Brush)new BrushConverter().ConvertFromString("#555555")!,
            };
            inputBox.Focus();
            inputBox.SelectAll();

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };


            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                IsDefault = true,
                Style = (Style?)Application.Current?.FindResource("MaterialDesignOutlinedButton"),
                Margin = new Thickness(0),
            };
            okButton.Click += (_, _) => window.DialogResult = true;

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsCancel = true,
                Style = (Style?)Application.Current?.FindResource("MaterialDesignOutlinedButton"),
                Margin = new Thickness(10, 0, 0, 0),
            };
            cancelButton.Click += (_, _) => window.DialogResult = false;

            buttonsPanel.Children.Add(okButton);
            buttonsPanel.Children.Add(cancelButton);

            contentPanel.Children.Add(messageText);
            contentPanel.Children.Add(inputBox);
            contentPanel.Children.Add(buttonsPanel);

            rootGrid.Children.Add(titleBar);
            rootGrid.Children.Add(contentPanel);

            window.Content = rootGrid;

            return window.ShowDialog() == true ? inputBox.Text : null;
        }
    }
}
