namespace sizoscope
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            toolStripContainer1 = new ToolStripContainer();
            splitContainer1 = new SplitContainer();
            tableLayoutPanel1 = new TableLayoutPanel();
            _sortByComboBox = new ComboBox();
            _tree = new TreeView();
            _imageList = new ImageList(components);
            tableLayoutPanel2 = new TableLayoutPanel();
            _searchTextBox = new TextBox();
            _searchComboBox = new ComboBox();
            _searchResultsListView = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            toolStrip1 = new ToolStrip();
            _openButton = new ToolStripButton();
            _reloadButton = new ToolStripButton();
            _diffButton = new ToolStripButton();
            _findButton = new ToolStripButton();
            _openFileDialog = new OpenFileDialog();
            _aboutButton = new ToolStripButton();
            toolStripContainer1.ContentPanel.SuspendLayout();
            toolStripContainer1.TopToolStripPanel.SuspendLayout();
            toolStripContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            toolStripContainer1.ContentPanel.Controls.Add(splitContainer1);
            toolStripContainer1.ContentPanel.Margin = new Padding(3, 2, 3, 2);
            toolStripContainer1.ContentPanel.Size = new Size(640, 334);
            toolStripContainer1.Dock = DockStyle.Fill;
            toolStripContainer1.Location = new Point(0, 0);
            toolStripContainer1.Margin = new Padding(3, 2, 3, 2);
            toolStripContainer1.Name = "toolStripContainer1";
            toolStripContainer1.Size = new Size(640, 361);
            toolStripContainer1.TabIndex = 0;
            toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            toolStripContainer1.TopToolStripPanel.Controls.Add(toolStrip1);
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Margin = new Padding(3, 2, 3, 2);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(tableLayoutPanel1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tableLayoutPanel2);
            splitContainer1.Panel2Collapsed = true;
            splitContainer1.Size = new Size(640, 334);
            splitContainer1.SplitterDistance = 278;
            splitContainer1.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(_sortByComboBox, 0, 0);
            tableLayoutPanel1.Controls.Add(_tree, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(640, 334);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // _sortByComboBox
            // 
            _sortByComboBox.Dock = DockStyle.Top;
            _sortByComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _sortByComboBox.FormattingEnabled = true;
            _sortByComboBox.Location = new Point(3, 2);
            _sortByComboBox.Margin = new Padding(3, 2, 3, 2);
            _sortByComboBox.Name = "_sortByComboBox";
            _sortByComboBox.Size = new Size(634, 23);
            _sortByComboBox.TabIndex = 1;
            _sortByComboBox.SelectedIndexChanged += _sortByComboBox_SelectedIndexChanged;
            // 
            // _tree
            // 
            _tree.AllowDrop = true;
            _tree.Dock = DockStyle.Fill;
            _tree.ImageIndex = 0;
            _tree.ImageList = _imageList;
            _tree.Location = new Point(3, 29);
            _tree.Margin = new Padding(3, 2, 3, 2);
            _tree.Name = "_tree";
            _tree.SelectedImageIndex = 0;
            _tree.Size = new Size(634, 303);
            _tree.TabIndex = 0;
            _tree.BeforeExpand += TreeBeforeExpand;
            _tree.NodeMouseDoubleClick += _tree_NodeMouseDoubleClick;
            _tree.DragDrop += _tree_DragDrop;
            _tree.DragEnter += _tree_DragEnter;
            // 
            // _imageList
            // 
            _imageList.ColorDepth = ColorDepth.Depth32Bit;
            _imageList.ImageStream = (ImageListStreamer)resources.GetObject("_imageList.ImageStream");
            _imageList.TransparentColor = Color.Transparent;
            _imageList.Images.SetKeyName(0, "Assembly.png");
            _imageList.Images.SetKeyName(1, "NameSpace.png");
            _imageList.Images.SetKeyName(2, "Class.png");
            _imageList.Images.SetKeyName(3, "Method.png");
            _imageList.Images.SetKeyName(4, "SubTypes.png");
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.Controls.Add(_searchTextBox, 0, 0);
            tableLayoutPanel2.Controls.Add(_searchComboBox, 1, 0);
            tableLayoutPanel2.Controls.Add(_searchResultsListView, 0, 1);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle());
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(96, 100);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // _searchTextBox
            // 
            _searchTextBox.Dock = DockStyle.Top;
            _searchTextBox.Location = new Point(3, 3);
            _searchTextBox.Name = "_searchTextBox";
            _searchTextBox.Size = new Size(1, 23);
            _searchTextBox.TabIndex = 1;
            _searchTextBox.TextChanged += _searchTextBox_TextChanged;
            // 
            // _searchComboBox
            // 
            _searchComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _searchComboBox.Items.AddRange(new object[] { "Types and methods", "Types", "Methods" });
            _searchComboBox.Location = new Point(-48, 3);
            _searchComboBox.Name = "_searchComboBox";
            _searchComboBox.Size = new Size(141, 23);
            _searchComboBox.TabIndex = 2;
            _searchComboBox.SelectedIndexChanged += _searchComboBox_SelectedIndexChanged;
            // 
            // _searchResultsListView
            // 
            _searchResultsListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _searchResultsListView.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3 });
            tableLayoutPanel2.SetColumnSpan(_searchResultsListView, 2);
            _searchResultsListView.FullRowSelect = true;
            _searchResultsListView.Location = new Point(3, 31);
            _searchResultsListView.Margin = new Padding(3, 2, 3, 2);
            _searchResultsListView.Name = "_searchResultsListView";
            _searchResultsListView.Size = new Size(90, 67);
            _searchResultsListView.TabIndex = 0;
            _searchResultsListView.UseCompatibleStateImageBehavior = false;
            _searchResultsListView.View = View.Details;
            _searchResultsListView.ColumnClick += _searchResultsListView_ColumnClick;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Name";
            columnHeader1.Width = 180;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Exclusive bytes";
            columnHeader2.Width = 100;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Inclusive bytes";
            columnHeader3.Width = 100;
            // 
            // toolStrip1
            // 
            toolStrip1.Dock = DockStyle.None;
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.ImageScalingSize = new Size(20, 20);
            toolStrip1.Items.AddRange(new ToolStripItem[] { _openButton, _reloadButton, _diffButton, _findButton, _aboutButton });
            toolStrip1.Location = new Point(3, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(154, 27);
            toolStrip1.TabIndex = 0;
            // 
            // _openButton
            // 
            _openButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _openButton.Image = (Image)resources.GetObject("_openButton.Image");
            _openButton.ImageTransparentColor = Color.Magenta;
            _openButton.Name = "_openButton";
            _openButton.Size = new Size(24, 24);
            _openButton.Text = "Open... (Ctrl-O)";
            _openButton.Click += OpenButtonClick;
            // 
            // _reloadButton
            // 
            _reloadButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _reloadButton.Enabled = false;
            _reloadButton.Image = (Image)resources.GetObject("_reloadButton.Image");
            _reloadButton.ImageTransparentColor = Color.Magenta;
            _reloadButton.Name = "_reloadButton";
            _reloadButton.Size = new Size(24, 24);
            _reloadButton.Text = "Reload (Ctrl-R)";
            _reloadButton.Click += ReloadButtonClick;
            // 
            // _diffButton
            // 
            _diffButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _diffButton.Enabled = false;
            _diffButton.Image = (Image)resources.GetObject("_diffButton.Image");
            _diffButton.ImageTransparentColor = Color.Magenta;
            _diffButton.Name = "_diffButton";
            _diffButton.Size = new Size(24, 24);
            _diffButton.Text = "Diff... (Ctrl-D)";
            _diffButton.Click += DiffButtonClick;
            // 
            // _findButton
            // 
            _findButton.CheckOnClick = true;
            _findButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _findButton.Image = (Image)resources.GetObject("_findButton.Image");
            _findButton.ImageTransparentColor = Color.Magenta;
            _findButton.Name = "_findButton";
            _findButton.Size = new Size(24, 24);
            _findButton.Text = "Find (Ctrl-F)";
            _findButton.CheckedChanged += FindButtonClick;
            // 
            // _openFileDialog
            // 
            _openFileDialog.FileName = "openFileDialog1";
            _openFileDialog.Filter = "Managed statistics|*.mstat";
            // 
            // _aboutButton
            // 
            _aboutButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _aboutButton.Image = (Image)resources.GetObject("_aboutButton.Image");
            _aboutButton.ImageTransparentColor = Color.Magenta;
            _aboutButton.Name = "_aboutButton";
            _aboutButton.Size = new Size(24, 24);
            _aboutButton.Text = "About...";
            _aboutButton.Click += _aboutButton_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 361);
            Controls.Add(toolStripContainer1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "MainForm";
            Text = "Sizoscope";
            toolStripContainer1.ContentPanel.ResumeLayout(false);
            toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            toolStripContainer1.TopToolStripPanel.PerformLayout();
            toolStripContainer1.ResumeLayout(false);
            toolStripContainer1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private ToolStripContainer toolStripContainer1;
        private SplitContainer splitContainer1;
        private TreeView _tree;
        private ListView _searchResultsListView;
        private ToolStrip toolStrip1;
        private ToolStripButton _openButton;
        private OpenFileDialog _openFileDialog;
        private ComboBox _sortByComboBox;
        private ToolStripButton _diffButton;
        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private TextBox _searchTextBox;
        private ComboBox _searchComboBox;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        private ToolStripButton _reloadButton;
        private ToolStripButton _findButton;
        internal ImageList _imageList;
        private ToolStripButton _aboutButton;
    }
}