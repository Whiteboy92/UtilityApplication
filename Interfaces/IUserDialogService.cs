using System.Windows;

namespace UtilityApplication.Interfaces
{
    public interface IUserDialogService
    {
        void ShowMessage(string message, string title, MessageBoxImage icon);

        string? SelectFolder(string description);

        string? ShowInputDialog(string message, string title, string defaultValue = "");
    }
}