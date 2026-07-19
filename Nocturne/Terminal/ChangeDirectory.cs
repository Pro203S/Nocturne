using Nocturne.Utils;

namespace Nocturne.Terminal
{
    public sealed class ChangeDirectory : TerminalCommand
    {
        protected override void Execute(string arguments, Shell shell)
        {
            if (arguments.Length == 0)
            {
                Console.WriteLine(shell.Cwd);
                return;
            }

            string path = arguments;

            if (path.StartsWith("/d ", StringComparison.OrdinalIgnoreCase))
            {
                path = path[3..].Trim();
            }

            string previousDirectory = shell.Cwd;
            shell.Cwd = ResolvePath(path, shell.Cwd);
            Logger.Log(
                $"[SHELL] Changed directory from {previousDirectory} to {shell.Cwd}.");
        }

        private static string ResolvePath(string path, string cwd)
        {
            path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));

            if (path.Length == 0)
            {
                path = ".";
            }

            string fullPath = Path.GetFullPath(path, cwd);
            string root = Path.GetPathRoot(fullPath)
                ?? throw new DirectoryNotFoundException("The system cannot find the path specified.");
            string current = root;

            foreach (string part in fullPath[root.Length..].Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries))
            {
                DirectoryInfo directory = new(Path.Combine(current, part));

                if (!directory.Exists)
                {
                    throw new DirectoryNotFoundException("The system cannot find the path specified.");
                }

                current = directory.ResolveLinkTarget(returnFinalTarget: true)?.FullName
                    ?? directory.FullName;
            }

            if (!Directory.Exists(current))
            {
                throw new DirectoryNotFoundException("The system cannot find the path specified.");
            }

            return Path.TrimEndingDirectorySeparator(MatchPathCase(current));
        }

        private static string MatchPathCase(string path)
        {
            string root = Path.GetPathRoot(path)!;

            if (root.Length >= 2 && root[1] == Path.VolumeSeparatorChar)
            {
                root = char.ToUpperInvariant(root[0]) + root[1..];
            }

            string current = root;

            foreach (string part in path[root.Length..].Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries))
            {
                current = Directory.EnumerateFileSystemEntries(current, part)
                    .FirstOrDefault(candidate => Path.GetFileName(candidate)
                        .Equals(part, StringComparison.OrdinalIgnoreCase))
                    ?? Path.Combine(current, part);
            }

            return current;
        }
    }
}
