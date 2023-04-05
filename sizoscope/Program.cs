using Avalonia;
using System.Runtime.InteropServices;

namespace sizoscope
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .With(new Win32PlatformOptions()
                {
                    UseWindowsUIComposition = true,
                    CompositionBackdropCornerRadius = 8f
                });
        //[STAThread]
        //static void Main(string[] args)
        //{
        //    // To customize application configuration such as set high DPI settings or default font,
        //    // see https://aka.ms/applicationconfiguration.
        //    ApplicationConfiguration.Initialize();

        //    MainForm form;
        //    if (args.Length > 0 && File.Exists(args[0]))
        //        form = new MainForm(args[0]);
        //    else
        //        form = new MainForm();

        //    Application.Run(form);
        //}
    }
}