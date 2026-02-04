using System.Windows;
using System.Windows.Input;
using UpworkJiraTracker.ViewModel;

namespace UpworkJiraTracker.View;

/// <summary>
/// Window for displaying time log data (opened from settings)
/// </summary>
public partial class TimeLogWindow : Window
{
    public TimeLogWindow(TimeLogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Allow dragging the window
        MouseDown += (s, e) =>
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
