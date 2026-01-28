using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using UpworkJiraTracker.Model;

using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace UpworkJiraTracker.XAML;

public partial class AutocompleteTextBox : WpfUserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(AutocompleteTextBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public event EventHandler<string>? TextSubmitted;
    public event EventHandler<string>? TextChanged;
    public event EventHandler<IssueDetails>? IssueSelected;
    public event EventHandler? Cancelled;

    public ObservableCollection<JiraIssue> Suggestions { get; } = new();

    private int _selectedIndex = -1;
    private bool _isSelectingSuggestion = false;

    public AutocompleteTextBox()
    {
        InitializeComponent();
        SuggestionsListBox.ItemsSource = Suggestions;
    }

    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Text = InputTextBox.Text;
        TextChanged?.Invoke(this, InputTextBox.Text);

        // Don't auto-close popup on empty text - let UpdateSuggestions control it
        // This allows default suggestions (Recent, My Issues) to show when text is empty
        if (!string.IsNullOrWhiteSpace(InputTextBox.Text) && Suggestions.Count > 0)
        {
            SuggestionsPopup.IsOpen = true;
            _selectedIndex = -1;
            SuggestionsListBox.SelectedIndex = -1;
        }
    }

    private void InputTextBox_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (SuggestionsPopup.IsOpen)
        {
            switch (e.Key)
            {
                case Key.Down:
                    MoveSelection(1);
                    e.Handled = true;
                    break;

                case Key.Up:
                    MoveSelection(-1);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    if (_selectedIndex >= 0 && _selectedIndex < Suggestions.Count)
                    {
                        var selected = Suggestions[_selectedIndex];
                        if (!selected.IsSectionHeader)
                        {
                            SelectSuggestion(selected);
                            SuggestionsPopup.IsOpen = false;
                        }
                    }
                    else
                    {
                        TextSubmitted?.Invoke(this, InputTextBox.Text);
                        SuggestionsPopup.IsOpen = false;
                    }
                    e.Handled = true;
                    break;

                case Key.Escape:
                    SuggestionsPopup.IsOpen = false;
                    e.Handled = true;
                    break;
            }
        }
        else if (e.Key == Key.Enter)
        {
            TextSubmitted?.Invoke(this, InputTextBox.Text);
            e.Handled = true;
        }
    }

    private void MoveSelection(int direction)
    {
        var newIndex = _selectedIndex + direction;

        // Skip section headers
        while (newIndex >= 0 && newIndex < Suggestions.Count && Suggestions[newIndex].IsSectionHeader)
        {
            newIndex += direction;
        }

        if (newIndex >= 0 && newIndex < Suggestions.Count)
        {
            _selectedIndex = newIndex;
            SuggestionsListBox.SelectedIndex = _selectedIndex;
            SuggestionsListBox.ScrollIntoView(SuggestionsListBox.SelectedItem);
        }
    }

    private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // Delay closing to allow click on suggestion
        Dispatcher.BeginInvoke(new Action(() =>
        {
            // Skip if we're in the process of selecting a suggestion
            if (_isSelectingSuggestion)
            {
                _isSelectingSuggestion = false;
                return;
            }

            SuggestionsPopup.IsOpen = false;
            Cancelled?.Invoke(this, EventArgs.Empty);
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void SuggestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SuggestionsListBox.SelectedItem is JiraIssue issue)
        {
            // Ignore section headers
            if (issue.IsSectionHeader)
            {
                SuggestionsListBox.SelectedIndex = -1;
                return;
            }

            _selectedIndex = SuggestionsListBox.SelectedIndex;
            _isSelectingSuggestion = true;
            SelectSuggestion(issue);
            SuggestionsPopup.IsOpen = false;
        }
    }

    private void SelectSuggestion(JiraIssue issue)
    {
        InputTextBox.Text = issue.Key;
        Text = issue.Key;
        // Only fire IssueSelected - TextSubmitted is for manual text entry only
        IssueSelected?.Invoke(this, new IssueDetails(issue));
    }

    public void UpdateSuggestions(List<JiraIssue> newSuggestions)
    {
        Suggestions.Clear();
        foreach (var suggestion in newSuggestions)
        {
            Suggestions.Add(suggestion);
        }

        HideLoading();
        SuggestionsPopup.IsOpen = newSuggestions.Count > 0;
        _selectedIndex = -1;
        SuggestionsListBox.SelectedIndex = -1;
    }

    public void ShowLoading()
    {
        LoadingBorder.Visibility = Visibility.Visible;
        SuggestionsListBox.Visibility = Visibility.Collapsed;
        SuggestionsPopup.IsOpen = true;
    }

    public void HideLoading()
    {
        LoadingBorder.Visibility = Visibility.Collapsed;
        SuggestionsListBox.Visibility = Visibility.Visible;
    }

    public void SetPopupPlacement(System.Windows.Controls.Primitives.PlacementMode placement)
    {
        SuggestionsPopup.Placement = placement;
    }

    public new void Focus()
    {
        InputTextBox.Focus();
    }

    public void ClosePopup()
    {
        SuggestionsPopup.IsOpen = false;
    }

    public bool IsPopupOpen => SuggestionsPopup.IsOpen;

    public bool IsClickInsidePopup(DependencyObject clickedElement)
    {
        if (!SuggestionsPopup.IsOpen || SuggestionsPopup.Child == null)
            return false;

        // Walk up the visual tree from clicked element to see if it's within popup content
        var element = clickedElement;
        while (element != null)
        {
            if (element == SuggestionsPopup.Child)
                return true;
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return false;
    }
}
