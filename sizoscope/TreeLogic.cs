using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using static MstatData;

namespace sizoscope
{
    public sealed class TreeNode : INotifyPropertyChanged
    {
        private string? name;
        private int imageIndex;
        private bool expaneded;

        public event PropertyChangedEventHandler? PropertyChanged;

        public TreeNode(string? name, int imageIndex)
        {
            Name = name;
            ImageIndex = imageIndex;
        }

        public string? Name
        {
            get
            {
                if (!expaneded)
                {
                    expaneded = true;
                    TreeLogic.Expand(this);
                }
                return name;
            }
            set
            {
                name = value;
                PropertyChanged?.Invoke(value, new(nameof(Name)));
            }
        }

        public int ImageIndex
        {
            get => imageIndex;
            set
            {
                imageIndex = value;
                PropertyChanged?.Invoke(this, new(nameof(ImageIndex)));
            }
        }

        public object? Tag { get; set; }

        public TreeNode? FirstNode => Nodes.FirstOrDefault();

        public ObservableCollection<TreeNode> Nodes { get; } = new();
    }

    public class TreeLogic
    {
        public static void RefreshTree(ObservableCollection<TreeNode> items, MstatData data, Sorter sorter)
        {
            items.Clear();
            var asms = data.GetScopes();
            foreach (var asm in sorter.Sort(asms))
            {
                // Do not show for now. This is currently not possible to diff and just causes problems.
                if (asm.Name == "System.Private.CompilerGenerated")
                    continue;

                // Aggregate size can be zero if this is a diff
                if (asm.AggregateSize == 0)
                    continue;

                string name = $"{asm.Name} ({AsFileSize(asm.AggregateSize)})";
                TreeNode node = new TreeNode(name, 0);
                node.Tag = asm;
                items.Add(node);
            }
        }

        public static void Expand(TreeNode node)
        {
            Debug.WriteLine($"Expanding {node.Name}");
            const int instantiationsImageIndex = 4;

            if (node.Tag is MstatAssembly asm)
            {
                var namespacesAndSizes = asm.GetTypes()
                    .Where(t => t.Namespace.Length > 0)
                    .GroupBy(t => t.Namespace)
                    .Select(g => (Name: g.Key, AggregateSize: g.Sum(t => t.AggregateSize)));
                foreach (var ns in namespacesAndSizes)
                {
                    string name = $"{ns.Name} ({AsFileSize(ns.AggregateSize)})";
                    var newNode = new TreeNode(name, 1);
                    newNode.Tag = (asm, ns.Name);
                    node.Nodes.Add(newNode);
                }

                AppendTypes(node, asm.GetTypes(), d => d.Namespace.Length == 0);
            }
            else if (node.Tag is (MstatAssembly a, string ns))
            {
                AppendTypes(node, a.GetTypes(), d => d.Namespace == ns);
            }
            else if (node.Tag is MstatTypeDefinition genericDef && node.ImageIndex == instantiationsImageIndex)
            {
                foreach (var inst in genericDef.GetTypeSpecifications())
                {
                    string name = $"{inst} ({AsFileSize(inst.AggregateSize)})";
                    var newNode = new TreeNode(name, 2);
                    newNode.Tag = inst;

                    node.Nodes.Add(newNode);
                }
            }
            else if (node.Tag is MstatTypeSpecification spec)
            {
                AppendMembers(node, spec.GetMembers());
            }
            else if (node.Tag is MstatTypeDefinition def)
            {
                if (def.GetTypeSpecifications().MoveNext())
                {
                    TreeNode newNode = new TreeNode("Instantiations",
                        instantiationsImageIndex);
                    newNode.Tag = def;
                    node.Nodes.Add(newNode);
                }
                AppendTypes(node, def.GetNestedTypes(), x => true);
                AppendMembers(node, def.GetMembers());
            }
            else if (node.Tag is MstatMemberDefinition memberDef)
            {
                foreach (var inst in memberDef.GetInstantiations())
                {
                    string name = $"{inst} ({AsFileSize(inst.Size)})";
                    var newNode = new TreeNode(name, 3);
                    newNode.Tag = inst;
                    node.Nodes.Add(newNode);
                }
            }

            static void AppendTypes(TreeNode node, Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> list, Func<MstatTypeDefinition, bool> filter)
            {
                foreach (var t in list.Where(filter))
                {
                    string name = $"{t.Name} ({AsFileSize(t.AggregateSize)})";
                    var n = new TreeNode(name, 2);
                    n.Tag = t;
                    node.Nodes.Add(n);
                }
            }

            static void AppendMembers(TreeNode node, Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> list)
            {
                foreach (var t in list)
                {
                    string name = $"{t} ({AsFileSize(t.AggregateSize)})";
                    var n = new TreeNode(name, 3);
                    n.Tag = t;
                    node.Nodes.Add(n);
                }
            }
        }

        public static string AsFileSize(int size)
            => size switch
            {
                < 1024 => $"{size:F0} B",
                < 1024 * 1024 => $"{size / 1024f:F1} kB",
                _ => $"{size / (1024f * 1024f):F1} MB",
            };

        public class Sorter
        {
            public string Key { get; private set; }

            private Sorter(string key) => Key = key;

            public IEnumerable<MstatAssembly> Sort(IEnumerable<MstatAssembly> asms)
                => Key == "Name" ? asms.OrderBy(a => a.Name) : asms.OrderByDescending(a => a.AggregateSize);
            public IEnumerable<(string Name, int AggregateSize)> Sort(IEnumerable<(string Name, int AggregateSize)> ns)
                => Key == "Name" ? ns.OrderBy(n => n.Name) : ns.OrderByDescending(n => n.AggregateSize);

            public IEnumerable<MstatTypeSpecification> Sort(IEnumerable<MstatTypeSpecification> specs)
                => Key == "Name" ? specs.OrderBy(s => s.ToString()) : specs.OrderByDescending(s => s.AggregateSize);

            public IEnumerable<MstatTypeDefinition> Sort(IEnumerable<MstatTypeDefinition> types)
                => Key == "Name" ? types.OrderBy(t => t.Name) : types.OrderByDescending(t => t.AggregateSize);

            public IEnumerable<MstatMemberDefinition> Sort(IEnumerable<MstatMemberDefinition> members)
                => Key == "Name" ? members.OrderBy(t => t.Name) : members.OrderByDescending(t => t.AggregateSize);

            public static Sorter ByName() => new Sorter("Name");
            public static Sorter BySize() => new Sorter("Size");

            public override string ToString() => $"Sort by {Key}";
        }
    }
}