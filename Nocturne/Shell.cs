using Nocturne.Utils;

namespace Nocturne
{
    public class Shell
    {
        public required string Cwd { get; set; }

        public void Run()
        {
            try
            {
                string input = (ShellUtils.GetInput(Cwd) ?? "").Trim();
                if (string.IsNullOrEmpty(input)) return;

                if (ShellUtils.HasLineContinuation(input))
                {
                    Profile.Execute(input[..^1] + ShellUtils.GetMultiLineInput(), Cwd);
                    return;
                }

                #region 터미널 내부 명령어
                if (input == "exit")
                {
                    Environment.Exit(0);
                    return;
                }

                if (input.StartsWith("cd "))
                {
                    string path = input[3..];
                    if (Path.IsPathRooted(path))
                    {
                        if (!Directory.Exists(path))
                        {
                            throw new Exception("The system cannot find the path specified.");
                        }

                        Cwd = path;
                        return;
                    }

                    string targetPath = Path.GetFullPath(Path.Join(Cwd, path)).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    Cwd = targetPath;
                    return;
                }
                #endregion


                if (input[0] != '/')
                {
                    Profile.Execute(input, Cwd);
                    return;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(Colors.BrightRed(e.Message));
                return;
            }
        }

    }
}
