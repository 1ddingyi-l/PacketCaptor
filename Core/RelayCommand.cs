using System;
using System.Windows.Input;

namespace DotNetPacketCaptor.Core
{
    //  Generic relay-command, which has a generic parameter
    public class RelayCommand<T> : ICommand
    {
        private readonly Predicate<T> _canExecute;
        private readonly Action<T> _execute;

        public RelayCommand(Action<T> execute)
            => _execute = execute ?? throw new NullReferenceException();

        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            _execute = execute ?? throw new NullReferenceException();
            // canExecute can be null while execute can't
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (parameter != null && !(parameter is T))
                return false;
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
            => _execute((T) parameter);

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecute != null)
                    CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (_canExecute != null)
                    CommandManager.RequerySuggested -= value;
            }
        }
    }
    
    public class RelayCommand : ICommand
    {
        private readonly Func<bool> _canExecute;
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add 
            {
                if (_canExecute != null)
                    CommandManager.RequerySuggested += value;
            }
            remove 
            {
                if (_canExecute != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
            => _execute();
    }
}