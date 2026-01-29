using System.Windows;
using System.Windows.Input;

namespace UpworkJiraTracker.View;

public partial class WorklogInputWindow : Window
{
    /// <summary>
    /// Result indicating whether user submitted values or cancelled.
    /// </summary>
    public bool Submitted { get; private set; }

    /// <summary>
    /// Work description entered by user (may be empty).
    /// </summary>
    public string WorkDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Remaining estimate in hours entered by user (null if not provided or invalid).
    /// </summary>
    public double? RemainingEstimateHours { get; private set; }

    public WorklogInputWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Position the window above the specified element.
    /// </summary>
    public void PositionAbove(Window owner)
    {
        // Get owner's screen position
        var ownerLeft = owner.Left;
        var ownerTop = owner.Top;

        // Position this window above the owner, aligned to left
        Left = ownerLeft;
        Top = ownerTop - ActualHeight - 4;

        // Ensure window is on screen
        var workArea = SystemParameters.WorkArea;

        // If would go above screen, position below instead
        if (Top < workArea.Top)
        {
            Top = ownerTop + owner.ActualHeight + 4;
        }

        // Ensure left edge is on screen
        if (Left < workArea.Left)
        {
            Left = workArea.Left;
        }

        // Ensure right edge is on screen
        if (Left + ActualWidth > workArea.Right)
        {
            Left = workArea.Right - ActualWidth;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Focus on description field
        DescriptionTextBox.Focus();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // Cancel - don't submit values
            Submitted = false;
            WorkDescription = string.Empty;
            RemainingEstimateHours = null;
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            Submit();
            e.Handled = true;
        }
    }

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        Submit();
    }

    private void Submit()
    {
        Submitted = true;
        WorkDescription = DescriptionTextBox.Text?.Trim() ?? string.Empty;

        // Parse ETA
        var etaText = EtaTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(etaText))
        {
            // Try parsing as decimal hours (e.g., "2.5" or "2,5")
            etaText = etaText.Replace(',', '.');
            if (double.TryParse(etaText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var hours) && hours >= 0)
            {
                RemainingEstimateHours = hours;
            }
        }

        Close();
    }
}
