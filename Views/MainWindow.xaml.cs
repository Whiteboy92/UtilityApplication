using System.Windows;
using UtilityApplication.Services;

namespace UtilityApplication.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var dialogService = new UserDialogService();
            this.DataContext = new ViewModels.MainWindowViewModel(dialogService);
        }
    }
}