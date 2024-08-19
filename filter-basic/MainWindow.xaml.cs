using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using filter_basic.Dialogs;

namespace filter_basic
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // ObservableCollections for binding to ListView
        public ObservableCollection<FileItem> Files { get; set; } = new ObservableCollection<FileItem>();
        public ObservableCollection<FileItem> _filesTemporary { get; set; } = new ObservableCollection<FileItem>();
        private ObservableCollection<FileItem> _selectedFiles = new ObservableCollection<FileItem>();
        
        private string _currentSortProperty;
        private ListSortDirection _currentSortDirection;

        private string _folderPath;
        public string FolderPath
        {
            get => _folderPath;
            set
            {
                _folderPath = value;
                OnPropertyChanged(nameof(FolderPath));
            }
        }

        private string _copyToPath;
        public string CopyToPath
        {
            get => _copyToPath;
            set
            {
                _copyToPath = value;
                OnPropertyChanged(nameof(CopyToPath));
            }
        }

        private string _filenameFilter;
        public string FilenameFilter
        {
            get => _filenameFilter;
            set
            {
                _filenameFilter = value;
                OnPropertyChanged(nameof(FilenameFilter));
                FilterFiles(); // Apply filter whenever FilenameFilter changes
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;  // Bind to self
            Loaded += Window_Loaded;
        }

        private void ShowSelectedFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ShowSelectedFiles(_selectedFiles);
            dialog.ShowDialog();
        }

        private void FileItem_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var fileItem = checkBox?.DataContext as FileItem;

            if (fileItem != null && !_selectedFiles.Contains(fileItem))
            {
                _selectedFiles.Add(fileItem);
            }
        }
        private void FileItem_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var fileItem = checkBox?.DataContext as FileItem;

            if (fileItem != null && _selectedFiles.Contains(fileItem))
            {
                _selectedFiles.Remove(fileItem);
            }
        }

        private void LoadDirectory(string directoryPath, TreeViewItem parentItem)
        {
            try
            {
                var directories = Directory.GetDirectories(directoryPath).Where(dir =>
                {
                    var dirInfo = new DirectoryInfo(dir);
                    return (dirInfo.Attributes & FileAttributes.Hidden) == 0 &&
                           (dirInfo.Attributes & FileAttributes.System) == 0;
                });

                foreach (var directory in directories)
                {
                    var subItem = new TreeViewItem { Header = Path.GetFileName(directory) };
                    subItem.Tag = directory; // Store the path in the Tag property
                    subItem.Items.Add(null); // Placeholder for lazy loading
                    subItem.Expanded += TreeViewItem_Expanded; // Attach expanded event
                    parentItem.Items.Add(subItem);
                }
            }
            catch (UnauthorizedAccessException) 
            {
                // Handle or log the access exception
            }
            catch (DirectoryNotFoundException)
            {
                // Handle or log the directory not found exception
            }
        }
        
        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            var item = e.OriginalSource as TreeViewItem;

            if (item == null || item.Items.Count != 1 || item.Items[0] != null) return;

            item.Items.Clear();

            var directoryPath = item.Tag as string;

            if (directoryPath != null)
            {
                LoadDirectory(directoryPath, item);
            }
        }

        private void myTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = e.NewValue as TreeViewItem;
            var directoryPath = selectedItem?.Tag as string;

            if (!string.IsNullOrEmpty(directoryPath))
            {
                FolderPath = directoryPath;
                LoadFiles(directoryPath);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var desktopItem = new TreeViewItem { Header = "Desktop" };
            LoadDirectory(desktopDirectory, desktopItem);
            myTreeView.Items.Add(desktopItem);

            // Downloads
            string downloadsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            var downloadsItem = new TreeViewItem { Header = "Downloads" };
            LoadDirectory(downloadsDirectory, downloadsItem);
            myTreeView.Items.Add(downloadsItem);

            // This PC (Loading drives)
            var thisPcItem = new TreeViewItem { Header = "This PC" };
            foreach (var drive in DriveInfo.GetDrives())
            {
                var driveItem = new TreeViewItem { Header = drive.Name };
                LoadDirectory(drive.Name, driveItem);
                thisPcItem.Items.Add(driveItem);
            }
            myTreeView.Items.Add(thisPcItem);
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog()
            {
                Multiselect = false,
            };

            if (dialog.ShowDialog() == true)
            {
                FolderPath = dialog.FolderName;  // Bind to FolderPath
                LoadFiles(FolderPath);
            }
        }
        
        private void BrowseTo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog()
            {
                Multiselect = false,
            };

            if (dialog.ShowDialog() == true)
            {
                CopyToPath = dialog.FolderName;
            }
        }

        private void LoadFiles(string folderPath)
        {
            Files.Clear();
            _filesTemporary.Clear(); // Clear temporary files as well

            var directoryInfo = new DirectoryInfo(folderPath);
            var fileInfos = directoryInfo.GetFiles();

            foreach (var file in fileInfos)
            {
                var fileItem = new FileItem
                {
                    FileName = file.Name,
                    NewFileName = file.Name,
                    Size = (file.Length / 1024).ToString(),
                    Extension = file.Extension,
                    DateModified = file.LastWriteTime.ToString(),
                    Directory = file.DirectoryName
                };

                Files.Add(fileItem);
                _filesTemporary.Add(fileItem);
            }
        }

        private void FilenameFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Apply the filter as soon as the text changes
            FilterFiles();
        }

        private void FilterFiles()
        {
            if (string.IsNullOrEmpty(FilenameFilter))
            {
                // Clear filter, show all files
                _filesTemporary.Clear();
                foreach (var file in Files)
                {
                    _filesTemporary.Add(file);
                }
            }
            else
            {
                var filteredFiles = Files.Where(f => f.FileName.Contains(FilenameFilter)).ToList();
                _filesTemporary.Clear();
                foreach (var file in filteredFiles)
                {
                    _filesTemporary.Add(file);
                }
            }
        }

        // Rename Files
        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in Files.Where(f => f.IsChecked))
            {
                var oldPath = Path.Combine(file.Directory, file.FileName);
                var newPath = Path.Combine(file.Directory, file.NewFileName);
                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath);
                }
            }
            MessageBox.Show("Files renamed successfully!");
            LoadFiles(FolderPath);
        }

        // Copy Files
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CopyToPath) || !Directory.Exists(CopyToPath))
            {
                MessageBox.Show("Invalid destination folder.");
                return;
            }

            foreach (var file in Files.Where(f => f.IsChecked))
            {
                var sourcePath = Path.Combine(file.Directory, file.FileName);
                var destinationPath = Path.Combine(CopyToPath, file.FileName);
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destinationPath);
                }
            }

            _selectedFiles = [];
            MessageBox.Show("Files copied successfully!");
            
        }

        // Move Files
        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CopyToPath) || !Directory.Exists(CopyToPath))
            {
                MessageBox.Show("Invalid destination folder.");
                return;
            }

            foreach (var file in Files.Where(f => f.IsChecked))
            {
                var sourcePath = Path.Combine(file.Directory, file.FileName);
                var destinationPath = Path.Combine(CopyToPath, file.FileName);
                if (File.Exists(sourcePath))
                {
                    File.Move(sourcePath, destinationPath);
                }
            }
            _selectedFiles = [];
            MessageBox.Show("Files moved successfully!");
            LoadFiles(FolderPath);
        }

        // Notify Property Changed implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var header = sender as GridViewColumnHeader;
            if (header != null)
            {
                var column = header.Column;
                if (column != null)
                {
                    // Retrieve the data to be sorted
                    var propertyName = (column.DisplayMemberBinding as Binding)?.Path.Path;

                    // Perform sorting
                    if (propertyName != null)
                        Sort(propertyName);
                    else
                        Sort("IsChecked");
                }
            }
        }

        
        private void Sort(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            // Check if the list is currently sorted in ascending or descending order
            var direction = ListSortDirection.Ascending;
            if (_currentSortProperty == propertyName && _currentSortDirection == ListSortDirection.Ascending)
            {
                direction = ListSortDirection.Descending;
            }

            _currentSortProperty = propertyName;
            _currentSortDirection = direction;

            var sortedList = direction == ListSortDirection.Ascending
                ? _filesTemporary.OrderBy(f => GetPropertyValue(f, propertyName)).ToList()
                : _filesTemporary.OrderByDescending(f => GetPropertyValue(f, propertyName)).ToList();

            _filesTemporary.Clear();
            foreach (var file in sortedList)
            {
                _filesTemporary.Add(file);
            }
        }

        private object GetPropertyValue(FileItem item, string propertyName)
        {
            var propertyInfo = item.GetType().GetProperty(propertyName);
            return propertyName == "IsChecked" ? (object)(item.IsChecked ? 1 : 0) : propertyInfo?.GetValue(item, null);
        }
    }

    // FileItem class for binding data to ListView
    public class FileItem
    {
        public bool IsChecked { get; set; }
        public string FileName { get; set; }
        public string NewFileName { get; set; }
        public string Size { get; set; }
        public string Extension { get; set; }
        public string DateModified { get; set; }
        public string Directory { get; set; }
    }
}
