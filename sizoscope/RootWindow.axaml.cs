using sizoscope.ViewModels;

namespace sizoscope
{
    public partial class RootWindow : FluentAppWindow
    {
        private readonly RootWindowViewModel _viewModel;
        public RootWindow(MstatData.Node node)
        {
            InitializeComponent();
            _viewModel = new RootWindowViewModel(node);
            DataContext = _viewModel;
        }
    }
}
