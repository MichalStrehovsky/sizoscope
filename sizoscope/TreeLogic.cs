using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection.Metadata;
using static MstatData;

namespace sizoscope
{
    internal sealed class TreeNode : INotifyPropertyChanged
    {
        private string? name;
        private int imageIndex;

        public event PropertyChangedEventHandler? PropertyChanged;

        public TreeNode() { }

        public TreeNode(string? name, int imageIndex)
        {
            Name = name;
            ImageIndex = imageIndex;
        }

        public TreeNode(string? name, int imageIndex, TreeNodeCollection nodes)
        {
            Name = name;
            ImageIndex = imageIndex;
            Nodes = nodes;
        }

        public string? Name
        {
            get => name;
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

        public TreeNode FirstNode => Nodes.First();

        public TreeNodeCollection Nodes { get; } = new TreeNodeCollection();
        public void BeginUpdate() => Nodes.BeginUpdate();
        public void EndUpdate() => Nodes.EndUpdate();
    }

    internal sealed class TreeNodeCollection : IList<TreeNode>, INotifyCollectionChanged
    {
        private readonly ObservableCollection<TreeNode> nodes = new();
        private bool isUpdating;

        public TreeNode this[int index] { get => nodes[index]; set => nodes[index] = value; }

        public int Count => nodes.Count;

        public bool IsReadOnly => false;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public TreeNodeCollection()
        {
            nodes.CollectionChanged += InnerCollectionChanged;
        }

        private void InnerCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (isUpdating) return;
            CollectionChanged?.Invoke(this, e);
        }

        public void Add(TreeNode item) => nodes.Add(item);

        public void BeginUpdate()
        {
            isUpdating = true;
        }

        public void EndUpdate()
        {
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));
            isUpdating = false;
        }

        public void Clear() => nodes.Clear();

        public bool Contains(TreeNode item) => nodes.Contains(item);

        public void CopyTo(TreeNode[] array, int arrayIndex) => nodes.CopyTo(array, arrayIndex);

        public IEnumerator<TreeNode> GetEnumerator() => nodes.GetEnumerator();

        public int IndexOf(TreeNode item) => nodes.IndexOf(item);

        public void Insert(int index, TreeNode item) => nodes.Insert(index, item);

        public bool Remove(TreeNode item) => nodes.Remove(item);

        public void RemoveAt(int index) => nodes.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => nodes.GetEnumerator();
    }

    internal class TreeLogic
    {
        public static void RefreshTree(TreeNode tree, MstatData data, Sorter sorter)
        {
            tree.BeginUpdate();
            tree.Nodes.Clear();
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
                TreeNode node = new TreeNode(name, 0, new() { new() });
                node.Tag = asm;
                tree.Nodes.Add(node);
            }
            tree.EndUpdate();
        }

        public static void BeforeExpand(TreeNode node, Sorter sorter)
        {
            if (node.FirstNode.Tag != null)
                return;

            node.Nodes.Clear();

            const int instantiationsImageIndex = 4;

            if (node.Tag is MstatAssembly asm)
            {
                var namespacesAndSizes = asm.GetTypes()
                    .Where(t => t.Namespace.Length > 0)
                    .GroupBy(t => t.Namespace)
                    .Select(g => (Name: g.Key, AggregateSize: g.Sum(t => t.AggregateSize)));
                foreach (var ns in sorter.Sort(namespacesAndSizes))
                {
                    string name = $"{ns.Name} ({AsFileSize(ns.AggregateSize)})";
                    var newNode = new TreeNode(name, 1, new() { new() });
                    newNode.Tag = (asm, ns.Name);
                    node.Nodes.Add(newNode);
                }

                AppendTypes(sorter, node, asm.GetTypes(), d => d.Namespace.Length == 0);
            }
            else if (node.Tag is (MstatAssembly a, string ns))
            {
                AppendTypes(sorter, node, a.GetTypes(), d => d.Namespace == ns);
            }
            else if (node.Tag is MstatTypeDefinition genericDef && node.ImageIndex == instantiationsImageIndex)
            {
                foreach (var inst in sorter.Sort(genericDef.GetTypeSpecifications()))
                {
                    string name = $"{inst} ({AsFileSize(inst.AggregateSize)})";
                    var newNode = new TreeNode(name, 2);
                    newNode.Tag = inst;

                    if (inst.GetMembers().MoveNext())
                        newNode.Nodes.Add(new TreeNode());

                    node.Nodes.Add(newNode);
                }
            }
            else if (node.Tag is MstatTypeSpecification spec)
            {
                AppendMembers(sorter, node, spec.GetMembers());
            }
            else if (node.Tag is MstatTypeDefinition def)
            {
                if (def.GetTypeSpecifications().MoveNext())
                {
                    TreeNode newNode = new TreeNode("Instantiations",
                        instantiationsImageIndex,
                        new() { new() });
                    newNode.Tag = def;
                    node.Nodes.Add(newNode);
                }
                AppendTypes(sorter, node, def.GetNestedTypes(), x => true);
                AppendMembers(sorter, node, def.GetMembers());
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

            static void AppendTypes(Sorter sorter, TreeNode node, Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> list, Func<MstatTypeDefinition, bool> filter)
            {
                foreach (var t in sorter.Sort(list.Where(filter)))
                {
                    string name = $"{t.Name} ({AsFileSize(t.AggregateSize)})";
                    var n = new TreeNode(name, 2, new() { new() });
                    n.Tag = t;
                    node.Nodes.Add(n);
                }
            }

            static void AppendMembers(Sorter sorter, TreeNode node, Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> list)
            {
                foreach (var t in sorter.Sort(list))
                {
                    string name = $"{t} ({AsFileSize(t.AggregateSize)})";
                    var n = new TreeNode(name, 3);
                    if (t.GetInstantiations().Any())
                        n.Nodes.Add(new TreeNode());
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
            private readonly string _key;

            private Sorter(string key) => _key = key;

            public IEnumerable<MstatAssembly> Sort(IEnumerable<MstatAssembly> asms)
                => _key == "Name" ? asms.OrderBy(a => a.Name) : asms.OrderByDescending(a => a.AggregateSize);
            public IEnumerable<(string Name, int AggregateSize)> Sort(IEnumerable<(string Name, int AggregateSize)> ns)
                => _key == "Name" ? ns.OrderBy(n => n.Name) : ns.OrderByDescending(n => n.AggregateSize);

            public IEnumerable<MstatTypeSpecification> Sort(IEnumerable<MstatTypeSpecification> specs)
                => _key == "Name" ? specs.OrderBy(s => s.ToString()) : specs.OrderByDescending(s => s.AggregateSize);

            public IEnumerable<MstatTypeDefinition> Sort(IEnumerable<MstatTypeDefinition> types)
                => _key == "Name" ? types.OrderBy(t => t.Name) : types.OrderByDescending(t => t.AggregateSize);

            public IEnumerable<MstatMemberDefinition> Sort(IEnumerable<MstatMemberDefinition> members)
                => _key == "Name" ? members.OrderBy(t => t.Name) : members.OrderByDescending(t => t.AggregateSize);

            public static Sorter ByName() => new Sorter("Name");
            public static Sorter BySize() => new Sorter("Size");

            public override string ToString() => $"Sort by {_key}";
        }
    }
}