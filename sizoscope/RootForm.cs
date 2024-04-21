namespace sizoscope
{
    public partial class RootForm : Form
    {
        public RootForm()
        {
            InitializeComponent();
        }

        public RootForm(MstatData.Node node, MstatData compare = null)
            : this()
        {
            Text = $"Path from roots to {node.Name}";

            _tree.BeginUpdate();
            _tree.Nodes.Add(CreateTree(compare, node));
            _tree.EndUpdate();

            if (_tree.GetNodeCount(true) < 100000)
                _tree.ExpandAll();
        }

        private TreeNode CreateTree(MstatData compareData, MstatData.Node node, string label = null)
        {
            TreeNode result = new TreeNode(label == null ? node.Name : $"({label}) {node.Name}");

            if (compareData != null && compareData.DgmlAvailable)
            {
                if (!compareData.ContainsNamedNode(node.Name))
                    result.ForeColor = Color.RebeccaPurple;
            }

            foreach (var edge in node.Edges)
                result.Nodes.Add(CreateTree(compareData, edge.Node, edge.Label));

            return result;
        }
    }
}
