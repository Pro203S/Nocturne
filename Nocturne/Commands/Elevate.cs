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

            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            {
                Logger.Log("[SYSTEM] Elevation skipped; already running as administrator.");
                Console.WriteLine(Colors.BrightYellow(
                    "The shell is already running with administrator privileges."));
                return;
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = processPath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = shell.Cwd
            };

            if (hostedByDotnet)
            {
                startInfo.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
            }

            startInfo.ArgumentList.Add(shell.Cwd);

            Logger.Log("[SYSTEM] Requesting an elevated Nocturne process.");
            _ = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the elevated shell.");

            Environment.Exit(0);
        }
    }
}
