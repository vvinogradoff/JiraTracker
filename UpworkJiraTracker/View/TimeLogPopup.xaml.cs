using System.Windows;
using UpworkJiraTracker.ViewModel;

namespace UpworkJiraTracker.View;

/// <summary>
/// Popup for displaying time log data (opened from main window timer click)
/// </summary>
public partial class TimeLogPopup : Window
{
    public TimeLogPopup(TimeLogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Close();
    }
}
