using System.Windows;
using UpworkJiraTracker.ViewModel;

namespace UpworkJiraTracker.XAML;

public partial class TimezonePickerDialog : Window
{
    private readonly TimezonePickerViewModel _viewModel;

    public TimeZoneInfo? SelectedTimezone { get; private set; }
    public string TimezoneCaption { get; private set; } = "";

    public TimezonePickerDialog()
    {
        InitializeComponent();

        _viewModel = new TimezonePickerViewModel();
        DataContext = _viewModel;

        _viewModel.AddRequested += OnAddRequested;
        _viewModel.CancelRequested += OnCancelRequested;

        CaptionTextBox.Focus();
    }

    private void OnAddRequested(object? sender, EventArgs e)
    {
        TimezoneCaption = _viewModel.TimezoneCaption.Trim();
        SelectedTimezone = _viewModel.SelectedTimezone;

        DialogResult = true;
        Close();
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
