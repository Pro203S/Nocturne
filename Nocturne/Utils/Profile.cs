using System.Diagnostics;

namespace Nocturne.Utils
{
    public static class Profile
    {
        public static readonly string DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static readonly string FilePath =
            Path.Combine(DirectoryPath, "nocturne.ns");

        public static void Load()
        {
            Directory.CreateDirectory(DirectoryPath);

            if (!File.Exists(FilePath))
            {
                File.WriteAllText(FilePath,
    @"# Nocturne Profile

# Decides show Nocturne help message.
set NOCTURNE_HELP_MSG=true
");
            }

            foreach (string line in File.ReadLines(FilePath))
            {
                Execute(line);
            }
        }

        public static void Execute(string line)
        {
            line = line.Trim();

            if (line.Length == 0 || line.StartsWith("#"))
            {
                return;
            }

            if (line.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
            {
                ReadOnlySpan<char> assignment = line.AsSpan(4).Trim();

                if (assignment.Length > 1 && assignment[0] == '"' && assignment[^1] == '"')
                {
                    assignment = assignment[1..^1];
                }

                int separator = assignment.IndexOf('=');

                if (separator > 0)
                {
                    Environment.SetEnvironmentVariable(
                        assignment[..separator].Trim().ToString(),
                        assignment[(separator + 1)..].ToString());
                    return;
                }
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = OperatingSystem.IsWindows()
                    ? Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe"
                    : "/bin/sh",
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(OperatingSystem.IsWindows() ? "/c" : "-c");
            startInfo.ArgumentList.Add(line);

            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the profile command.");
            process.WaitForExit();
        }
    }
}
