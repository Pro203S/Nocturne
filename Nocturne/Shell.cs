using System.Text;
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
                string input = (GetInput() ?? "").Trim();
                if (string.IsNullOrEmpty(input)) return;

                if (HasLineContinuation(input))
                {
                    Profile.Execute(input[..^1] + GetMultiLineInput(), Cwd);
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
                    Console.Write(Cwd + " " + path);
                    if (Path.IsPathRooted(input))
                    {
                        if (!Directory.Exists(path))
                        {
                            throw new Exception("The system cannot find the path specified.");
                        }

                        Cwd = path;
                        return;
                    }

                    string targetPath = Path.Join(Cwd, path);

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

        static string GetMultiLineInput()
        {
            StringBuilder command = new();

            while (true)
            {
                Console.Write("        ");
                string? input = Console.ReadLine();

                if (input is null)
                {
                    return command.ToString();
                }

                if (HasLineContinuation(input))
                {
                    command.Append(input.AsSpan(0, input.Length - 1));
                    continue;
                }

                return command.Append(input).ToString();
            }
        }

        static bool HasLineContinuation(string input)
        {
            int caretCount = 0;

            for (int i = input.Length - 1; i >= 0 && input[i] == '^'; i--)
            {
                caretCount++;
            }

            return (caretCount & 1) != 0;
        }

        string? GetInput()
        {
            string userName = Environment.UserName;
            string computerName = Environment.MachineName;

            Console.Write("{0}{1}{2}{3}{4}{5}{6}{4}\n",
                Colors.Blue("┌─["),
                Colors.Bold(Colors.BrightGreen(userName)),
                Colors.BrightBlue("@"),
                Colors.Bold(Colors.BrightBlue(computerName)),
                Colors.Blue("]"),
                Colors.Blue("──["),
                Colors.Bold(Colors.BrightWhite(Cwd))
            );
            Console.Write("{0}{1}{2} ",
                Colors.Blue("└─["),
                Colors.Bold(Colors.BrightYellow("$")),
                Colors.Blue("]")
            );
            return Console.ReadLine();
        }
    }
}