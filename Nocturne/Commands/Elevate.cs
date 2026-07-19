using System.Diagnostics;
using System.Security.Principal;
using Nocturne.Utils;

namespace Nocturne.Commands
{
    public class Elevate : SlashCommand
    {
        public override string Description
        {
            get
            {
                return "Restarts the shell with elevated privileges.";
            }
        }

        protected override void Execute(string[] args, Shell shell)
        {
            string processPath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot determine the current process path.");
            bool hostedByDotnet = string.Equals(
                Path.GetFileNameWithoutExtension(processPath),
                "dotnet",
                StringComparison.OrdinalIgnoreCase);

            ProcessStartInfo startInfo;

            if (OperatingSystem.IsWindows())
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    Console.WriteLine(Colors.BrightYellow(
                        "The shell is already running with administrator privileges."));
                    return;
                }

                startInfo = new ProcessStartInfo
                {
                    FileName = processPath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = shell.Cwd
                };
            }
            else
            {
                if (Environment.UserName == "root")
                {
                    Console.WriteLine(Colors.BrightYellow(
                        "The shell is already running with elevated privileges."));
                    return;
                }

                startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    UseShellExecute = false,
                    WorkingDirectory = shell.Cwd
                };
                startInfo.ArgumentList.Add(processPath);
            }

            if (hostedByDotnet)
            {
                startInfo.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
            }

            startInfo.ArgumentList.Add(shell.Cwd);

            _ = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the elevated shell.");

            Environment.Exit(0);
        }
    }
}
