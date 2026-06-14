using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using FFXIVLogAnalyzer.Models;
using FFXIVLogAnalyzer.ViewModels;

namespace FFXIVLogAnalyzer;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private RawLogWindow? _rawLogWindow;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
    }

    private RawLogWindow GetRawLogWindow()
    {
        if (_rawLogWindow == null || !_rawLogWindow.IsLoaded)
        {
            _rawLogWindow = new RawLogWindow(_vm.RawLogLines) { Owner = this };
        }
        return _rawLogWindow;
    }

    private void OpenRawLogWindow_Click(object sender, RoutedEventArgs e)
    {
        var win = GetRawLogWindow();
        if (win.IsVisible) win.Activate();
        else win.Show();
    }

    private void BossGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (BossGrid.SelectedItem is BossSkillEntry entry)
        {
            var win = GetRawLogWindow();
            win.JumpToLine(entry.LineNumber);
        }
    }

    private void PlayerSkillGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PlayerSkillGrid.SelectedItem is PlayerSkillEntry entry)
            _vm.ToggleHighlightCommand.Execute(entry);
    }

    private void JumpBossToLog_Click(object sender, RoutedEventArgs e)
    {
        if (BossGrid.SelectedItem is BossSkillEntry entry)
            GetRawLogWindow().JumpToLine(entry.LineNumber);
    }

    private void JumpPlayerToLog_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerSkillGrid.SelectedItem is PlayerSkillEntry entry)
            GetRawLogWindow().JumpToLine(entry.LineNumber);
    }

    private void CopyBossSkillId_Click(object sender, RoutedEventArgs e)
    {
        if (BossGrid.SelectedItem is BossSkillEntry entry)
        {
            CopyToClipboard(entry.SkillId.ToString(), $"已复制Boss技能ID: {entry.SkillId} ({entry.SkillName})");
        }
    }

    private void CopyBossSkillName_Click(object sender, RoutedEventArgs e)
    {
        if (BossGrid.SelectedItem is BossSkillEntry entry)
        {
            CopyToClipboard(entry.SkillName, $"已复制Boss技能名: {entry.SkillName}");
        }
    }

    private void CopyPlayerSkillId_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerSkillGrid.SelectedItem is PlayerSkillEntry entry)
        {
            CopyToClipboard(entry.SkillId.ToString(), $"已复制玩家技能ID: {entry.SkillId} ({entry.DisplayName})");
        }
    }

    private void CopyPlayerSkillName_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerSkillGrid.SelectedItem is PlayerSkillEntry entry)
        {
            CopyToClipboard(entry.DisplayName, $"已复制玩家技能名: {entry.DisplayName}");
        }
    }

    private void CopyDelta_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerSkillGrid.SelectedItem is PlayerSkillEntry entry)
        {
            CopyToClipboard(entry.DeltaSeconds.ToString("F1"), $"已复制Delta: {entry.DeltaDisplay} ({entry.DisplayName})");
        }
    }

    private void EffectGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EffectGrid.SelectedItem is BossEffectEntry entry)
            GetRawLogWindow().JumpToLine(entry.LineNumber);
    }

    private void JumpEffectToLog_Click(object sender, RoutedEventArgs e)
    {
        if (EffectGrid.SelectedItem is BossEffectEntry entry)
            GetRawLogWindow().JumpToLine(entry.LineNumber);
    }

    private void CopyEffectId_Click(object sender, RoutedEventArgs e)
    {
        if (EffectGrid.SelectedItem is BossEffectEntry entry)
        {
            CopyToClipboard(entry.ActionId.ToString(), $"已复制效果ID: {entry.ActionId} ({entry.ActionName})");
        }
    }

    private void CopyEffectName_Click(object sender, RoutedEventArgs e)
    {
        if (EffectGrid.SelectedItem is BossEffectEntry entry)
        {
            CopyToClipboard(entry.ActionName, $"已复制效果技能名: {entry.ActionName}");
        }
    }

    private void QueryDistanceToBoss_Click(object sender, RoutedEventArgs e)
    {
        if (PlayerSkillGrid.SelectedItem is not PlayerSkillEntry playerEntry) return;

        var dlg = new InputDialog("输入Boss技能ID或效果命中ID:", "查询距离Boss机制时间") { Owner = this };
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.InputText))
        {
            var result = _vm.QueryDistanceToBossEvent(playerEntry, dlg.InputText);
            _vm.StatusText = result;
            MessageBox.Show(result, "查询结果", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid) return;

        var row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row == null) return;

        grid.SelectedItem = row.Item;
        row.Focus();
    }

    private void CopyToClipboard(string text, string successStatus)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (TrySetClipboardText(text))
            {
                _vm.StatusText = successStatus;
                return;
            }

            if (attempt < maxAttempts)
                Thread.Sleep(30);
        }

        _vm.StatusText = "剪贴板正被其他程序占用，复制失败，请稍后重试";
    }

    private bool TrySetClipboardText(string text)
    {
        var owner = new WindowInteropHelper(this).Handle;
        if (!OpenClipboard(owner))
            return false;

        var handle = IntPtr.Zero;
        try
        {
            if (!EmptyClipboard())
                return false;

            var bytes = Encoding.Unicode.GetBytes(text + "\0");
            handle = GlobalAlloc(GmemMoveable, (UIntPtr)bytes.Length);
            if (handle == IntPtr.Zero)
                return false;

            var target = GlobalLock(handle);
            if (target == IntPtr.Zero)
                return false;

            Marshal.Copy(bytes, 0, target, bytes.Length);
            GlobalUnlock(handle);

            if (SetClipboardData(CfUnicodeText, handle) == IntPtr.Zero)
                return false;

            handle = IntPtr.Zero;
            return true;
        }
        finally
        {
            if (handle != IntPtr.Zero)
                GlobalFree(handle);
            CloseClipboard();
        }
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T target) return target;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
