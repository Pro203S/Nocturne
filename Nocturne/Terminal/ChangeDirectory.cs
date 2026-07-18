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

            shell.Cwd = ResolvePath(path, shell.Cwd);
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

            return Path.TrimEndingDirectorySeparator(current);
        }
    }
}
