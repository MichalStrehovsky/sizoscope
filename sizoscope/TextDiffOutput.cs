using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace sizoscope
{
    /// <summary>
    /// Prints a text-based diff summary to the console.
    /// Used for --text mode to avoid launching the GUI.
    /// </summary>
    internal static partial class TextDiffOutput
    {
        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AttachConsole(int dwProcessId);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AllocConsole();

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial IntPtr GetStdHandle(int nStdHandle);

        private const int ATTACH_PARENT_PROCESS = -1;
        private const int STD_OUTPUT_HANDLE = -11;

        /// <summary>
        /// Sets up console output for a WinExe process.
        /// Tries to use inherited stdout handle first (works with piping/redirection),
        /// then attaches to parent console, and finally allocates a new console if needed.
        /// </summary>
        public static void SetupConsoleOutput()
        {
            // Check if we already have a valid stdout handle (e.g. output was redirected by parent)
            IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (handle != IntPtr.Zero && handle != new IntPtr(-1))
            {
                try
                {
                    var stream = new FileStream(new SafeFileHandle(handle, ownsHandle: false), FileAccess.Write);
                    Console.SetOut(new StreamWriter(stream) { AutoFlush = true });
                    return;
                }
                catch { }
            }

            // Try to attach to parent process console
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
            {
                // Last resort: create a new console window
                AllocConsole();
            }

            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }

        /// <summary>
        /// Prints a summary of the diff between two mstat files to the console.
        /// </summary>
        public static void Print(MstatData leftDiff, MstatData rightDiff, int totalDiffSize)
        {
            Print(Console.Out, leftDiff, rightDiff, totalDiffSize);
        }

        /// <summary>
        /// Prints a summary of the diff between two mstat files to the given writer.
        /// </summary>
        public static void Print(TextWriter writer, MstatData leftDiff, MstatData rightDiff, int totalDiffSize)
        {
            writer.WriteLine();
            writer.WriteLine($"Total accounted size difference: {TreeLogic.AsFileSize(totalDiffSize)}");
            writer.WriteLine();

            // Collect assemblies from the right diff (new/grown items)
            var grown = new List<(string Name, int Size)>();
            foreach (var asm in rightDiff.GetScopes())
            {
                if (asm.Name == "System.Private.CompilerGenerated")
                    continue;
                if (asm.AggregateSize == 0)
                    continue;
                grown.Add((asm.Name, asm.AggregateSize));
            }

            // Collect assemblies from the left diff (removed/shrunk items)
            var shrunk = new List<(string Name, int Size)>();
            foreach (var asm in leftDiff.GetScopes())
            {
                if (asm.Name == "System.Private.CompilerGenerated")
                    continue;
                if (asm.AggregateSize == 0)
                    continue;
                shrunk.Add((asm.Name, asm.AggregateSize));
            }

            // Sort by absolute size descending
            grown.Sort((a, b) => b.Size.CompareTo(a.Size));
            shrunk.Sort((a, b) => b.Size.CompareTo(a.Size));

            if (grown.Count > 0)
            {
                writer.WriteLine("=== New / Grown ===");
                foreach (var (name, size) in grown)
                {
                    writer.WriteLine($"  +{TreeLogic.AsFileSize(size),-12} {name}");
                }
                writer.WriteLine();
            }

            if (shrunk.Count > 0)
            {
                writer.WriteLine("=== Removed / Shrunk ===");
                foreach (var (name, size) in shrunk)
                {
                    writer.WriteLine($"  -{TreeLogic.AsFileSize(size),-12} {name}");
                }
                writer.WriteLine();
            }

            // Also show frozen objects and blobs if they contribute
            if (rightDiff.UnownedFrozenObjectSize > 0)
                writer.WriteLine($"  New frozen objects: +{TreeLogic.AsFileSize(rightDiff.UnownedFrozenObjectSize)}");
            if (leftDiff.UnownedFrozenObjectSize > 0)
                writer.WriteLine($"  Removed frozen objects: -{TreeLogic.AsFileSize(leftDiff.UnownedFrozenObjectSize)}");
            if (rightDiff.BlobsSize > 0)
                writer.WriteLine($"  New blobs: +{TreeLogic.AsFileSize(rightDiff.BlobsSize)}");
            if (leftDiff.BlobsSize > 0)
                writer.WriteLine($"  Removed blobs: -{TreeLogic.AsFileSize(leftDiff.BlobsSize)}");
        }
    }
}
