namespace filter_basic.Models;
using System.ComponentModel;

public class FileItem : INotifyPropertyChanged
{
    private bool _isChecked;
    private string _fileName;
    private string _newFileName;
    private string _size;
    private string _extension;
    private string _dateModified;
    private string _directory;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            _isChecked = value;
            OnPropertyChanged(nameof(IsChecked));
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged(nameof(FileName));
        }
    }

    public string NewFileName
    {
        get => _newFileName;
        set
        {
            _newFileName = value;
            OnPropertyChanged(nameof(NewFileName));
        }
    }

    public string Size
    {
        get => _size;
        set
        {
            _size = value;
            OnPropertyChanged(nameof(Size));
        }
    }

    public string Extension
    {
        get => _extension;
        set
        {
            _extension = value;
            OnPropertyChanged(nameof(Extension));
        }
    }

    public string DateModified
    {
        get => _dateModified;
        set
        {
            _dateModified = value;
            OnPropertyChanged(nameof(DateModified));
        }
    }

    public string Directory
    {
        get => _directory;
        set
        {
            _directory = value;
            OnPropertyChanged(nameof(Directory));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}