using System.ComponentModel;

namespace sizoscope.ViewModels;

public sealed class SearchResultItem : INotifyPropertyChanged
{
    private string? name;
    private int exclusiveSize;
    private int inclusiveSize;

    public SearchResultItem(string? name, int exclusiveSize, int inclusiveSize)
    {
        Name = name;
        ExclusiveSize = exclusiveSize;
        InclusiveSize = inclusiveSize;
    }

    public string? Name
    {
        get => name;
        set
        {
            name = value;
            PropertyChanged?.Invoke(this, new(nameof(Name)));
        }
    }

    public int ExclusiveSize
    {
        get => exclusiveSize;
        set
        {
            exclusiveSize = value;
            PropertyChanged?.Invoke(value, new(nameof(ExclusiveSize)));
            PropertyChanged?.Invoke(this, new(nameof(ExclusiveFileSize)));
        }
    }

    public int InclusiveSize
    {
        get => inclusiveSize;
        set
        {
            inclusiveSize = value;
            PropertyChanged?.Invoke(this, new(nameof(InclusiveSize)));
            PropertyChanged?.Invoke(this, new(nameof(InclusiveFileSize)));
        }
    }

    public object? Tag { get; set; }

    public string ExclusiveFileSize => TreeLogic.AsFileSize(ExclusiveSize);
    public string InclusiveFileSize => TreeLogic.AsFileSize(InclusiveSize);

    public event PropertyChangedEventHandler? PropertyChanged;
}
