using Avalonia.Threading;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection.Metadata;
using static MstatData;
using static sizoscope.TreeLogic;

namespace sizoscope.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public MainWindowViewModel()
    {
        _searchDebouncer = new(TimeSpan.FromMilliseconds(500), DispatcherPriority.Normal, ExecuteSearch);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private MstatData? _data;
    private string? _fileName;
    private int _sortMode;
    private int _searchMode;
    private string? _searchPattern;
    private readonly DispatcherTimer _searchDebouncer;

    public ObservableCollection<TreeNode> Items { get; } = new();
    public ObservableCollection<SearchResultItem> SearchResult { get; } = new();
    public Sorter Sorter => SortMode is 0 ? Sorter.BySize() : Sorter.ByName();

    public void Refresh()
    {
        if (_data is not null)
        {
            RefreshTree(Items, _data, Sorter);
            RefreshSearch();
        }
    }

    public MstatData? CurrentData => _data;

    public string? FileName
    {
        get => _fileName;
        set
        {
            if (value != _fileName)
            {
                if (value is null || value == FileName) return;

                _fileName = value;
                PropertyChanged?.Invoke(this, new(nameof(FileName)));

                _data?.Dispose();
                _data = Read(value);
                RefreshTree(Items, _data, Sorter);
                RefreshSearch();
            }
        }
    }

    public int SortMode
    {
        get => _sortMode;
        set
        {
            if (value != _sortMode)
            {
                _sortMode = value;
                PropertyChanged?.Invoke(this, new(nameof(SortMode)));
                PropertyChanged?.Invoke(this, new(nameof(Sorter)));
                if (_data is not null)
                {
                    RefreshTree(Items, _data, Sorter);
                }
            }
        }
    }

    public int SearchMode
    {
        get => _searchMode;
        set
        {
            if (value != _searchMode)
            {
                _searchMode = value;
                PropertyChanged?.Invoke(this, new(nameof(SearchMode)));
                RefreshSearch();
            }
        }
    }

    public string? SearchPattern
    {
        get => _searchPattern;
        set
        {
            if (value != _searchPattern)
            {
                _searchPattern = value;
                PropertyChanged?.Invoke(this, new(nameof(SearchPattern)));
                _searchDebouncer.Stop();
                _searchDebouncer.Start();
            }
        }
    }

    private void ExecuteSearch(object? sender, EventArgs args)
    {
        _searchDebouncer.Stop();
        RefreshSearch();
    }

    private void RefreshSearch()
    {
        if (_searchPattern is null || _data is null)
        {
            return;
        }

        SearchResult.Clear();

        if (_searchPattern.Length > 0)
        {
            foreach (var asm in _data.GetScopes())
            {
                if (asm.Name == "System.Private.CompilerGenerated")
                    continue;

                AddTypes(asm.GetTypes());
            }
        }

        void AddTypes(Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> types)
        {
            foreach (var t in types)
            {
                if (t.Name.Contains(_searchPattern) || t.Namespace.Contains(_searchPattern))
                {
                    var newItem = new SearchResultItem(t.ToString(), t.Size, t.AggregateSize);

                    newItem.Tag = t;

                    SearchResult.Add(newItem);
                }

                AddTypes(t.GetNestedTypes());

                if (_searchMode is 0 or 2)
                    AddMembers(t.GetMembers());
            }
        }

        void AddMembers(Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> members)
        {
            foreach (var m in members)
            {
                if (m.Name.Contains(_searchPattern))
                {
                    var newItem = new SearchResultItem(m.ToString(), m.Size, m.AggregateSize);

                    newItem.Tag = m;

                    SearchResult.Add(newItem);
                }
            }
        }
    }

    class SearchResultComparer : IComparer
    {
        public bool InvertSort { get; set; }

        public int SortColumn { get; set; }

        public int Compare(object? x, object? y)
        {
            var i1 = (SearchResultItem)x!;
            var i2 = (SearchResultItem)y!;

            int result;
            if (SortColumn == 0)
            {
                string? s1 = i1.Tag?.ToString();
                string? s2 = i2.Tag?.ToString();
                result = string.Compare(s1, s2);
            }
            else
            {
                int v1 = i1.Tag switch
                {
                    MstatTypeDefinition def => SortColumn == 1 ? def.Size : def.AggregateSize,
                    MstatTypeSpecification spec => SortColumn == 1 ? spec.Size : spec.AggregateSize,
                    MstatMemberDefinition mem => SortColumn == 1 ? mem.Size : mem.AggregateSize,
                    MstatMethodSpecification met => met.Size,
                    _ => throw new InvalidOperationException()
                };
                int v2 = i2.Tag switch
                {
                    MstatTypeDefinition def => SortColumn == 1 ? def.Size : def.AggregateSize,
                    MstatTypeSpecification spec => SortColumn == 1 ? spec.Size : spec.AggregateSize,
                    MstatMemberDefinition mem => SortColumn == 1 ? mem.Size : mem.AggregateSize,
                    MstatMethodSpecification met => met.Size,
                    _ => throw new InvalidOperationException()
                };
                result = v1.CompareTo(v2);
            }

            return InvertSort ? -result : result;
        }
    }
}
