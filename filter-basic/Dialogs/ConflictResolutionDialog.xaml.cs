using System.Windows;
using filter_basic.Models;

namespace filter_basic.Dialogs;

public partial class ConflictResolutionDialog : Window
{
    public ConflictResolutionDialog(IEnumerable<FileConflict> conflicts)
    {
        InitializeComponent();
        FilesListView.ItemsSource = conflicts;
    }

    public enum ConflictResolution
    {
        Overwrite,
        CreateCopy,
        Skip
    }

    public ConflictResolution UserChoice { get; private set; }
    private void OverwriteButton_Click(object sender, RoutedEventArgs e)
    {
        UserChoice = ConflictResolution.Overwrite;
        this.DialogResult = true;
        Close();
    }

    private void CreateCopyButton_Click(object sender, RoutedEventArgs e)
    {
        UserChoice = ConflictResolution.CreateCopy;
        this.DialogResult = true;
        Close();
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        UserChoice = ConflictResolution.Skip;
        this.DialogResult = true;
        Close();
    }
}