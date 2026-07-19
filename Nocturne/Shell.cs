using Nocturne.Commands;
using Nocturne.Terminal;
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

                if (TerminalCommand.TryExecute(input, this))
                {
                    return;
                }
                #endregion

                // 슬래시 커맨드 아닐때
                if (input[0] != '/')
                {
                    Profile.Execute(input, Cwd);
                    return;
                }

                SlashCommand.TryExecute(input, this);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(Colors.BrightRed(e.Message));
                return;
            }
        }

    }
}
