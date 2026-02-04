using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UpworkJiraTracker.Helper;

namespace UpworkJiraTracker.ViewModel;

public class ConfirmationOverlayViewModel : INotifyPropertyChanged
{
    private string _title = "Confirm Exit";
    private string _message = "Are you sure you want to close the application?";
    private string _confirmButtonText = "Exit";

    public event EventHandler? ConfirmRequested;
    public event EventHandler? CancelRequested;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public string Message
    {
        get => _message;
        set { _message = value; OnPropertyChanged(); }
    }

    public string ConfirmButtonText
    {
        get => _confirmButtonText;
        set { _confirmButtonText = value; OnPropertyChanged(); }
    }

    public ConfirmationOverlayViewModel()
    {
        ConfirmCommand = new RelayCommand(_ => ConfirmRequested?.Invoke(this, EventArgs.Empty));
        CancelCommand = new RelayCommand(_ => CancelRequested?.Invoke(this, EventArgs.Empty));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
