

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

EzSettings settings = EzSettings.Load();






Console.WriteLine("Welcome to EZ!");
while (true)
{
    Console.Write("EZ> ");
    var cmmd = Console.ReadLine();

    if (cmmd == "help" | cmmd == "ez help")
    {
        Console.WriteLine("| EZ Terminal");
        Console.WriteLine("| Commands: ");
        Console.WriteLine("| ez push ");
        Console.WriteLine("| ez status");
        Console.WriteLine("| ez pull");
        Console.WriteLine("| ez help");
        Console.WriteLine("| ez new");
        Console.WriteLine("| ez read");
        Console.WriteLine("| ez settings");
        Console.WriteLine("| exit");
        Console.WriteLine("! Both commands with EZ or without work!");
        Console.WriteLine("  ");
    }
    else if (cmmd == "ez exit" | cmmd == "exit")
    {
        return;
    }
    else if (cmmd == "ez status" | cmmd == "status")
    {
        Console.WriteLine("Getting status...");
        GitManager("status");
    }
    else if (cmmd == "ez pull" | cmmd == "pull")
    {
        Console.WriteLine("Pulling...");
        GitManager("pull");
        Console.WriteLine("Done!");
    }
    else if (cmmd == "ez push" | cmmd == "push")

    {
        GitManager("add .");
        GitManager("Commit -a");
        GitManager("push");
    }
    else if (cmmd == "ez settings" | cmmd == "settings")
    {
        settings.ShowInteractive();
    }
    else
    {

        Console.WriteLine("Invalid command!");
    } 


    if (args.Length > 0)
    {
        string command = string.Join(" ", args);
        ExecuteCommand(command);
        return;
    }

    while (true)
    {
        Console.Write("EZ> ");
        string input = Console.ReadLine() ?? "";

        if (input.ToLower() == "exit") break;

        ExecuteCommand(input);
    }

    void GitManager(string command, string workingDir = "")
    {
        if (!CanUseGit())
        {
            Console.WriteLine("EZ is required in path for quick access and git commands");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = command,
            WorkingDirectory = string.IsNullOrEmpty(workingDir) ? Directory.GetCurrentDirectory() : workingDir,
            UseShellExecute = false
        };

        using Process? process = Process.Start(startInfo);
        process?.WaitForExit();
    }

    bool CanUseGit()
    {
        return settings.EasyPathEnabled &&
               IsEzInPath() &&
               IsCommandPromptLaunchOutsideEzFolder();
    }

    bool IsEzInPath()
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmedDirectory = directory.Trim().Trim('"');
            if (File.Exists(Path.Combine(trimmedDirectory, "EZ.exe")) ||
                File.Exists(Path.Combine(trimmedDirectory, "EZ")))
            {
                return true;
            }
        }

        return false;
    }

    bool IsCommandPromptLaunchOutsideEzFolder()
    {
        string? processPath = Environment.ProcessPath;
        string? processDirectory = processPath is null ? null : Path.GetDirectoryName(processPath);
        string currentDirectory = Path.GetFullPath(Directory.GetCurrentDirectory());

        if (string.IsNullOrEmpty(processDirectory) ||
            string.Equals(currentDirectory, Path.GetFullPath(processDirectory), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        using Process currentProcess = Process.GetCurrentProcess();
        using Process? parentProcess = ProcessHelper.GetParentProcess(currentProcess.Id);
        if (parentProcess is null)
        {
            return false;
        }

        string parentName = parentProcess.ProcessName;
        return parentName.Equals("cmd", StringComparison.OrdinalIgnoreCase) ||
               parentName.Equals("powershell", StringComparison.OrdinalIgnoreCase) ||
               parentName.Equals("pwsh", StringComparison.OrdinalIgnoreCase);
    }

    void ExecuteCommand(string input)
    {
        if (input is "push" or "ez push")
        {
            GitManager("add .");
            GitManager("commit -m \"EZ Auto-Push\"");
            GitManager("push");
        }
        else if (input is "settings" or "ez settings")
        {
            settings.ShowInteractive();
        }
    }
}


public class EzPluginFile
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }
    
    public class EzCommand
    {
        public string Name { get; set; } = "";
    }
    
    
    public class GitManager;

    internal static class ProcessHelper
    {
        private const uint SnapshotFlags = 0x00000002;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ProcessEntry
        {
            public uint Size;
            public uint Usage;
            public uint ProcessId;
            private IntPtr DefaultHeapId;
            private uint ModuleId;
            private uint Threads;
            private uint ParentProcessId;
            private int Priority;
            private uint Flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string ExecutableFile;

            public uint GetParentProcessId() => ParentProcessId;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry entry);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry entry);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        public static Process? GetParentProcess(int processId)
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            IntPtr snapshot = CreateToolhelp32Snapshot(SnapshotFlags, 0);
            if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
            {
                return null;
            }

            try
            {
                ProcessEntry entry = new() { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
                if (Process32First(snapshot, ref entry))
                {
                    do
                    {
                        if (entry.ProcessId == processId)
                        {
                            uint parentId = entry.GetParentProcessId();
                            return parentId == 0 ? null : Process.GetProcessById((int)parentId);
                        }
                    } while (Process32Next(snapshot, ref entry));
                }
            }
            catch (ArgumentException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
            finally
            {
                CloseHandle(snapshot);
            }

            return null;
        }
    }
