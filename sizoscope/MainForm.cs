using static MstatData;
using System.Reflection.Metadata;
using System.Collections;
using System.Reflection;

#pragma warning disable 8509 // switch is not exhaustive

namespace sizoscope
{
    public partial class MainForm : Form
    {
        private string _fileName;
        private MstatData _data;

        private TreeLogic.Sorter TreeSorter => (TreeLogic.Sorter)_sortByComboBox.SelectedItem;

        public MainForm()
        {
            InitializeComponent();

            _sortByComboBox.Items.Add(TreeLogic.Sorter.BySize());
            _sortByComboBox.Items.Add(TreeLogic.Sorter.ByName());
            _sortByComboBox.SelectedIndex = 0;

            _searchComboBox.SelectedIndex = 1;

            _searchResultsListView.ListViewItemSorter = new SearchResultComparer();
        }

        public MainForm(string fileName)
            : this()
        {
            LoadData(fileName);
        }

        private void LoadData(string fileName)
        {
            _fileName = fileName;

            if (_data != null)
                _data.Dispose();

            _data = MstatData.Read(fileName);

            Text = $"{Path.GetFileName(fileName)} - Sizoscope";

            _toolStripStatusLabel.Text = $"Total accounted size: {TreeLogic.AsFileSize(_data.Size)}";

            RefreshViews();

            _reloadButton.Enabled = true;
            _diffButton.Enabled = true;
        }

        private void OpenButtonClick(object sender, EventArgs e)
        {
            if (_openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadData(_openFileDialog.FileName);
            }
        }

        private void RefreshViews()
        {
            TreeLogic.RefreshTree(_tree, _data, TreeSorter);

            if (_findButton.Checked)
                RefreshSearch();
        }

        private void TreeBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeLogic.BeforeExpand(e.Node, TreeSorter);
        }

        private void _sortByComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_data != null)
                TreeLogic.RefreshTree(_tree, _data, TreeSorter);
        }

        private void DiffButtonClick(object sender, EventArgs e)
        {
            if (_openFileDialog.ShowDialog() == DialogResult.OK)
            {
                MstatData right = MstatData.Read(_openFileDialog.FileName);

                (MstatData leftDiff, MstatData rightDiff) = MstatData.Diff(_data, right);

                new DiffForm(leftDiff, rightDiff, right.Size - _data.Size).ShowDialog(this);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.O))
            {
                OpenButtonClick(null, null);
                return true;
            }

            if (keyData == (Keys.Control | Keys.F))
            {
                _findButton.Checked = !_findButton.Checked;
                if (_findButton.Checked)
                    RefreshSearch();
                return true;
            }

            if (keyData == (Keys.Control | Keys.D) && _diffButton.Enabled)
            {
                DiffButtonClick(null, null);
                return true;
            }

            if (keyData == (Keys.Control | Keys.R) && _reloadButton.Enabled)
            {
                ReloadButtonClick(null, null);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void RefreshSearch()
        {
            if (_data == null)
                return;

            var sorter = _searchResultsListView.ListViewItemSorter;
            _searchResultsListView.ListViewItemSorter = null;

            _searchResultsListView.BeginUpdate();
            _searchResultsListView.Items.Clear();

            foreach (var asm in _data.GetScopes())
            {
                if (asm.Name == "System.Private.CompilerGenerated")
                    continue;

                AddTypes(this, asm.GetTypes());
            }

            static void AddTypes(MainForm form, Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> types)
            {
                foreach (var t in types)
                {
                    if (form._searchComboBox.SelectedIndex is 0 or 1
                        && (t.Name.Contains(form._searchTextBox.Text) || t.Namespace.Contains(form._searchTextBox.Text)))
                    {
                        var newItem = new ListViewItem(new string[]
                        {
                            t.ToString(),
                            TreeLogic.AsFileSize(t.Size),
                            TreeLogic.AsFileSize(t.AggregateSize)
                        });

                        newItem.Tag = t;

                        form._searchResultsListView.Items.Add(newItem);
                    }

                    AddTypes(form, t.GetNestedTypes());

                    if (form._searchComboBox.SelectedIndex is 0 or 2)
                        AddMembers(form, t.GetMembers());
                }
            }

            static void AddMembers(MainForm form, Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> members)
            {
                foreach (var m in members)
                {
                    if (m.Name.Contains(form._searchTextBox.Text))
                    {
                        var newItem = new ListViewItem(new string[]
                        {
                        m.ToQualifiedString(),
                        TreeLogic.AsFileSize(m.Size),
                        TreeLogic.AsFileSize(m.AggregateSize)
                        });

                        newItem.Tag = m;

                        form._searchResultsListView.Items.Add(newItem);
                    }
                }
            }

            _searchResultsListView.EndUpdate();

            _searchResultsListView.ListViewItemSorter = sorter;
        }

        class SearchResultComparer : IComparer
        {
            public bool InvertSort { get; set; }

            public int SortColumn { get; set; }

            public int Compare(object x, object y)
            {
                var i1 = (ListViewItem)x;
                var i2 = (ListViewItem)y;

                int result;
                if (SortColumn == 0)
                {
                    string s1 = i1.Text;
                    string s2 = i2.Text;
                    result = string.Compare(s1, s2, StringComparison.Ordinal);
                }
                else
                {
                    int v1 = i1.Tag switch
                    {
                        MstatTypeDefinition def => SortColumn == 1 ? def.Size : def.AggregateSize,
                        MstatTypeSpecification spec => SortColumn == 1 ? spec.Size : spec.AggregateSize,
                        MstatMemberDefinition mem => SortColumn == 1 ? mem.Size : mem.AggregateSize,
                        MstatMethodSpecification met => met.Size,
                    };
                    int v2 = i2.Tag switch
                    {
                        MstatTypeDefinition def => SortColumn == 1 ? def.Size : def.AggregateSize,
                        MstatTypeSpecification spec => SortColumn == 1 ? spec.Size : spec.AggregateSize,
                        MstatMemberDefinition mem => SortColumn == 1 ? mem.Size : mem.AggregateSize,
                        MstatMethodSpecification met => met.Size,
                    };
                    result = v1.CompareTo(v2);
                }

                return InvertSort ? -result : result;
            }
        }

        private void _searchResultsListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var sorter = (SearchResultComparer)_searchResultsListView.ListViewItemSorter;
            if (e.Column == sorter.SortColumn)
                sorter.InvertSort = !sorter.InvertSort;
            else
                sorter.SortColumn = e.Column;

            _searchResultsListView.Sort();
        }

        private void ReloadButtonClick(object sender, EventArgs e)
        {
            LoadData(_fileName);
        }

        private void _searchTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_data != null)
                RefreshSearch();
        }

        private void _searchComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshSearch();
        }

        private void FindButtonClick(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = !_findButton.Checked;
            if (_findButton.Checked)
            {
                RefreshSearch();
                _searchTextBox.Focus();
            }
        }

        private void _tree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (!_data.DgmlSupported)
            {
                MessageBox.Show("Dependency graph information is only available in .NET 8 Preview 4 or later.");
                return;
            }

            if (!_data.DgmlAvailable)
            {
                MessageBox.Show("Dependency graph data was not found. Ensure IlcGenerateDgmlFile=true is specified.");
                return;
            }

            int? id = e.Node.Tag switch
            {
                MstatTypeDefinition typedef => typedef.NodeId,
                MstatTypeSpecification typespec => typespec.NodeId,
                MstatMemberDefinition memberdef => memberdef.NodeId,
                MstatMethodSpecification methodspec => methodspec.NodeId,
                _ => null
            };

            if (id.HasValue)
            {
                if (id.Value < 0)
                {
                    MessageBox.Show("This node was not used directly and is included for display purposes only. Try analyzing sub nodes.");
                    return;
                }

                var node = _data.GetNodeForId(id.Value);
                if (node == null)
                {
                    MessageBox.Show("Unable to load dependency graph. Was IlcGenerateDgmlFile=true specified?");
                    return;
                }

                new RootForm(node).ShowDialog(this);
            }
        }

        private void _tree_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void _tree_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Holding alt will open a diff
            if ((e.KeyState & 32) == 32 && _data != null)
            {
                // BeginInvoke so that we don't block the drag source while the modal is open
                BeginInvoke(() =>
                {
                    MstatData right = MstatData.Read(files[0]);
                    (MstatData leftDiff, MstatData rightDiff) = MstatData.Diff(_data, right);
                    new DiffForm(leftDiff, rightDiff, right.Size - _data.Size).ShowDialog(this);
                });
            }
            else
            {
                LoadData(files[0]);
            }
        }

        private void _aboutButton_Click(object sender, EventArgs e)
        {
            var page = new TaskDialogPage()
            {
                Caption = "About Sizoscope",
                Heading = $"Sizoscope {GetType().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version}",
                Icon = TaskDialogIcon.Information,
                Expander = new TaskDialogExpander
                {
                    CollapsedButtonText = "Third party notices",
                    Text = """
                           License notice for SharpDevelop

                                                      The MIT License (MIT)

                           Copyright (c) 2002-2016 AlphaSierraPapa

                           Permission is hereby granted, free of charge, to any person obtaining a copy
                           of this software and associated documentation files (the "Software"), to deal
                           in the Software without restriction, including without limitation the rights
                           to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
                           copies of the Software, and to permit persons to whom the Software is
                           furnished to do so, subject to the following conditions:

                           The above copyright notice and this permission notice shall be included in
                           all copies or substantial portions of the Software.

                           THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
                           IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
                           FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
                           AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
                           LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
                           OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
                           THE SOFTWARE.
                           """
                },
                Text = """
                       Copyright (c) 2023 Michal Strehovsky

                       https://github.com/MichalStrehovsky

                       .NET Native AOT binary size analysis tool.

                       This program is free software: you can redistribute it and/or modify
                       it under the terms of the GNU Affero General Public License as published
                       by the Free Software Foundation, either version 3 of the License, or
                       (at your option) any later version.
                       
                       This program is distributed in the hope that it will be useful,
                       but WITHOUT ANY WARRANTY; without even the implied warranty of
                       MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
                       GNU Affero General Public License for more details.
                       
                       You should have received a copy of the GNU Affero General Public License
                       along with this program.  If not, see <https://www.gnu.org/licenses/>.
                       """,
                DefaultButton = TaskDialogButton.OK,
                Buttons = new TaskDialogButtonCollection
                {
                    TaskDialogButton.OK,
                    { new TaskDialogButton("Project website") }
                },
            };


            if (TaskDialog.ShowDialog(page) != TaskDialogButton.OK)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = "https://github.com/MichalStrehovsky/sizoscope",
                    UseShellExecute = true,
                });
            }
        }
    }
}
