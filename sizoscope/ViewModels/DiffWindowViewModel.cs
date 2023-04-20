using System.Collections.ObjectModel;

namespace sizoscope.ViewModels;

public class DiffWindowViewModel
{
    public ObservableCollection<TreeNode> BaselineItems { get; }
    public ObservableCollection<TreeNode> CompareItems { get; }

    public DiffWindowViewModel(MstatData baseline, MstatData compare)
    {
        var baselineTree = new ObservableCollection<TreeNode>();
        var compareTree = new ObservableCollection<TreeNode>();
        var (left, right) = MstatData.Diff(baseline, compare);
        TreeLogic.RefreshTree(baselineTree, left, TreeLogic.Sorter.ByName());
        TreeLogic.RefreshTree(compareTree, right, TreeLogic.Sorter.ByName());
        BaselineItems = baselineTree;
        CompareItems = compareTree;
    }
}
