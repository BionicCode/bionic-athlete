namespace BionicCode.Utilities.Net;

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

/// <summary>
/// A reusable command that encapsulates the implementation of <see cref="ICommand"/> with support for async/await command delegates. 
/// <br/>Enables instant creation of an ICommand without implementing the ICommand interface for each command.
/// The <see cref="AsyncRelayCommand{TParam}"/> accepts asynchronous command handlers and supports data binding to properties like <see cref="AsyncRelayCommandCore.IsExecuting"/> by implementing <see cref="INotifyPropertyChanged"/>.
/// <br/>Call and await the <see cref="IAsyncRelayCommandCommon.ExecuteAsync()"/> method or one of its overloads to execute the command explicitly asynchronously.
/// </summary>
/// <remarks><c>AsyncRelayCommandCommon</c> implements <see cref="System.Windows.Input.ICommand" />. In case the <see cref="AsyncRelayCommand{TParam}"/> is executed explicitly, especially with an asynchronous command handler registered, it is highly recommended to invoke the awaitable <see cref="AsyncRelayCommandCommon.ExecuteAsync()"/> or its overloads instead.</remarks>
public class AsyncRelayCommand<TParam> : AsyncRelayCommandCommon<TParam>, IAsyncRelayCommand<TParam>
{
    private bool _isCommandManagerRequerySuggestedEnabled;
    /// <inheritdoc />
    public bool IsCommandManagerRequerySuggestedEnabled
    {
        get => _isCommandManagerRequerySuggestedEnabled;
        set
        {
            if (value == IsCommandManagerRequerySuggestedEnabled)
            {
                return;
            }

            _isCommandManagerRequerySuggestedEnabled = value;
            if (IsCommandManagerRequerySuggestedEnabled)
            {
                // CommandManager internally uses a WeakEventManager to register the event handlers
                CommandManager.RequerySuggested += OnCommandManagerRequerySuggested;
            }
            else
            {
                CommandManager.RequerySuggested -= OnCommandManagerRequerySuggested;
            }

            OnPropertyChanged();
        }
    }

    #region Constructors

    /// <inheritdoc />
    public AsyncRelayCommand(Func<TParam, CancellationToken, Task> executeAsync) : base(executeAsync) => IsCommandManagerRequerySuggestedEnabled = true;

    /// <inheritdoc />
    public AsyncRelayCommand(Func<TParam, Task> executeAsync) : base(executeAsync) => IsCommandManagerRequerySuggestedEnabled = true;

    /// <inheritdoc />
    public AsyncRelayCommand(Func<TParam, Task> executeAsync, Func<TParam, bool> canExecute) : base(executeAsync, canExecute) => IsCommandManagerRequerySuggestedEnabled = true;

    /// <inheritdoc />
    public AsyncRelayCommand(Func<TParam, CancellationToken, Task> executeAsync, Func<TParam, bool> canExecute) : base(executeAsync, canExecute) => IsCommandManagerRequerySuggestedEnabled = true;

    /// <summary>
    ///   Creates a new asynchronous command that supports cancellation and accepts a command parameter of <typeparamref name="TParam"/>.
    /// </summary>
    /// <param name="executeAsync">The awaitable execute handler.</param>
    /// <param name="canExecute">The can execute handler.</param>
    /// <param name="isCommandManagerRequerySuggestedEnabled"><see langword="true"/> to enable the WPF framework to raise the CanExecuteChanged event via the <see cref="CommandManager.RequerySuggested"/> event. 
    /// <br/><see langword="false"/> to only raise the <see cref="ICommand.CanExecuteChanged"/> event manually by calling <see cref="IAsyncRelayCommandCore.InvalidateCommand"/>.
    /// <br/>The behavior can be changed anytime by setting the <see cref="IsCommandManagerRequerySuggestedEnabled"/> property.</param>
    public AsyncRelayCommand(Func<TParam, CancellationToken, Task> executeAsync, Func<TParam, bool> canExecute, bool isCommandManagerRequerySuggestedEnabled) : base(executeAsync, canExecute) => IsCommandManagerRequerySuggestedEnabled = isCommandManagerRequerySuggestedEnabled;

    /// <summary>
    ///   Creates a new asynchronous command that accepts a command parameter of type <typeparamref name="TParam"/>.
    /// </summary>
    /// <param name="executeAsync">The awaitable execute handler.</param>
    /// <param name="canExecute">The can execute handler.</param>
    /// <param name="isCommandManagerRequerySuggestedEnabled"><see langword="true"/> to enable the WPF framework to raise the CanExecuteChanged event via the <see cref="CommandManager.RequerySuggested"/> event. 
    /// <br/><see langword="false"/> to only raise the <see cref="ICommand.CanExecuteChanged"/> event manually by calling <see cref="IAsyncRelayCommandCore.InvalidateCommand"/>.
    /// <br/>The behavior can be changed anytime by setting the <see cref="IsCommandManagerRequerySuggestedEnabled"/> property.</param>
    public AsyncRelayCommand(Func<TParam, Task> executeAsync, Func<TParam, bool> canExecute, bool isCommandManagerRequerySuggestedEnabled) : base(executeAsync, canExecute) => IsCommandManagerRequerySuggestedEnabled = isCommandManagerRequerySuggestedEnabled;

    #endregion Constructors

    /// <summary>
    /// Event invocator. Called when <see cref="IAsyncRelayCommand.IsCommandManagerRequerySuggestedEnabled"/> is <see langword="true"/> and the <see cref="CommandManager.RequerySuggested"/> is raised.
    /// </summary>
    /// <param name="sender"><see langword="null"/> because the source event is the static <see cref="CommandManager.RequerySuggested"/> event.</param>
    /// <param name="e">The event args object.</param>
    protected virtual void OnCommandManagerRequerySuggested(object? sender, EventArgs e)
         => OnCanExecuteChanged(sender, e);
}
