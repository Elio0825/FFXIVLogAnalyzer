using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FFXIVLogAnalyzer.ViewModels;

namespace FFXIVLogAnalyzer;

public partial class RawLogWindow : Window
{
    private readonly ObservableCollection<RawLogLine> _allLines;
    private readonly ObservableCollection<RawLogLine> _filteredLines = new();

    public RawLogWindow(ObservableCollection<RawLogLine> lines)
    {
        InitializeComponent();
        _allLines = lines;
        LogListBox.ItemsSource = _allLines;
    }

    public void JumpToLine(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > _allLines.Count) return;

        // Reset filter to show all lines
        SearchBox.Text = string.Empty;
        LogListBox.ItemsSource = _allLines;

        var target = _allLines[lineNumber - 1];
        LogListBox.ScrollIntoView(target);
        LogListBox.SelectedItem = target;
        LineInfoText.Text = $"第 {lineNumber} 行";

        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(keyword))
        {
            LogListBox.ItemsSource = _allLines;
            LineInfoText.Text = "";
            return;
        }

        _filteredLines.Clear();
        foreach (var line in _allLines)
        {
            if (line.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                _filteredLines.Add(line);
        }
        LogListBox.ItemsSource = _filteredLines;
        LineInfoText.Text = $"找到 {_filteredLines.Count} 条匹配";
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
