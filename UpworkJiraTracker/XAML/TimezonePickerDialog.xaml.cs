using System.Windows;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

namespace UpworkJiraTracker.XAML;

public partial class TimezonePickerDialog : Window
{
    public TimeZoneInfo? SelectedTimezone { get; private set; }
    public string TimezoneCaption { get; private set; } = "";

    public TimezonePickerDialog()
    {
        InitializeComponent();

        // Load all timezones
        var timezones = TimeZoneInfo.GetSystemTimeZones();
        TimezoneComboBox.ItemsSource = timezones;

        // Select first timezone
        if (timezones.Count > 0)
        {
            TimezoneComboBox.SelectedIndex = 0;
        }

        CaptionTextBox.Focus();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (TimezoneComboBox.SelectedItem == null)
        {
            WpfMessageBox.Show("Please select a timezone.", "Validation", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        TimezoneCaption = CaptionTextBox.Text.Trim();
        SelectedTimezone = TimezoneComboBox.SelectedItem as TimeZoneInfo;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
