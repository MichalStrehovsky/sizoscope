namespace sizoscope
{
    partial class DiffForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DiffForm));
            tableLayoutPanel1 = new TableLayoutPanel();
            _leftTree = new TreeView();
            _rightTree = new TreeView();
            label1 = new Label();
            label2 = new Label();
            toolStripContainer1 = new ToolStripContainer();
            statusStrip1 = new StatusStrip();
            _toolStripStatusLabel = new ToolStripStatusLabel();
            tableLayoutPanel1.SuspendLayout();
            toolStripContainer1.BottomToolStripPanel.SuspendLayout();
            toolStripContainer1.ContentPanel.SuspendLayout();
            toolStripContainer1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(_leftTree, 0, 1);
            tableLayoutPanel1.Controls.Add(_rightTree, 1, 1);
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Controls.Add(label2, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(755, 394);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // _leftTree
            // 
            _leftTree.Dock = DockStyle.Fill;
            _leftTree.Location = new Point(3, 18);
            _leftTree.Name = "_leftTree";
            _leftTree.Size = new Size(371, 373);
            _leftTree.TabIndex = 0;
            _leftTree.BeforeExpand += _leftTree_BeforeExpand;
            _leftTree.NodeMouseDoubleClick += _leftTree_NodeMouseDoubleClick;
            // 
            // _rightTree
            // 
            _rightTree.Dock = DockStyle.Fill;
            _rightTree.Location = new Point(380, 18);
            _rightTree.Name = "_rightTree";
            _rightTree.Size = new Size(372, 373);
            _rightTree.TabIndex = 1;
            _rightTree.BeforeExpand += _rightTree_BeforeExpand;
            _rightTree.NodeMouseDoubleClick += _rightTree_NodeMouseDoubleClick;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(94, 15);
            label1.TabIndex = 2;
            label1.Text = "Only in baseline:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(380, 0);
            label2.Name = "label2";
            label2.Size = new Size(98, 15);
            label2.TabIndex = 3;
            label2.Text = "Only in compare:";
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.BottomToolStripPanel
            // 
            toolStripContainer1.BottomToolStripPanel.Controls.Add(statusStrip1);
            // 
            // toolStripContainer1.ContentPanel
            // 
            toolStripContainer1.ContentPanel.Controls.Add(tableLayoutPanel1);
            toolStripContainer1.ContentPanel.Size = new Size(755, 394);
            toolStripContainer1.Dock = DockStyle.Fill;
            toolStripContainer1.Location = new Point(0, 0);
            toolStripContainer1.Name = "toolStripContainer1";
            toolStripContainer1.Size = new Size(755, 416);
            toolStripContainer1.TabIndex = 1;
            toolStripContainer1.Text = "toolStripContainer1";
            // 
            // statusStrip1
            // 
            statusStrip1.Dock = DockStyle.None;
            statusStrip1.Items.AddRange(new ToolStripItem[] { _toolStripStatusLabel });
            statusStrip1.Location = new Point(0, 0);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(755, 22);
            statusStrip1.TabIndex = 0;
            // 
            // _toolStripStatusLabel
            // 
            _toolStripStatusLabel.Name = "_toolStripStatusLabel";
            _toolStripStatusLabel.Size = new Size(118, 17);
            _toolStripStatusLabel.Text = "toolStripStatusLabel1";
            // 
            // DiffForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(755, 416);
            Controls.Add(toolStripContainer1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "DiffForm";
            Text = "Sizoscope";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
            toolStripContainer1.BottomToolStripPanel.PerformLayout();
            toolStripContainer1.ContentPanel.ResumeLayout(false);
            toolStripContainer1.ResumeLayout(false);
            toolStripContainer1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TreeView _leftTree;
        private TreeView _rightTree;
        private Label label1;
        private Label label2;
        private ToolStripContainer toolStripContainer1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel _toolStripStatusLabel;
    }
}