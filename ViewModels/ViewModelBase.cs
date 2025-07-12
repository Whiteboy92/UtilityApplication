using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UtilityApplication.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the field to the new value and raises PropertyChanged if different.
        /// Optionally raise PropertyChanged for additional property names.
        /// </summary>
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null,
            params string[]? additionalProperties)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);

            if (additionalProperties != null)
            {
                foreach (var prop in additionalProperties)
                    OnPropertyChanged(prop);
            }

            return true;
        }
    }
}