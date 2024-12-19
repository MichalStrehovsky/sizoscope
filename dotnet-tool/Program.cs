using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

string exe = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sizoscope.exe");

// Avoid accidental forkbomb
if (new FileInfo(exe).Length < 1024 * 1024)
    return;

if (!OperatingSystem.IsWindows())
{
    string[] newArgs = new string[args.Length + 1];
    newArgs[0] = exe;
    Array.Copy(args, 0, newArgs, 1, args.Length);
    exe = "wine";
    args = newArgs;
}

try
{
    Process.Start(exe, args);
}
catch (Win32Exception) when (!OperatingSystem.IsWindows())
{
    Console.WriteLine("Failed to launch sizoscope under wine. Make sure you have wine installed.");
    Console.WriteLine("On Ubuntu, `sudo apt install wine`, or follow https://www.winehq.org/.");
}
