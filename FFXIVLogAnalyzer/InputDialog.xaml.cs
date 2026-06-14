using System.Windows;

namespace FFXIVLogAnalyzer;

public partial class InputDialog : Window
{
    public string InputText => InputBox.Text;

    public InputDialog(string prompt, string title = "输入")
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
