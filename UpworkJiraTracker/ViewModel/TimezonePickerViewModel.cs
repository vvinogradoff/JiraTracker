using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UpworkJiraTracker.Helper;

namespace UpworkJiraTracker.ViewModel;

public class TimezonePickerViewModel : INotifyPropertyChanged
{
    private TimeZoneInfo? _selectedTimezone;
    private string _timezoneCaption = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? AddRequested;
    public event EventHandler? CancelRequested;

    public ICommand AddCommand { get; }
    public ICommand CancelCommand { get; }

    public ObservableCollection<TimeZoneInfo> Timezones { get; } = new();

    public TimeZoneInfo? SelectedTimezone
    {
        get => _selectedTimezone;
        set
        {
            if (_selectedTimezone != value)
            {
                _selectedTimezone = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimezoneCaption
    {
        get => _timezoneCaption;
        set
        {
            if (_timezoneCaption != value)
            {
                _timezoneCaption = value;
                OnPropertyChanged();
            }
        }
    }

    public TimezonePickerViewModel()
    {
        AddCommand = new RelayCommand(_ => Add());
        CancelCommand = new RelayCommand(_ => CancelRequested?.Invoke(this, EventArgs.Empty));

        // Load all timezones
        var timezones = TimeZoneInfo.GetSystemTimeZones();
        foreach (var tz in timezones)
        {
            Timezones.Add(tz);
        }

        // Select first timezone
        if (Timezones.Count > 0)
        {
            SelectedTimezone = Timezones[0];
        }
    }

    private void Add()
    {
        if (SelectedTimezone == null)
        {
            System.Windows.MessageBox.Show("Please select a timezone.", "Validation",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        AddRequested?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
