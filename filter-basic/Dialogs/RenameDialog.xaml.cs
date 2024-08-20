using System.Windows;

namespace filter_basic.Dialogs;

public partial class RenameDialog : Window
{
    public string BaseName { get; private set; }
    public string Code { get; private set; }
    public RenameDialog()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        BaseName = BaseNameTextBox.Text;
        Code = CodeTextBox.Text;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}