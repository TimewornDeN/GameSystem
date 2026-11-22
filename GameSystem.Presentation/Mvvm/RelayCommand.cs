using System.Windows.Input;

namespace GameSystem.Presentation.Mvvm;

/// <summary>
/// A command that runs a delegate. Buttons are bound to it; whether a button is enabled is decided by a separate
/// bool property of the view model, so the command itself is always executable.
/// </summary>
public sealed class RelayCommand(Action<object?> execute) : ICommand
{
    private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public RelayCommand(Action execute) : this(_ => execute())
    {
    }

    // The command can always run, so this event never fires.
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute(parameter);
}
