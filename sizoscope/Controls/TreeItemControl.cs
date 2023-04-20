using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace sizoscope.Controls;

[PseudoClasses(pcAssembly, pcNamespace, pcClass, pcMethod, pcInstantiation)]
public class TreeItemControl : TemplatedControl
{
    private const string pcAssembly = ":assembly";
    private const string pcNamespace = ":namespace";
    private const string pcClass = ":class";
    private const string pcMethod = ":method";
    private const string pcInstantiation = ":instantiation";

    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<TreeItemControl, string?>(nameof(Text));
    public static readonly StyledProperty<NodeType> TypeProperty = AvaloniaProperty.Register<TreeItemControl, NodeType>(nameof(Type));
    public static readonly StyledProperty<IImage?> ImageProperty = AvaloniaProperty.Register<TreeItemControl, IImage?>(nameof(Image));

    private IImage? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public NodeType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    private void SetPseudoClasses(NodeType type)
    {
        PseudoClasses.Set(pcAssembly, type is NodeType.Assembly);
        PseudoClasses.Set(pcNamespace, type is NodeType.Namespace);
        PseudoClasses.Set(pcClass, type is NodeType.Class);
        PseudoClasses.Set(pcMethod, type is NodeType.Method);
        PseudoClasses.Set(pcInstantiation, type is NodeType.Instantiation);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        SetPseudoClasses(Type);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TypeProperty)
        {
            SetPseudoClasses(change.GetNewValue<NodeType>());
            InvalidateVisual();
        }
    }
}
