using System.Windows;
using System.Windows.Input;

namespace UtilityApplication.Commands
{
    /// <summary>
    /// A reusable command that supports async execution and state toggling.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> execute;
        private readonly Func<bool> canExecute;

        public RelayCommand(Func<Task> execute, Func<bool> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return this.canExecute();
        }

        public async void Execute(object? parameter)
        {
            try
            {
                await this.execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RelayCommand] Unhandled exception: {ex.Message}");
                Console.WriteLine(ex.StackTrace);

                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}