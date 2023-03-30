namespace sizoscope
{
    partial class RootForm
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
            _tree = new TreeView();
            SuspendLayout();
            // 
            // _tree
            // 
            _tree.Dock = DockStyle.Fill;
            _tree.Location = new Point(0, 0);
            _tree.Name = "_tree";
            _tree.Size = new Size(502, 335);
            _tree.TabIndex = 0;
            // 
            // RootForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(502, 335);
            Controls.Add(_tree);
            Name = "RootForm";
            Text = "RootForm";
            ResumeLayout(false);
        }

        #endregion

        private TreeView _tree;
    }
}