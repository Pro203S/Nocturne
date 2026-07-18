namespace Nocturne.Terminal
{
    public static class ChangeDirectory
    {
        public static bool TryExecute(string input, string cwd, out string newCwd)
        {
            newCwd = cwd;

            if (input.Equals("cd", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(cwd);
                return true;
            }

            if (!input.StartsWith("cd ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string path = input[3..].Trim();

            if (path.StartsWith("/d ", StringComparison.OrdinalIgnoreCase))
            {
                path = path[3..].Trim();
            }

            newCwd = ResolvePath(path, cwd);
            return true;
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
