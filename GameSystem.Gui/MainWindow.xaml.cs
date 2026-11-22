using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using GameSystem.Presentation;

namespace GameSystem.Gui;

/// <summary>
/// The window has no game logic. This file only does two things that XAML cannot: keeps the output scrolled to
/// the newest line and returns the keyboard focus to the input line when the game asks for input again.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is MainViewModel viewModel)
                viewModel.Lines.CollectionChanged += OnLinesChanged;
        };

        InputBox.IsEnabledChanged += (_, _) =>
        {
            if (InputBox.IsEnabled)
                InputBox.Focus();
        };
    }

    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(OutputScroll.ScrollToEnd));
}
