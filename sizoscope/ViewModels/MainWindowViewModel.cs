using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static MstatData;

namespace sizoscope.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private MstatData? _data;
    private string? fileName;


    public TreeLogic.Sorter Sorter { get; set; } = TreeLogic.Sorter.BySize();
    public ObservableCollection<TreeNode> Items { get; } = new();

    public string? FileName
    {
        get => fileName;
        set
        {
            if (value is null || value == FileName) return;

            fileName = value;
            _data = Read(value);

            TreeLogic.RefreshTree(Items, _data, Sorter);
            // RefreshSearch();
        }
    }

    //private void RefreshSearch()
    //{
    //    const int MaxSearchResults = 250;

    //    _searchResultsListView.BeginUpdate();
    //    _searchResultsListView.Items.Clear();

    //    if (_searchTextBox.Text.Length > 0)
    //    {
    //        foreach (var asm in _data.GetScopes())
    //        {
    //            if (asm.Name == "System.Private.CompilerGenerated")
    //                continue;

    //            AddTypes(asm.GetTypes());
    //        }
    //    }

    //    static void AddTypes(Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> types)
    //    {
    //        foreach (var t in types)
    //        {
    //            if (_searchResultsListView.Items.Count >= MaxSearchResults)
    //                return;

    //            if (t.Name.Contains(_searchTextBox.Text) || t.Namespace.Contains(_searchTextBox.Text))
    //            {
    //                var newItem = new ListViewItem(new string[]
    //                {
    //                        t.ToString(),
    //                        TreeLogic.AsFileSize(t.Size),
    //                        TreeLogic.AsFileSize(t.AggregateSize)
    //                });

    //                newItem.Tag = t;

    //                _searchResultsListView.Items.Add(newItem);
    //            }

    //            AddTypes(t.GetNestedTypes());

    //            if (_searchComboBox.SelectedIndex is 0 or 2)
    //                AddMembers(t.GetMembers());
    //        }
    //    }

    //    static void AddMembers(Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> members)
    //    {
    //        foreach (var m in members)
    //        {
    //            if (_searchResultsListView.Items.Count >= MaxSearchResults)
    //                return;

    //            if (m.Name.Contains(_searchTextBox.Text))
    //            {
    //                var newItem = new ListViewItem(new string[]
    //                {
    //                    m.ToQualifiedString(),
    //                    TreeLogic.AsFileSize(m.Size),
    //                    TreeLogic.AsFileSize(m.AggregateSize)
    //                });

    //                newItem.Tag = m;

    //                _searchResultsListView.Items.Add(newItem);
    //            }
    //        }
    //    }

    //    _searchResultsListView.EndUpdate();
    //}

    class SearchResultComparer : IComparer
    {
        public bool InvertSort { get; set; }

        public int SortColumn { get; set; }

        public int Compare(object? x, object? y)
        {
            var i1 = (ListViewItem)x!;
            var i2 = (ListViewItem)y!;

            int result;
            if (SortColumn == 0)
            {
                string? s1 = i1.Tag.ToString();
                string? s2 = i2.Tag.ToString();
                result = string.Compare(s1, s2);
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
}

internal class ListViewItem
{
    public object Tag { get; set; } = default!;
}