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
            // Collect file paths from args (ignore unknown flags gracefully)
            var filePaths = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-"))
                    filePaths.Add(args[i]);
            }

            // Two files = GUI diff mode
            if (filePaths.Count == 2 && File.Exists(filePaths[0]) && File.Exists(filePaths[1]))
            {
                ApplicationConfiguration.Initialize();

#pragma warning disable WFO5001
                Application.SetColorMode(SystemColorMode.System);
#pragma warning restore

                Application.Run(new DiffForm(filePaths[0], filePaths[1]));
                return;
            }

            // Normal GUI mode
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

#pragma warning disable WFO5001
            Application.SetColorMode(SystemColorMode.System);
#pragma warning restore

            MainForm form;
            if (filePaths.Count > 0 && File.Exists(filePaths[0]))
                form = new MainForm(filePaths[0]);
            else
                form = new MainForm();

            Application.Run(form);
        }
    }
}