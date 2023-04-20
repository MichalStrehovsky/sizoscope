using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Media;
using FluentAvalonia.UI.Windowing;
using sizoscope.ViewModels;
using System.Runtime.InteropServices;

namespace sizoscope
{
    public partial class DiffWindow : AppWindow
    {
        private readonly DiffWindowViewModel _viewModel;
        public DiffWindow(MstatData baseline, MstatData compare)
        {
            InitializeComponent();
            TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
            Application.Current!.ActualThemeVariantChanged += ApplicationActualThemeVariantChanged;
            _viewModel = new(baseline, compare);
            DataContext = _viewModel;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            Application.Current!.ActualThemeVariantChanged -= ApplicationActualThemeVariantChanged;
        }

        private void ApplicationActualThemeVariantChanged(object? sender, EventArgs e)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (IsWindows11 && ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
                {
                    TryEnableMicaEffect();
                }
                else if (ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
                {
                    SetValue(BackgroundProperty, AvaloniaProperty.UnsetValue);
                }
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            var thm = ActualThemeVariant;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (IsWindows11 && thm != FluentAvaloniaTheme.HighContrastTheme)
                {
                    TransparencyBackgroundFallback = Brushes.Transparent;
                    TransparencyLevelHint = WindowTransparencyLevel.Mica;

                    TryEnableMicaEffect();
                }
            }
        }

        private void TryEnableMicaEffect()
        {
            if (ActualThemeVariant == ThemeVariant.Dark)
            {
                var color = this.TryFindResource("SolidBackgroundFillColorBase",
                    ThemeVariant.Dark, out var value) ? (Color2)(Color)value! : new Color2(32, 32, 32);

                color = color.LightenPercent(-0.8f);

                Background = new ImmutableSolidColorBrush(color, 0.78);
            }
            else if (ActualThemeVariant == ThemeVariant.Light)
            {
                // Similar effect here
                var color = this.TryFindResource("SolidBackgroundFillColorBase",
                    ThemeVariant.Light, out var value) ? (Color2)(Color)value! : new Color2(243, 243, 243);

                color = color.LightenPercent(0.5f);

                Background = new ImmutableSolidColorBrush(color, 0.9);
            }
        }

        private void Tree_DoubleTapped(object? sender, TappedEventArgs args)
        {

        }
    }
}
