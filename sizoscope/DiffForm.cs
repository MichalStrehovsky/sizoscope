using static MstatData;

namespace sizoscope
{
    public partial class DiffForm : Form
    {
        private MstatData _leftDiff, _rightDiff;
        private TreeLogic.Sorter _sorter;

        public DiffForm(MstatData leftDiff, MstatData rightDiff, int diffSize)
        {
            InitializeComponent();

            _leftDiff = leftDiff;
            _rightDiff = rightDiff;
            _sorter = TreeLogic.Sorter.BySize();

            ImageList imageList = new MainForm()._imageList;
            _leftTree.ImageList = imageList;
            _rightTree.ImageList = imageList;

            TreeLogic.RefreshTree(_leftTree, _leftDiff, _sorter);
            TreeLogic.RefreshTree(_rightTree, _rightDiff, _sorter);

            _toolStripStatusLabel.Text = $"Total accounted difference: {TreeLogic.AsFileSize(diffSize)}";
        }

        private void _leftTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeLogic.BeforeExpand(e.Node, _sorter);
        }

        private void _rightTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeLogic.BeforeExpand(e.Node, _sorter);
        }

        private void _leftTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            NodeMouseDoubleClickCommon(e.Node, _leftDiff);
        }

        private void NodeMouseDoubleClickCommon(TreeNode treeNode, MstatData data)
        {
            if (!data.DgmlSupported)
            {
                MessageBox.Show("Dependency graph information is only available in .NET 8 Preview 4 or later.");
                return;
            }

            if (!data.DgmlAvailable)
            {
                MessageBox.Show("Dependency graph data was not found. Ensure IlcGenerateDgmlFile=true is specified.");
                return;
            }

            int? id = treeNode.Tag switch
            {
                MstatTypeDefinition typedef => typedef.NodeId,
                MstatTypeSpecification typespec => typespec.NodeId,
                MstatMemberDefinition memberdef => memberdef.NodeId,
                MstatMethodSpecification methodspec => methodspec.NodeId,
                int val => val,
                _ => null
            };

            if (id.HasValue)
            {
                if (id.Value < 0)
                {
                    MessageBox.Show("This node was not used directly and is included for display purposes only. Try analyzing sub nodes.");
                    return;
                }

                var node = data.GetNodeForId(id.Value, out string name);
                if (node == null)
                {
                    MessageBox.Show($"Could not find path to roots from {name}.");
                    return;
                }

                new RootForm(node).ShowDialog(this);
            }
        }

        private void _rightTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            NodeMouseDoubleClickCommon(e.Node, _rightDiff);
        }
    }
}
