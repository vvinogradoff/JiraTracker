using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using UpworkJiraTracker.Helper;
using UpworkJiraTracker.Model;
using UpworkJiraTracker.Service;

namespace UpworkJiraTracker.ViewModel;

/// <summary>
/// ViewModel for displaying time log data in a reusable control
/// </summary>
public class TimeLogViewModel : INotifyPropertyChanged
{
    private readonly TimeLogService _timeLogService;
    private ObservableCollection<TimeLogWeek> _weeks = new();
    private bool _isLoading;

    public TimeLogViewModel(TimeLogService timeLogService)
    {
        _timeLogService = timeLogService;
        RefreshCommand = new RelayCommand(ExecuteRefresh);
        _ = LoadDataAsync(); // Fire and forget - starts loading in background
    }

    public ObservableCollection<TimeLogWeek> Weeks
    {
        get => _weeks;
        set
        {
            _weeks = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public ICommand RefreshCommand { get; }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var weeks = await _timeLogService.LoadAllDataAsync();
            Weeks = new ObservableCollection<TimeLogWeek>(weeks);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load time log data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void ExecuteRefresh(object? args)
    {
        _timeLogService.InvalidateCache();
        await LoadDataAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
