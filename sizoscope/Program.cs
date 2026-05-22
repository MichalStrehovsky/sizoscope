namespace sizoscope
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

#pragma warning disable WFO5001
            Application.SetColorMode(SystemColorMode.System);
#pragma warning restore

            Form form;
            if (args.Length >= 2 && File.Exists(args[0]) && File.Exists(args[1]))
                form = new DiffForm(args[0], args[1]);
            else if (args.Length >= 1 && File.Exists(args[0]))
                form = new MainForm(args[0]);
            else
                form = new MainForm();

            Application.Run(form);
        }
    }
}