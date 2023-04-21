using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using sizoscope.ViewModels;
using static MstatData;

namespace sizoscope
{
    public partial class DiffWindow : FluentAppWindow
    {
        private readonly DiffWindowViewModel _viewModel;
        public DiffWindow(MstatData baseline, MstatData compare)
        {
            InitializeComponent();
            _viewModel = new(baseline, compare);
            DataContext = _viewModel;
        }

        private async void Tree_DoubleTapped(object? sender, TappedEventArgs args)
        {
            if (sender is not TreeView treeView || 
                treeView.SelectedItem is not TreeNode tn ||
                treeView.Tag is not MstatData currentData) return;

            int? id = tn.Tag switch
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
                    var dialog = new ContentDialog
                    {
                        CloseButtonText = "OK",
                        Title = "Error",
                        Content = "Dependency graph information is only available in .NET 8 Preview 4 or later."
                    };
                    await dialog.ShowAsync();
                    return;
                }

                var node = currentData.GetNodeForId(id.Value);
                if (node == null)
                {
                    var dialog = new ContentDialog
                    {
                        CloseButtonText = "OK",
                        Title = "Error",
                        Content = "Unable to load dependency graph. Was IlcGenerateDgmlLog=true specified?"
                    };
                    await dialog.ShowAsync();
                    return;
                }

                await new RootWindow(node).ShowDialog(this);
            }
        }
    }
}
