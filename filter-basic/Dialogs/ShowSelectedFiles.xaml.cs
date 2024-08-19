﻿using System.Collections.ObjectModel;
using System.Windows;

namespace filter_basic.Dialogs;

public partial class ShowSelectedFiles : Window
{
    public ObservableCollection<FileItem> SelectedFiles { get; }

    public ShowSelectedFiles(ObservableCollection<FileItem> selectedFiles)
    {
        InitializeComponent();
        SelectedFiles = selectedFiles;
        DataContext = this;
    }
}