using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using filter_basic.Dialogs;
using filter_basic.Models;

namespace filter_basic
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // ObservableCollections for binding to ListView
        public ObservableCollection<FileItem> Files { get; set; } = new ObservableCollection<FileItem>();
        public ObservableCollection<FileItem> _filesTemporary { get; set; } = new ObservableCollection<FileItem>();
        public ObservableCollection<FileItem> _selectedFiles = new ObservableCollection<FileItem>();

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
            DataContext = this; // Bind to self
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
            string downloadsDirectory =
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
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
                FolderPath = dialog.FolderName; // Bind to FolderPath
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
                    FileName = Path.GetFileNameWithoutExtension(file.Name), // Lấy phần tên mà không có phần mở rộng
                    NewFileName = "",
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
            if (!CheckSelectedFiles()) return;

            var dialog = new RenameDialog();
            if (dialog.ShowDialog() == true)
            {
                string baseName = dialog.BaseName;
                string code = dialog.Code;
                int counter = 1;

                foreach (var file in _selectedFiles)
                {
                    string newFileName = $"{baseName}{code}{counter++}";
                    var fileItem = _filesTemporary.SingleOrDefault(m => m.FileName == file.FileName);
                    if (fileItem != null)
                    {
                        fileItem.NewFileName = newFileName;
                    }
                }
            }
        }


        private void RenameFilesInFolderPath()
        {
            foreach (var file in _selectedFiles)
            {
                // Lấy phần mở rộng của file từ FileName
                string extension = Path.GetExtension(file.Extension);

                // Ghép lại phần mở rộng vào NewFileName trước khi thực hiện rename
                var oldPath = Path.Combine(file.Directory, $"{file.FileName}{extension}");
                var newPath = Path.Combine(file.Directory, $"{file.NewFileName}{extension}");

                if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath);
                    file.FileName = file.NewFileName;
                }
            }

            MessageBox.Show("Files renamed successfully!");
            // Lưu lại các file đã được chọn trước khi LoadFiles


            // Load lại Files
            LoadFiles(FolderPath);

            // Cập nhật lại _selectedFiles dựa trên danh sách mới
            UpdateSelectedFiles();
        }

        private void UpdateSelectedFiles()
        {
            var selectedFiles = _selectedFiles.ToList();
            _selectedFiles.Clear();
            foreach (var file in selectedFiles)
            {
                var updatedFile = _filesTemporary.SingleOrDefault(f => f.FileName == file.FileName);
                if (updatedFile != null)
                {
                    updatedFile.IsChecked = true; // Đảm bảo giữ lại trạng thái tích chọn
                    _selectedFiles.Add(updatedFile); // Thêm lại vào _selectedFiles
                }
            }
        }

        // Copy Files
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckSelectedFiles()) return;

            if (string.IsNullOrEmpty(CopyToPath) || !Directory.Exists(CopyToPath))
            {
                MessageBox.Show("Invalid destination folder.");
                return;
            }

            // Kiểm tra xung đột tên file
            var conflicts = new List<FileConflict>();
            foreach (var file in _selectedFiles)
            {
                var destinationPath = Path.Combine(CopyToPath,
                    file.NewFileName != "" ? (file.NewFileName + file.Extension) : (file.FileName + file.Extension));
                if (File.Exists(destinationPath))
                {
                    conflicts.Add(new FileConflict
                    {
                        FileName = file.NewFileName != ""
                            ? file.NewFileName + file.Extension
                            : file.FileName + file.Extension,
                        Path = destinationPath
                    });
                }
            }

            if (conflicts.Any())
            {
                var dialog = new ConflictResolutionDialog(conflicts);
                if (dialog.ShowDialog() == true)
                {
                    switch (dialog.UserChoice)
                    {
                        case ConflictResolutionDialog.ConflictResolution.Overwrite:
                            HandleCopyOverwrite(conflicts);
                            break;
                        case ConflictResolutionDialog.ConflictResolution.CreateCopy:
                            HandleCopyCreateCopy(conflicts);
                            break;
                        case ConflictResolutionDialog.ConflictResolution.Skip:
                            HandleCopySkip(conflicts);
                            break;
                    }
                }
            }
            else
            {
                // Nếu không có xung đột, thực hiện sao chép ngay
                CopyFiles();
            }
        }

        private void HandleCopyOverwrite(IEnumerable<FileConflict> conflicts)
        {
            foreach (var conflict in conflicts)
            {
                // Xóa file cũ trước khi sao chép
                File.Delete(conflict.Path);
            }

            CopyFiles();
        }

        private void HandleCopyCreateCopy(IEnumerable<FileConflict> conflicts)
        {
            foreach (var file in _selectedFiles)
            {
                var destinationPath = Path.Combine(CopyToPath,
                    file.NewFileName != "" ? (file.NewFileName + file.Extension) : (file.FileName + file.Extension));
                if (File.Exists(destinationPath))
                {
                    // Tạo bản sao với thêm hậu tố "(Copy)"
                    var copyPath = Path.Combine(CopyToPath,
                        file.NewFileName != ""
                            ? file.NewFileName + " (Copy)" + file.Extension
                            : file.FileName + " (Copy)" + file.Extension);
                    File.Copy(Path.Combine(file.Directory, file.FileName + file.Extension), copyPath);
                }
                else
                {
                    File.Copy(Path.Combine(file.Directory, file.FileName + file.Extension), destinationPath);
                }
            }

            _selectedFiles.Clear();
            MessageBox.Show("Files copied successfully!");
            LoadFiles(FolderPath);
        }

        private void HandleCopySkip(IEnumerable<FileConflict> conflicts)
        {
            foreach (var file in _selectedFiles)
            {
                var destinationPath = Path.Combine(CopyToPath,
                    file.NewFileName != "" ? (file.NewFileName + file.Extension) : (file.FileName + file.Extension));
                if (!File.Exists(destinationPath))
                {
                    File.Copy(Path.Combine(file.Directory, file.FileName + file.Extension), destinationPath);
                }
            }

            _selectedFiles.Clear();
            MessageBox.Show("Files copied successfully!");
            LoadFiles(FolderPath);
        }

        private void CopyFiles()
        {
            foreach (var file in _selectedFiles)
            {
                var sourcePath = Path.Combine(file.Directory, file.FileName + file.Extension);
                var destinationPath = Path.Combine(CopyToPath,
                    file.NewFileName != "" ? (file.NewFileName + file.Extension) : (file.FileName + file.Extension));
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destinationPath);
                }
            }

            _selectedFiles.Clear();
            MessageBox.Show("Files copied successfully!");
            LoadFiles(FolderPath);
        }

        // Move Files
        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckSelectedFiles()) return;

            if (string.IsNullOrEmpty(CopyToPath) || !Directory.Exists(CopyToPath))
            {
                MessageBox.Show("Invalid destination folder.");
                return;
            }

            // Kiểm tra xung đột tên file
            var conflicts = new List<FileConflict>();
            foreach (var file in _selectedFiles)
            {
                var destinationPath =
                    Path.Combine(CopyToPath, file.NewFileName != "" ? file.NewFileName : file.FileName) +
                    file.Extension;
                if (File.Exists(destinationPath))
                {
                    conflicts.Add(new FileConflict
                    {
                        FileName = file.NewFileName != ""
                            ? file.NewFileName + file.Extension
                            : file.FileName + file.Extension,
                        Path = destinationPath
                    });
                }
            }

            if (conflicts.Any())
            {
                var dialog = new ConflictResolutionDialog(conflicts);
                if (dialog.ShowDialog() == true)
                {
                    switch (dialog.UserChoice)
                    {
                        case ConflictResolutionDialog.ConflictResolution.Overwrite:
                            HandleOverwrite(conflicts);
                            break;
                        case ConflictResolutionDialog.ConflictResolution.CreateCopy:
                            HandleCreateCopy(conflicts);
                            break;
                        case ConflictResolutionDialog.ConflictResolution.Skip:
                            HandleSkip(conflicts);
                            break;
                    }
                }
            }
            else
            {
                // Nếu không có xung đột, thực hiện di chuyển ngay
                MoveFiles();
            }
        }

        private void HandleOverwrite(IEnumerable<FileConflict> conflicts)
        {
            foreach (var conflict in conflicts)
            {
                // Xóa file cũ trước khi di chuyển
                File.Delete(conflict.Path);
            }

            MoveFiles();
        }

        private void HandleCreateCopy(IEnumerable<FileConflict> conflicts)
        {
            foreach (var file in _selectedFiles)
            {
                var destinationPath =
                    Path.Combine(CopyToPath, file.NewFileName != "" ? file.NewFileName : file.FileName) +
                    file.Extension;
                if (File.Exists(destinationPath))
                {
                    // Tạo bản sao với thêm hậu tố "(Copy)"
                    var copyPath = Path.Combine(CopyToPath,
                        file.NewFileName != ""
                            ? file.NewFileName + " (Copy)" + file.Extension
                            : file.FileName + " (Copy)" + file.Extension);
                    File.Copy(Path.Combine(file.Directory, file.FileName + file.Extension), copyPath);
                }
                else
                {
                    File.Copy(Path.Combine(file.Directory, file.FileName + file.Extension), destinationPath);
                }
            }

            _selectedFiles.Clear();
            MessageBox.Show("Files copied successfully!");
            LoadFiles(FolderPath);
        }

        private void HandleSkip(IEnumerable<FileConflict> conflicts)
        {
            foreach (var file in _selectedFiles)
            {
                var destinationPath =
                    Path.Combine(CopyToPath, file.NewFileName != "" ? file.NewFileName : file.FileName) +
                    file.Extension;
                if (!File.Exists(destinationPath))
                {
                    File.Move(Path.Combine(file.Directory, file.FileName + file.Extension), destinationPath);
                }
            }

            _selectedFiles.Clear();
            MessageBox.Show("Files moved successfully!");
            LoadFiles(FolderPath);
        }

        private void MoveFiles()
        {
            foreach (var file in _selectedFiles)
            {
                var sourcePath = Path.Combine(file.Directory, file.FileName + file.Extension);
                var destinationPath =
                    Path.Combine(CopyToPath, file.NewFileName != "" ? file.NewFileName : file.FileName) +
                    file.Extension;

                if (File.Exists(sourcePath))
                {
                    File.Move(sourcePath, destinationPath);
                }
            }

            _selectedFiles.Clear();
            MessageBox.Show("Files moved successfully!");
            LoadFiles(FolderPath);
        }


        // Notify Property Changed implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
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

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckSelectedFiles()) return;
            if (!CheckAllFilesHaveNewNames()) return;
            RenameFilesInFolderPath();
        }

        private bool CheckAllFilesHaveNewNames()
        {
            if (_selectedFiles.Any(file => string.IsNullOrEmpty(file.NewFileName)))
            {
                MessageBox.Show("All selected files must have a new name assigned.");
                return false;
            }

            return true;
        }

        private bool CheckSelectedFiles()
        {
            if (_selectedFiles == null || !_selectedFiles.Any())
            {
                MessageBox.Show("Please select at least one file.");
                return false;
            }

            return true;
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Kiểm tra xem người dùng có nhấp đúp vào một item không
            if (sender is ListView listView && listView.SelectedItem is FileItem selectedFile)
            {
                // Lấy đường dẫn đầy đủ của tệp tin (bao gồm tên tệp và phần mở rộng)
                string filePath = Path.Combine(selectedFile.Directory, selectedFile.FileName + selectedFile.Extension);

                // Mở tệp tin hình ảnh
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", filePath);
                }
                else
                {
                    MessageBox.Show("File does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void HeaderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Lặp qua tất cả các mục trong ListView và đánh dấu chúng là checked
            foreach (var item in FilesListView.Items)
            {
                if (item is FileItem fileItem)
                {
                    fileItem.IsChecked = true;
                }
            }
        }

        private void HeaderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Lặp qua tất cả các mục trong ListView và bỏ chọn chúng
            foreach (var item in FilesListView.Items)
            {
                if (item is FileItem fileItem)
                {
                    fileItem.IsChecked = false;
                }
            }
        }
    }
}