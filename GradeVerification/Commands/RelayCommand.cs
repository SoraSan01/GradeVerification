using System;
using System.Windows.Input;

namespace GradeVerification.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        private EventHandler _canExecuteChangedHandlers;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) =>
            _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) =>
            _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add
            {
                _canExecuteChangedHandlers += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _canExecuteChangedHandlers -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            // Raise our own event handlers (in addition to CommandManager.RequerySuggested)
            _canExecuteChangedHandlers?.Invoke(this, EventArgs.Empty);
        }
    }
}
