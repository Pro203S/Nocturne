using System.Text;
using Nocturne.Utils;

namespace Nocturne
{
    public class Program
    {
        static string _path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        static void Main(string[] args)
        {
            _path = args.ElementAtOrDefault(0) ?? _path;

            Console.Clear();
            Console.OutputEncoding = Encoding.UTF8;

            Profile.Load();

            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("NOCTURNE_HELP_MSG")))
            {
                Console.Write(
                    "Welcome to {0} 🌙\n\nType {1} to experience special.\n\n",
                    Colors.Bold(Colors.BrightWhite("Nocturne shell")),
                    Colors.Bold(Colors.BrightYellow("/help"))
                );
            }

            for (; ; )
            {
                string? input = GetInput();
                if (string.IsNullOrEmpty(input)) continue;

                if (HasLineContinuation(input))
                {
                    Profile.Execute(input[..^1] + GetMultiLineInput());
                    continue;
                }

                if (input.Trim() == "exit")
                {
                    Environment.Exit(0);
                    return;
                }

                if (input[0] != '/')
                {
                    Profile.Execute(input);
                    continue;
                }
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

        static string? GetInput()
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
                Colors.Bold(Colors.BrightWhite(_path))
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
