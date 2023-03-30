namespace sizoscope
{
    public partial class RootForm : Form
    {
        public RootForm()
        {
            InitializeComponent();
        }

        public RootForm(MstatData.Node node)
            : this()
        {
            Text = $"Path from roots to {node.Name}";

            _tree.BeginUpdate();
            _tree.Nodes.Add(CreateTree(node));
            _tree.EndUpdate();

            _tree.ExpandAll();
        }

        private TreeNode CreateTree(MstatData.Node node, string label = null)
        {
            TreeNode result = new TreeNode(label == null ? node.Name : $"({label}) {node.Name}");
            foreach (var edge in node.Edges)
                result.Nodes.Add(CreateTree(edge.Node, edge.Label));

            return result;
        }
    }
}
