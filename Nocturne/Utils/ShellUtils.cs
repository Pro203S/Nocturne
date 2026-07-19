using Nocturne.Commands;
using System.Text;

namespace Nocturne.Utils
{
    public class ShellUtils
    {
        private static readonly List<string> InputHistory = [];

        public static void SetTitle(string title)
        {
            try
            {
                Console.Title = title;
            }
            catch
            {
                // A terminal that does not support titles must not interrupt the shell.
            }
        }

        public static string GetMultiLineInput()
        {
            StringBuilder command = new();

            while (true)
            {
                const string prompt = "        ";
                Console.Write(prompt);
                string? input = ReadLine(prompt);

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

        public static bool HasLineContinuation(string input)
        {
            int caretCount = 0;

            for (int i = input.Length - 1; i >= 0 && input[i] == '^'; i--)
            {
                caretCount++;
            }

            return (caretCount & 1) != 0;
        }

        public static string? GetInput(string cwd)
        {
            string userName = Environment.UserName;
            string computerName = Environment.MachineName;

            string themeName = Environment.GetEnvironmentVariable("NOCTURNE_THEME") ?? "nocturne";

            if (themeName == "bash")
            {
                string bashPrompt = string.Format("{0}{1}{2}{3} ",
                    Colors.Bold(Colors.Green(string.Format("{0}@{1}", userName, computerName))),
                    Colors.Bold(Colors.BrightWhite(":")),
                    Colors.Bold(Colors.Blue(cwd)),
                    Colors.BrightWhite("$")
                );
                Console.Write(bashPrompt);
                return ReadLine(bashPrompt, cwd);
            }

            if (themeName == "cmd")
            {
                string cmdPrompt = string.Format("{0}> ", cwd);
                Console.Write(cmdPrompt);
                return ReadLine(cmdPrompt, cwd);
            }

            if (themeName == "nocturne")
            {
                Console.Write("{0}{1}{2}{3}{4}{5}{6}{4}\n",
                    Colors.Blue("┌─["),
                    Colors.Bold(Colors.BrightGreen(userName)),
                    Colors.BrightBlue("@"),
                    Colors.Bold(Colors.BrightBlue(computerName)),
                    Colors.Blue("]"),
                    Colors.Blue("──["),
                    Colors.Bold(Colors.BrightWhite(cwd))
                );
                string nocturnePrompt = string.Format("{0}{1}{2} ",
                    Colors.Blue("└─["),
                    Colors.Bold(Colors.BrightYellow("$")),
                    Colors.Blue("]")
                );
                Console.Write(nocturnePrompt);
                return ReadLine(nocturnePrompt, cwd);
            }

            Console.WriteLine(Colors.BrightYellow("The theme \"" + themeName + "\" is not available.\nPlease choose bash, cmd, or nocturne."));
            string errorPrompt = string.Format("{0}> ", cwd);
            Console.Write(errorPrompt);
            return ReadLine(errorPrompt, cwd);
        }

        public static string? ReadLine()
        {
            return ReadLine(string.Empty);
        }

        private static string? ReadLine(string prompt, string? cwd = null)
        {
            if (Console.IsInputRedirected)
            {
                return Console.ReadLine();
            }

            bool treatControlCAsInput = Console.TreatControlCAsInput;

            try
            {
                Console.TreatControlCAsInput = true;
                return ReadInteractiveLine(prompt, cwd);
            }
            finally
            {
                Console.TreatControlCAsInput = treatControlCAsInput;
            }
        }

        private static string ReadInteractiveLine(string prompt, string? cwd)
        {
            StringBuilder input = new();
            Stack<(string Text, int Cursor)> undo = new();
            Stack<(string Text, int Cursor)> redo = new();
            int historyIndex = InputHistory.Count;
            int cursorPosition = 0;
            string draft = "";
            PathCompletionState? pathCompletion = null;

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                bool control = (key.Modifiers & ConsoleModifiers.Control) != 0;
                bool shift = (key.Modifiers & ConsoleModifiers.Shift) != 0;

                if (key.Key != ConsoleKey.Tab)
                {
                    pathCompletion = null;
                }

                if (control && key.Key == ConsoleKey.C)
                {
                    Console.Write("\r\x1b[2K");
                    Console.WriteLine();
                    throw new OperationCanceledException();
                }

                if (key.Key == ConsoleKey.Enter)
                {
                    string value = input.ToString();

                    if (cwd is not null &&
                        value.Length > 0 &&
                        (InputHistory.Count == 0 || InputHistory[^1] != value))
                    {
                        InputHistory.Add(value);
                    }

                    RenderInput(prompt, input, input.Length, false);
                    Console.WriteLine();
                    return value;
                }

                if (cwd is not null &&
                    (key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow))
                {
                    int newIndex = key.Key == ConsoleKey.UpArrow
                        ? Math.Max(0, historyIndex - 1)
                        : Math.Min(InputHistory.Count, historyIndex + 1);

                    if (newIndex != historyIndex)
                    {
                        if (historyIndex == InputHistory.Count)
                        {
                            draft = input.ToString();
                        }

                        undo.Push((input.ToString(), cursorPosition));
                        redo.Clear();
                        historyIndex = newIndex;
                        input.Clear().Append(
                            historyIndex == InputHistory.Count ? draft : InputHistory[historyIndex]);
                        cursorPosition = input.Length;
                        RenderInput(prompt, input, cursorPosition, true);
                    }
                    continue;
                }

                if (control && key.Key == ConsoleKey.Z)
                {
                    Stack<(string Text, int Cursor)> source = shift ? redo : undo;
                    Stack<(string Text, int Cursor)> destination = shift ? undo : redo;

                    if (source.TryPop(out var state))
                    {
                        destination.Push((input.ToString(), cursorPosition));
                        input.Clear().Append(state.Text);
                        cursorPosition = state.Cursor;
                        RenderInput(prompt, input, cursorPosition, cwd is not null);
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.LeftArrow)
                {
                    if (cursorPosition > 0)
                    {
                        cursorPosition--;
                        RenderInput(prompt, input, cursorPosition, cwd is not null);
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.RightArrow)
                {
                    if (cursorPosition < input.Length)
                    {
                        cursorPosition++;
                        RenderInput(prompt, input, cursorPosition, cwd is not null);
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.Home)
                {
                    cursorPosition = 0;
                    RenderInput(prompt, input, cursorPosition, cwd is not null);
                    continue;
                }

                if (key.Key == ConsoleKey.End)
                {
                    cursorPosition = input.Length;
                    RenderInput(prompt, input, cursorPosition, cwd is not null);
                    continue;
                }

                if (key.Key == ConsoleKey.Tab && cwd is not null)
                {
                    string previous = input.ToString();
                    int previousCursor = cursorPosition;
                    bool completed = false;

                    if (pathCompletion is not null)
                    {
                        pathCompletion.Move(shift);
                        pathCompletion.Apply(input);
                        completed = true;
                    }
                    else
                    {
                        string? slashCommand = cursorPosition == input.Length
                            ? FindSlashCommand(input)
                            : null;

                        if (slashCommand is not null)
                        {
                            completed = TryCompleteSlashCommand(input, slashCommand);
                        }
                        else
                        {
                            pathCompletion = CreatePathCompletion(
                                input,
                                cursorPosition,
                                cwd,
                                shift);

                            if (pathCompletion is not null)
                            {
                                pathCompletion.Apply(input);
                                completed = true;
                            }
                            else
                            {
                                Console.Write('\a');
                            }
                        }
                    }

                    if (completed)
                    {
                        undo.Push((previous, previousCursor));
                        redo.Clear();
                        cursorPosition = input.Length;
                    }

                    RenderInput(prompt, input, cursorPosition, true);
                    continue;
                }

                if (key.Key == ConsoleKey.Backspace && cursorPosition > 0)
                {
                    undo.Push((input.ToString(), cursorPosition));
                    redo.Clear();
                    input.Remove(cursorPosition - 1, 1);
                    cursorPosition--;
                    RenderInput(prompt, input, cursorPosition, cwd is not null);
                    continue;
                }

                if (key.Key == ConsoleKey.Delete && cursorPosition < input.Length)
                {
                    undo.Push((input.ToString(), cursorPosition));
                    redo.Clear();
                    input.Remove(cursorPosition, 1);
                    RenderInput(prompt, input, cursorPosition, cwd is not null);
                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    undo.Push((input.ToString(), cursorPosition));
                    redo.Clear();
                    input.Insert(cursorPosition, key.KeyChar);
                    cursorPosition++;
                    RenderInput(prompt, input, cursorPosition, cwd is not null);
                }
            }
        }

        private static void RenderInput(
            string prompt,
            StringBuilder input,
            int cursorPosition,
            bool showSuggestion)
        {
            Console.Write("\r{0}", prompt);
            Console.Write(input.ToString(0, cursorPosition));
            Console.Write("\x1b[s");
            Console.Write(input.ToString(cursorPosition, input.Length - cursorPosition));
            Console.Write("\x1b[K");

            if (showSuggestion && cursorPosition == input.Length)
            {
                string? slashCommand = FindSlashCommand(input);
                if (slashCommand is not null && slashCommand.Length != input.Length)
                {
                    string suggestion = slashCommand[input.Length..];
                    Console.Write(Colors.Gray(suggestion));
                }
            }

            Console.Write("\x1b[u");
        }

        private static string? FindSlashCommand(StringBuilder input)
        {
            string text = input.ToString();
            if (text.Length == 0 || text[0] != '/' || text.Any(char.IsWhiteSpace))
            {
                return null;
            }

            return SlashCommand.Commands.Keys
                .Concat(SlashCommand.ExtensionCommands.Keys)
                .FirstOrDefault(
                command => command.StartsWith(text, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryCompleteSlashCommand(StringBuilder input, string slashCommand)
        {
            if (slashCommand.Length == input.Length)
            {
                return false;
            }

            input.Clear().Append(slashCommand);
            return true;
        }

        private static PathCompletionState? CreatePathCompletion(
            StringBuilder input,
            int cursorPosition,
            string cwd,
            bool reverse)
        {
            string text = input.ToString(0, cursorPosition);
            int tokenStart = FindPathTokenStart(text);
            string rawToken = text[tokenStart..];
            bool containsQuotes = rawToken.Contains('"');
            string token = rawToken.Replace("\"", "");
            string expandedToken = Environment.ExpandEnvironmentVariables(token);
            string directoryPart = Path.GetDirectoryName(token) ?? "";
            string searchDirectoryPart = Path.GetDirectoryName(expandedToken) ?? "";
            string searchPattern = Path.GetFileName(expandedToken);

            if (searchPattern.IndexOfAny(['*', '?']) < 0)
            {
                searchPattern += '*';
            }

            string[] matches;
            try
            {
                string directory = Path.GetFullPath(
                    Path.Combine(cwd, searchDirectoryPart));
                bool directoriesOnly = UsesDirectoryCompletion(text[..tokenStart]);

                matches = Directory.EnumerateFileSystemEntries(directory, searchPattern)
                    .Where(path => !directoriesOnly || Directory.Exists(path))
                    .Select(path =>
                    {
                        string completion = Path.GetFileName(path);
                        string completedToken = directoryPart.Length == 0
                            ? completion
                            : Path.Combine(directoryPart, completion);
                        bool needsQuotes = containsQuotes ||
                            RequiresCompletionQuotes(completedToken);

                        return needsQuotes
                            ? $"\"{completedToken}\""
                            : completedToken;
                    })
                    .ToArray();
            }
            catch (Exception exception) when (
                exception is ArgumentException or
                DirectoryNotFoundException or
                IOException or
                NotSupportedException or
                UnauthorizedAccessException)
            {
                return null;
            }

            if (matches.Length == 0)
            {
                return null;
            }

            return new PathCompletionState(
                text[..tokenStart],
                matches,
                reverse ? matches.Length - 1 : 0);
        }

        private static int FindPathTokenStart(string text)
        {
            int tokenStart = 0;
            bool insideQuotes = false;

            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] == '"')
                {
                    insideQuotes = !insideQuotes;
                }
                else if (!insideQuotes && char.IsWhiteSpace(text[index]))
                {
                    tokenStart = index + 1;
                }
            }

            return tokenStart;
        }

        private static bool UsesDirectoryCompletion(string textBeforePath)
        {
            int commandStart = 0;
            bool insideQuotes = false;

            for (int index = 0; index < textBeforePath.Length; index++)
            {
                if (textBeforePath[index] == '"')
                {
                    insideQuotes = !insideQuotes;
                }
                else if (!insideQuotes &&
                    textBeforePath[index] is '&' or '|')
                {
                    commandStart = index + 1;
                }
            }

            string commandLine = textBeforePath[commandStart..]
                .TrimStart(' ', '\t', '(', '@');
            int separator = commandLine.IndexOfAny([' ', '\t']);
            string command = separator < 0
                ? commandLine
                : commandLine[..separator];

            return command.Equals("cd", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("chdir", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("md", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("mkdir", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("rd", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("rmdir", StringComparison.OrdinalIgnoreCase);
        }

        private static bool RequiresCompletionQuotes(string path)
        {
            const string specialCharacters = "&()[]{}^=;!'+,`~";
            return path.Any(character =>
                char.IsWhiteSpace(character) ||
                specialCharacters.Contains(character));
        }

        private sealed class PathCompletionState(
            string prefix,
            string[] matches,
            int selectedIndex)
        {
            private int selectedIndex = selectedIndex;

            public void Move(bool reverse)
            {
                selectedIndex = reverse
                    ? (selectedIndex - 1 + matches.Length) % matches.Length
                    : (selectedIndex + 1) % matches.Length;
            }

            public void Apply(StringBuilder input)
            {
                input.Clear()
                    .Append(prefix)
                    .Append(matches[selectedIndex]);
            }
        }
    }
}
