using System.ComponentModel;

namespace sizoscope.ViewModels;

public sealed class SearchResultItem : INotifyPropertyChanged
{
    private string? _name;
    private int _exclusiveSize;
    private int _inclusiveSize;

    public SearchResultItem(string? name, int exclusiveSize, int inclusiveSize)
    {
        Name = name;
        ExclusiveSize = exclusiveSize;
        InclusiveSize = inclusiveSize;
    }

    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            PropertyChanged?.Invoke(this, new(nameof(Name)));
        }
    }

    public int ExclusiveSize
    {
        get => _exclusiveSize;
        set
        {
            _exclusiveSize = value;
            PropertyChanged?.Invoke(value, new(nameof(ExclusiveSize)));
            PropertyChanged?.Invoke(this, new(nameof(ExclusiveFileSize)));
        }
    }

    public int InclusiveSize
    {
        get => _inclusiveSize;
        set
        {
            _inclusiveSize = value;
            PropertyChanged?.Invoke(this, new(nameof(InclusiveSize)));
            PropertyChanged?.Invoke(this, new(nameof(InclusiveFileSize)));
        }
    }

    public object? Tag { get; set; }

    public string ExclusiveFileSize => TreeLogic.AsFileSize(ExclusiveSize);
    public string InclusiveFileSize => TreeLogic.AsFileSize(InclusiveSize);

    public event PropertyChangedEventHandler? PropertyChanged;
}
