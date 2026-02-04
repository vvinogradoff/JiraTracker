using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UpworkJiraTracker.Helper;

namespace UpworkJiraTracker.ViewModel;

public class WorklogInputViewModel : INotifyPropertyChanged
{
    private string _workDescription = string.Empty;
    private string _etaText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SubmitRequested;
    public event EventHandler? CancelRequested;

    public ICommand SubmitCommand { get; }
    public ICommand CancelCommand { get; }

    public string WorkDescription
    {
        get => _workDescription;
        set
        {
            if (_workDescription != value)
            {
                _workDescription = value;
                OnPropertyChanged();
            }
        }
    }

    public string EtaText
    {
        get => _etaText;
        set
        {
            if (_etaText != value)
            {
                _etaText = value;
                OnPropertyChanged();
            }
        }
    }

    public double? RemainingEstimateHours { get; private set; }

    public WorklogInputViewModel()
    {
        SubmitCommand = new RelayCommand(_ => Submit());
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    private void Submit()
    {
        // Parse ETA
        var etaText = EtaText?.Trim();
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

        SubmitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
