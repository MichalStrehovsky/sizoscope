using System.Runtime.InteropServices;

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
            // Separate flags from file paths
            bool textMode = false;
            string? outputFile = null;
            var filePaths = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--text", StringComparison.OrdinalIgnoreCase))
                    textMode = true;
                else if (string.Equals(args[i], "--output", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    outputFile = args[++i];
                else
                    filePaths.Add(args[i]);
            }

            // Two files = diff mode
            if (filePaths.Count == 2 && File.Exists(filePaths[0]) && File.Exists(filePaths[1]))
            {
                if (textMode)
                {
                    // --text mode: do all work synchronously, print summary, and exit
                    using var resolvedLeft = ResolvedFile.Open(filePaths[0]);
                    using var resolvedRight = ResolvedFile.Open(filePaths[1]);

                    MstatData left = MstatData.Read(resolvedLeft.MstatPath, loadDgmlAsync: false, skipDgml: true);
                    MstatData right = MstatData.Read(resolvedRight.MstatPath, loadDgmlAsync: false, skipDgml: true);

                    (MstatData leftDiff, MstatData rightDiff) = MstatData.Diff(left, right);
                    int diffSize = right.Size - left.Size;

                    if (outputFile != null)
                    {
                        using var writer = new StreamWriter(outputFile);
                        TextDiffOutput.Print(writer, leftDiff, rightDiff, diffSize);
                    }
                    else
                    {
                        TextDiffOutput.SetupConsoleOutput();
                        TextDiffOutput.Print(leftDiff, rightDiff, diffSize);
                    }
                    return;
                }

                // GUI diff mode: show DiffForm immediately, load data in background
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