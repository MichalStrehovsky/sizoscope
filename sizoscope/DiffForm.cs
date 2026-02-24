using static MstatData;

namespace sizoscope
{
    public partial class DiffForm : Form
    {
        private MstatData _leftDiff, _rightDiff;
        private TreeLogic.Sorter _sorter;
        private ResolvedFile _resolvedLeft, _resolvedRight;

        /// <summary>
        /// Creates a DiffForm with pre-computed diff data (used from MainForm diff button).
        /// </summary>
        public DiffForm(MstatData leftDiff, MstatData rightDiff, int diffSize)
        {
            InitializeComponent();
            InitializeCommon();

            _leftDiff = leftDiff;
            _rightDiff = rightDiff;

            TreeLogic.RefreshTree(_leftTree, _leftDiff, _sorter);
            TreeLogic.RefreshTree(_rightTree, _rightDiff, _sorter);

            _toolStripStatusLabel.Text = $"Total accounted difference: {TreeLogic.AsFileSize(diffSize)}";
        }

        /// <summary>
        /// Creates a DiffForm that loads and diffs the files asynchronously after showing the UI.
        /// Used from CLI when two file paths are passed.
        /// </summary>
        public DiffForm(string leftFilePath, string rightFilePath)
        {
            InitializeComponent();
            InitializeCommon();

            _toolStripStatusLabel.Text = "Loading and computing diff...";
            _leftTree.Enabled = false;
            _rightTree.Enabled = false;

            Shown += async (s, e) => await LoadDiffAsync(leftFilePath, rightFilePath);
        }

        private void InitializeCommon()
        {
            _sorter = TreeLogic.Sorter.BySize();

            ImageList imageList = new MainForm()._imageList;
            _leftTree.ImageList = imageList;
            _rightTree.ImageList = imageList;
        }

        private async Task LoadDiffAsync(string leftFilePath, string rightFilePath)
        {
            try
            {
                var result = await Task.Run(() =>
                {
                    var rl = ResolvedFile.Open(leftFilePath);
                    var rr = ResolvedFile.Open(rightFilePath);

                    MstatData left;
                    using (var ms = rl.OpenMstat())
                        left = MstatData.Read(ms, rl.MstatLength, rl.OpenDgml, loadDgmlAsync: true);

                    MstatData right;
                    using (var ms = rr.OpenMstat())
                        right = MstatData.Read(ms, rr.MstatLength, rr.OpenDgml);

                    (MstatData ld, MstatData rd) = MstatData.Diff(left, right);
                    int ds = right.Size - left.Size;

                    return (ld, rd, ds, rl, rr);
                });

                _resolvedLeft = result.rl;
                _resolvedRight = result.rr;
                _leftDiff = result.ld;
                _rightDiff = result.rd;

                TreeLogic.RefreshTree(_leftTree, _leftDiff, _sorter);
                TreeLogic.RefreshTree(_rightTree, _rightDiff, _sorter);

                _toolStripStatusLabel.Text = $"Total accounted difference: {TreeLogic.AsFileSize(result.ds)}";
                _leftTree.Enabled = true;
                _rightTree.Enabled = true;
            }
            catch (Exception ex)
            {
                _toolStripStatusLabel.Text = $"Error: {ex.Message}";
                _leftTree.Enabled = true;
                _rightTree.Enabled = true;
            }
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
            NodeMouseDoubleClickCommon(e.Node, _leftDiff, _rightDiff);
        }

        private void NodeMouseDoubleClickCommon(TreeNode treeNode, MstatData data, MstatData compare)
        {
            if (!data.DgmlSupported)
            {
                MessageBox.Show("Dependency graph information is only available in .NET 8 or later.");
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

                new RootForm(node, compare).ShowDialog(this);
            }
        }

        private void _rightTree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            NodeMouseDoubleClickCommon(e.Node, _rightDiff, _leftDiff);
        }
    }
}
