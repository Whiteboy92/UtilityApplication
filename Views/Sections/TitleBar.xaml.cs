using System.Windows;
using System.Windows.Input;

namespace UtilityApplication.Views.Sections
{
    public partial class TitleBar
    {
        public TitleBar()
        {
            InitializeComponent();
        }

        // DependencyProperty to bind title text
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBar), new PropertyMetadata(""));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // Raise dragging on TextBlock mouse down
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Window.GetWindow(this)?.DragMove();
        }

        // Button click handlers for minimize, maximize/restore, close:
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)!.WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this)!;
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this)!;
            window.Close();
        }
    }
}