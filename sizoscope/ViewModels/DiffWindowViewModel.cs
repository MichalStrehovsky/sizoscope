using System.Collections.ObjectModel;
using static sizoscope.TreeLogic;
using System.ComponentModel;

namespace sizoscope.ViewModels;

public class DiffWindowViewModel : INotifyPropertyChanged
{
    private readonly MstatData _baseline, _compare;
    public DiffWindowViewModel(MstatData baseline, MstatData compare)
    {
        var baselineTree = new ObservableCollection<TreeNode>();
        var compareTree = new ObservableCollection<TreeNode>();
        (_baseline, _compare) = MstatData.Diff(baseline, compare);
        RefreshTree(baselineTree, _baseline, Sorter.BySize());
        RefreshTree(compareTree, _compare, Sorter.BySize());
        BaselineItems = baselineTree;
        CompareItems = compareTree;
    }

    private int _baselineSortMode, _compareSortMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TreeNode> BaselineItems { get; }
    public ObservableCollection<TreeNode> CompareItems { get; }

    public Sorter BaselineSorter => BaselineSortMode is 0 ? Sorter.BySize() : Sorter.ByName();
    public Sorter CompareSorter => CompareSortMode is 0 ? Sorter.BySize() : Sorter.ByName();

    public int BaselineSortMode
    {
        get => _baselineSortMode;
        set
        {
            if (value != _baselineSortMode)
            {
                _baselineSortMode = value;
                PropertyChanged?.Invoke(this, new(nameof(BaselineSortMode)));
                PropertyChanged?.Invoke(this, new(nameof(BaselineSorter)));
                RefreshTree(BaselineItems, _baseline, BaselineSorter);
            }
        }
    }

    public int CompareSortMode
    {
        get => _compareSortMode;
        set
        {
            if (value != _compareSortMode)
            {
                _compareSortMode = value;
                PropertyChanged?.Invoke(this, new(nameof(CompareSortMode)));
                PropertyChanged?.Invoke(this, new(nameof(CompareSorter)));
                RefreshTree(CompareItems, _compare, CompareSorter);
            }
        }
    }
}
