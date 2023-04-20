using Avalonia.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using static sizoscope.TreeLogic;

namespace sizoscope
{
    public sealed class TreeNode : INotifyPropertyChanged
    {
        private string? name;
        private NodeType type;
        private bool expaneded;

        public event PropertyChangedEventHandler? PropertyChanged;

        public TreeNode(string? name, NodeType type, Sorter sorter)
        {
            Name = name;
            Type = type;
            Sorter = sorter;
        }

        public string? Name
        {
            get
            {
                if (!expaneded)
                {
                    expaneded = true;
                    Expand(this);
                }
                return name;
            }
            set
            {
                name = value;
                PropertyChanged?.Invoke(value, new(nameof(Name)));
            }
        }

        public NodeType Type
        {
            get => type;
            set
            {
                type = value;
                PropertyChanged?.Invoke(this, new(nameof(Type)));
            }
        }

        public object? Tag { get; set; }

        public TreeNode? FirstNode => Nodes.FirstOrDefault();

        public ObservableCollection<TreeNode> Nodes { get; } = new();

        public Sorter Sorter { get; }
    }
}