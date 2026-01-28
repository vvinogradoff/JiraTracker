using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using UpworkJiraTracker.ViewModel;
using UpworkJiraTracker.Service;
using UpworkJiraTracker.Model;
using UpworkJiraTracker.Helper;
using UpworkJiraTracker.Extensions;

namespace UpworkJiraTracker.View;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly DispatcherTimer _topmostTimer;
    private readonly WindowSettingsService _settingsService;
    private SettingsWindow? _settingsWindow;
    private CancellationTokenSource? _searchDebounceTokenSource;

    public MainWindowViewModel ViewModel => _viewModel;

    // Drag tracking
    private bool _isDragging = false;
    private WpfPoint _dragStartPoint;
    private WpfPoint _windowStartPosition;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize ViewModel (it creates its own services)
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        // Keep reference to settings service from ViewModel
        _settingsService = _viewModel.SettingsService;

        // Load window position and size
        var settings = _settingsService.Load();
        Width = settings.WindowWidth;
        Height = settings.WindowHeight;

        // Validate and constrain position within screen bounds
        var (validLeft, validTop) = ConstrainToScreen(settings.WindowLeft, settings.WindowTop, Width, Height);
        Left = validLeft;
        Top = validTop;

        // Ensure window is always on top
        Topmost = true;
        Loaded += OnWindowLoaded;

        // Wire up autocomplete events
        JiraAutocomplete.TextChanged += JiraAutocomplete_TextChanged;
        JiraAutocomplete.TextSubmitted += JiraAutocomplete_TextSubmitted;
        JiraAutocomplete.IssueSelected += JiraAutocomplete_IssueSelected;
        JiraAutocomplete.PreviewKeyDown += JiraAutocomplete_PreviewKeyDown;
        JiraAutocomplete.Cancelled += JiraAutocomplete_Cancelled;

        // Wire up JiraIssuesService events
        _viewModel.JiraIssuesService.SuggestionsUpdated += JiraIssuesService_SuggestionsUpdated;
        _viewModel.JiraIssuesService.LoadingStateChanged += JiraIssuesService_LoadingStateChanged;

        // Initialize topmost enforcement timer
        var intervalSeconds = Properties.Settings.Default.TopmostEnforcementIntervalSeconds;
        if (intervalSeconds < 1) intervalSeconds = 5; // Default to 5 seconds if invalid
        if (intervalSeconds > 60) intervalSeconds = 60; // Cap at 60 seconds

        _topmostTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(intervalSeconds)
        };
        _topmostTimer.Tick += (s, e) => SetTopmostAboveTaskbar();
        _topmostTimer.Start();
    }

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        SetTopmostAboveTaskbar();

        // Initialize async services (don't wait - fire and forget)
        _ = _viewModel.InitializeAsync();
    }

    private static (double Left, double Top) ConstrainToScreen(double left, double top, double width, double height)
    {
        // Use full screen bounds (including taskbar area) since app can be positioned over taskbar
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        // Ensure window is not positioned off the left or top
        if (left < 0)
            left = 0;
        if (top < 0)
            top = 0;

        // Ensure window is not positioned off the right or bottom
        if (left + width > screenWidth)
            left = screenWidth - width;
        if (top + height > screenHeight)
            top = screenHeight - height;

        // Final safety check
        if (left < 0)
            left = 0;
        if (top < 0)
            top = 0;

        return (left, top);
    }

    private void SetTopmostAboveTaskbar()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TOPMOST);

        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    #region Drag Handlers

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Fallback handler
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Check if the click is within the autocomplete control or its popup
        var clickedElement = e.OriginalSource as DependencyObject;
        if (clickedElement != null &&
            !IsDescendantOf(clickedElement, JiraAutocomplete) &&
            !JiraAutocomplete.IsClickInsidePopup(clickedElement))
        {
            // Always try to close popup - it may be stuck open from a previous action
            JiraAutocomplete.ClosePopup();

            if (_viewModel.IsAutocompleteActive)
            {
                _viewModel.HideAutocomplete();
            }
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        // Always try to close popup - it may be stuck open from a previous action
        JiraAutocomplete.ClosePopup();

        if (_viewModel.IsAutocompleteActive)
        {
            _viewModel.HideAutocomplete();
        }
    }

    private static bool IsDescendantOf(DependencyObject element, DependencyObject parent)
    {
        while (element != null)
        {
            if (element == parent)
                return true;
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void UpperArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        StartDrag(sender as System.Windows.Controls.Border, e);
    }

    private void LowerArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        StartDrag(sender as System.Windows.Controls.Border, e);
    }

    private void StartDrag(System.Windows.Controls.Border? border, MouseButtonEventArgs e)
    {
        if (border == null) return;

        if (e.ChangedButton == MouseButton.Left)
        {
            _isDragging = false;

            var screenPoint = PointToScreen(e.GetPosition(this));
            _dragStartPoint = new WpfPoint(screenPoint.X, screenPoint.Y);
            _windowStartPosition = new WpfPoint(Left, Top);

            border.CaptureMouse();
        }
    }

    private void UpperArea_MouseMove(object sender, WpfMouseEventArgs e)
    {
        HandleMouseMove(sender as System.Windows.Controls.Border, e);
    }

    private void LowerArea_MouseMove(object sender, WpfMouseEventArgs e)
    {
        HandleMouseMove(sender as System.Windows.Controls.Border, e);
    }

    private void HandleMouseMove(System.Windows.Controls.Border? border, WpfMouseEventArgs e)
    {
        if (border == null) return;

        if (border.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
        {
            var screenPoint = PointToScreen(e.GetPosition(this));
            WpfPoint currentPoint = new WpfPoint(screenPoint.X, screenPoint.Y);

            double deltaX = currentPoint.X - _dragStartPoint.X;
            double deltaY = currentPoint.Y - _dragStartPoint.Y;

            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            if (distance >= Constants.UI.DragThresholdPixels)
            {
                _isDragging = true;
                Left = _windowStartPosition.X + deltaX;
                Top = _windowStartPosition.Y + deltaY;
            }
        }
    }

    private void UpperArea_MouseUp(object sender, MouseButtonEventArgs e)
    {
        HandleMouseUp(sender as System.Windows.Controls.Border, e, isUpperArea: true);
    }

    private void LowerArea_MouseUp(object sender, MouseButtonEventArgs e)
    {
        HandleMouseUp(sender as System.Windows.Controls.Border, e, isUpperArea: false);
    }

    private void HandleMouseUp(System.Windows.Controls.Border? border, MouseButtonEventArgs e, bool isUpperArea)
    {
        if (border == null) return;

        if (border.IsMouseCaptured)
        {
            border.ReleaseMouseCapture();

            if (!_isDragging && e.ChangedButton == MouseButton.Left)
            {
                if (isUpperArea)
                {
                    ShowJiraAutocomplete();
                }
                else
                {
                    ShowSettingsWindow();
                }
            }
            else if (_isDragging)
            {
                _settingsService.SavePosition(Left, Top);
            }

            _isDragging = false;
        }
    }

    #endregion

    #region Autocomplete

    private async void ShowJiraAutocomplete()
    {
		if (!_viewModel.ShowAutocomplete())
			return;

        JiraAutocomplete.Text = "";

        // Determine popup placement based on window position
        var workArea = SystemParameters.WorkArea;
        var windowBottom = Top + Height;
        var spaceBelow = workArea.Bottom - windowBottom;
        var spaceAbove = Top - workArea.Top;

        if (spaceAbove > spaceBelow || windowBottom > workArea.Height / 2)
        {
            JiraAutocomplete.SetPopupPlacement(System.Windows.Controls.Primitives.PlacementMode.Top);
        }
        else
        {
            JiraAutocomplete.SetPopupPlacement(System.Windows.Controls.Primitives.PlacementMode.Bottom);
        }

        JiraAutocomplete.Focus();

        await _viewModel.JiraIssuesService.LoadDefaultSuggestionsAsync();
    }

    private void JiraIssuesService_SuggestionsUpdated(object? sender, List<JiraIssue> suggestions)
    {
        JiraAutocomplete.UpdateSuggestions(suggestions);
    }

    private void JiraIssuesService_LoadingStateChanged(object? sender, bool isLoading)
    {
        if (isLoading)
        {
            JiraAutocomplete.ShowLoading();
        }
        else
        {
            JiraAutocomplete.HideLoading();
        }
    }

    private async void JiraAutocomplete_TextChanged(object? sender, string text)
    {
        // If cache has issues, search instantly (no debounce)
        if (_viewModel.JiraIssuesService.HasCachedIssues)
        {
            _viewModel.JiraIssuesService.SearchFromCache(text);
            return;
        }

        // Cache is empty - fall back to API search with debounce
        _searchDebounceTokenSource?.Cancel();
        _searchDebounceTokenSource = new CancellationTokenSource();
        var token = _searchDebounceTokenSource.Token;

        try
        {
            // Wait before searching via API
            await Task.Delay(Constants.Jira.SearchDebounceMs, token);

            // If not cancelled, perform the API search
            if (!token.IsCancellationRequested)
            {
                await _viewModel.JiraIssuesService.SearchFromApiAsync(text);
            }
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled - new text was typed
        }
    }

    private void JiraAutocomplete_TextSubmitted(object? sender, string text)
    {
		// This is for manual text entry (typing a key directly)
		_viewModel.SelectJiraIssue(text)
			.ForgetOnFirstAwait();
    }

    private void JiraAutocomplete_IssueSelected(object? sender, IssueDetails issue)
    {
        // This is for selecting from the dropdown with full issue details
        _viewModel.SelectJiraIssue(issue)
			.ForgetOnFirstAwait();
	}

    private void JiraAutocomplete_PreviewKeyDown(object? sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            JiraAutocomplete.ClosePopup();
            _viewModel.HideAutocomplete();
            e.Handled = true;
        }
    }

    private void JiraAutocomplete_Cancelled(object? sender, EventArgs e)
    {
        JiraAutocomplete.ClosePopup();
        _viewModel.HideAutocomplete();
    }

    #endregion

    #region Settings Window

    private void ShowSettingsWindow()
    {
        // Check if settings window already exists and is visible
        try
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.Activate();
                return;
            }
        }
        catch
        {
            // Window might be in invalid state, reset reference
            _settingsWindow = null;
        }

        // Clean up old reference if window was closed
        if (_settingsWindow != null)
        {
            _settingsWindow = null;
        }

        _settingsWindow = new SettingsWindow(this)
        {
            Owner = this
        };

        _settingsWindow.Closed += (s, e) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    #endregion

    protected override void OnClosed(EventArgs e)
    {
        _settingsService.SavePosition(Left, Top);
        _topmostTimer.Stop();
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
