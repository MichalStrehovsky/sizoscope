using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

string exe = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sizoscope-cli.exe");

// Avoid accidental forkbomb
if (new FileInfo(exe).Length < 512 * 1024)
    return;

try
{
    var psi = new ProcessStartInfo(exe)
    {
        UseShellExecute = false,
    };
    foreach (var arg in args)
        psi.ArgumentList.Add(arg);

    var process = Process.Start(psi);
    process.WaitForExit();
    Environment.ExitCode = process.ExitCode;
}
catch (Win32Exception)
{
    Console.Error.WriteLine("Failed to launch sizoscope-cli. This tool requires Windows.");
}
