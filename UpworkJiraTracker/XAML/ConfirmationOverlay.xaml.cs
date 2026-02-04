using System.Windows;
using WpfUserControl = System.Windows.Controls.UserControl;
using UpworkJiraTracker.ViewModel;

namespace UpworkJiraTracker.XAML;

public partial class ConfirmationOverlay : WpfUserControl
{
    private readonly ConfirmationOverlayViewModel _viewModel;

    public event EventHandler? Confirmed;
    public event EventHandler? Cancelled;

    public ConfirmationOverlay()
    {
        InitializeComponent();

        _viewModel = new ConfirmationOverlayViewModel();
        DataContext = _viewModel;

        _viewModel.ConfirmRequested += (s, e) => Confirmed?.Invoke(this, EventArgs.Empty);
        _viewModel.CancelRequested += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Set the title, message, and confirm button text for the overlay.
    /// </summary>
    public void SetMessage(string title, string message, string confirmButtonText = "Confirm")
    {
        _viewModel.Title = title;
        _viewModel.Message = message;
        _viewModel.ConfirmButtonText = confirmButtonText;
    }

    /// <summary>
    /// Reset the overlay to default exit confirmation.
    /// </summary>
    public void ResetToDefault()
    {
        _viewModel.Title = "Confirm Exit";
        _viewModel.Message = "Are you sure you want to close the application?";
        _viewModel.ConfirmButtonText = "Exit";
    }
}
