using System.Text;
using Nocturne.Utils;

namespace Nocturne.Commands
{
    public class Editor : SlashCommand
    {
        private const int TabWidth = 4;

        public override string Description
        {
            get
            {
                return "Create or edit a text file.";
            }
        }

        protected override void Execute(string[] args, Shell shell)
        {
            string requestedPath = string.Join(' ', args).Trim();
            if (requestedPath.Length >= 2 &&
                requestedPath[0] == '"' &&
                requestedPath[^1] == '"')
            {
                requestedPath = requestedPath[1..^1];
            }

            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                Console.WriteLine(Colors.BrightYellow("Usage: /editor <file>"));
                return;
            }

            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                throw new InvalidOperationException("The editor requires an interactive console.");
            }

            string filePath = Path.GetFullPath(requestedPath, shell.Cwd);
            if (Directory.Exists(filePath))
            {
                throw new IOException($"\"{filePath}\" is a directory.");
            }

            string? directory = Path.GetDirectoryName(filePath);
            if (directory is null || !Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory not found: {directory}");
            }

            Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            string newLine = Environment.NewLine;
            string contents = "";
            bool existingFile = File.Exists(filePath);

            if (existingFile)
            {
                using StreamReader reader = new(
                    filePath,
                    encoding,
                    detectEncodingFromByteOrderMarks: true);
                contents = reader.ReadToEnd();
                encoding = reader.CurrentEncoding;
                newLine = DetectNewLine(contents);
            }

            string[] initialLines = contents
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');
            string displayPath = Path.GetRelativePath(shell.Cwd, filePath);
            Logger.Log(
                $"[EDITOR] Opening {(existingFile ? "existing" : "new")} file {filePath}.");
            string[]? editedLines = OpenEditor(initialLines, displayPath);

            if (editedLines is null)
            {
                Logger.Log($"[EDITOR] Canceled editing {filePath}.");
                Console.WriteLine(Colors.Dim("Editing canceled."));
                return;
            }

            File.WriteAllText(filePath, string.Join(newLine, editedLines), encoding);
            Logger.Log(
                $"[EDITOR] Saved {filePath} ({editedLines.Length} lines).");
            Console.WriteLine(Colors.BrightGreen($"Saved {displayPath}"));
        }

        private static string[]? OpenEditor(string[] initialLines, string displayPath)
        {
            List<string> lines = initialLines.Length == 0 ? [""] : [.. initialLines];
            int line = 0;
            int column = 0;
            int verticalOffset = 0;
            int horizontalOffset = 0;
            bool dirty = false;
            bool treatControlCAsInput = Console.TreatControlCAsInput;

            try
            {
                Console.TreatControlCAsInput = true;
                Console.Write("\x1b[?1049h\x1b[?25l");

                while (true)
                {
                    Render(
                        lines,
                        displayPath,
                        line,
                        column,
                        dirty,
                        ref verticalOffset,
                        ref horizontalOffset);

                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                    bool control = key.Modifiers.HasFlag(ConsoleModifiers.Control);

                    if (key.Key == ConsoleKey.F2)
                    {
                        return [.. lines];
                    }

                    if (key.Key == ConsoleKey.Escape ||
                        key.KeyChar == '\u001b' ||
                        control && key.Key == ConsoleKey.C ||
                        key.KeyChar == '\u0003')
                    {
                        return null;
                    }

                    if (control && key.Key == ConsoleKey.Home)
                    {
                        line = 0;
                        column = 0;
                        continue;
                    }

                    if (control && key.Key == ConsoleKey.End)
                    {
                        line = lines.Count - 1;
                        column = lines[line].Length;
                        continue;
                    }

                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            if (line > 0)
                            {
                                line--;
                                column = Math.Min(column, lines[line].Length);
                            }
                            continue;

                        case ConsoleKey.DownArrow:
                            if (line < lines.Count - 1)
                            {
                                line++;
                                column = Math.Min(column, lines[line].Length);
                            }
                            continue;

                        case ConsoleKey.LeftArrow:
                            if (column > 0)
                            {
                                column--;
                            }
                            else if (line > 0)
                            {
                                line--;
                                column = lines[line].Length;
                            }
                            continue;

                        case ConsoleKey.RightArrow:
                            if (column < lines[line].Length)
                            {
                                column++;
                            }
                            else if (line < lines.Count - 1)
                            {
                                line++;
                                column = 0;
                            }
                            continue;

                        case ConsoleKey.Home:
                            column = 0;
                            continue;

                        case ConsoleKey.End:
                            column = lines[line].Length;
                            continue;

                        case ConsoleKey.PageUp:
                            line = Math.Max(0, line - GetContentHeight());
                            column = Math.Min(column, lines[line].Length);
                            continue;

                        case ConsoleKey.PageDown:
                            line = Math.Min(lines.Count - 1, line + GetContentHeight());
                            column = Math.Min(column, lines[line].Length);
                            continue;

                        case ConsoleKey.Enter:
                            string remainder = lines[line][column..];
                            lines[line] = lines[line][..column];
                            lines.Insert(++line, remainder);
                            column = 0;
                            dirty = true;
                            continue;

                        case ConsoleKey.Backspace:
                            if (column > 0)
                            {
                                lines[line] = lines[line].Remove(column - 1, 1);
                                column--;
                                dirty = true;
                            }
                            else if (line > 0)
                            {
                                int previousLength = lines[line - 1].Length;
                                lines[line - 1] += lines[line];
                                lines.RemoveAt(line--);
                                column = previousLength;
                                dirty = true;
                            }
                            continue;

                        case ConsoleKey.Delete:
                            if (column < lines[line].Length)
                            {
                                lines[line] = lines[line].Remove(column, 1);
                                dirty = true;
                            }
                            else if (line < lines.Count - 1)
                            {
                                lines[line] += lines[line + 1];
                                lines.RemoveAt(line + 1);
                                dirty = true;
                            }
                            continue;

                        case ConsoleKey.Tab:
                            int spaces = TabWidth - column % TabWidth;
                            lines[line] = lines[line].Insert(column, new string(' ', spaces));
                            column += spaces;
                            dirty = true;
                            continue;
                    }

                    if (!char.IsControl(key.KeyChar))
                    {
                        lines[line] = lines[line].Insert(column, key.KeyChar.ToString());
                        column++;
                        dirty = true;
                    }
                }
            }
            finally
            {
                Console.Write("\x1b[?25h\x1b[?1049l");
                Console.TreatControlCAsInput = treatControlCAsInput;
            }
        }

        private static void Render(
            IReadOnlyList<string> lines,
            string displayPath,
            int currentLine,
            int currentColumn,
            bool dirty,
            ref int verticalOffset,
            ref int horizontalOffset)
        {
            int width = Math.Max(20, Console.WindowWidth);
            int contentHeight = GetContentHeight();
            int lineNumberWidth = Math.Max(1, lines.Count.ToString().Length);
            int prefixWidth = lineNumberWidth + 3;
            int textWidth = Math.Max(1, width - prefixWidth - 1);

            if (currentLine < verticalOffset)
            {
                verticalOffset = currentLine;
            }
            else if (currentLine >= verticalOffset + contentHeight)
            {
                verticalOffset = currentLine - contentHeight + 1;
            }

            if (currentColumn < horizontalOffset)
            {
                horizontalOffset = currentColumn;
            }
            else if (currentColumn >= horizontalOffset + textWidth)
            {
                horizontalOffset = currentColumn - textWidth + 1;
            }

            Console.Write("\x1b[?25l\x1b[H");
            WriteClipped(
                Colors.Bold(Colors.BrightWhite(" Nocturne Editor ")) +
                Colors.Dim($" {displayPath}{(dirty ? " *" : "")}"),
                $" Nocturne Editor  {displayPath}{(dirty ? " *" : "")}",
                width);

            for (int screenLine = 0; screenLine < contentHeight; screenLine++)
            {
                int sourceLine = verticalOffset + screenLine;
                Console.Write("\x1b[2K");

                if (sourceLine >= lines.Count)
                {
                    Console.Write(Colors.Dim("~"));
                    Console.WriteLine();
                    continue;
                }

                string number = (sourceLine + 1).ToString().PadLeft(lineNumberWidth);
                Console.Write(sourceLine == currentLine
                    ? Colors.BrightCyan(number)
                    : Colors.Dim(number));
                Console.Write(Colors.Dim(" │ "));

                string text = lines[sourceLine];
                if (horizontalOffset < text.Length)
                {
                    int length = Math.Min(textWidth, text.Length - horizontalOffset);
                    Console.Write(Sanitize(text.Substring(horizontalOffset, length)));
                }

                Console.WriteLine();
            }

            Console.Write("\x1b[2K");
            WriteClipped(
                Colors.Dim($" Ln {currentLine + 1}, Col {currentColumn + 1}"),
                $" Ln {currentLine + 1}, Col {currentColumn + 1}",
                width);
            Console.Write("\x1b[2K");
            WriteClipped(
                $" {Colors.BrightWhite("F2")} Save  " +
                $"{Colors.BrightWhite("Esc")} Cancel  " +
                $"{Colors.BrightWhite("Arrows")} Move",
                " F2 Save  Esc Cancel  Arrows Move",
                width,
                appendNewLine: false);

            int cursorRow = currentLine - verticalOffset + 2;
            int cursorColumn = prefixWidth + currentColumn - horizontalOffset + 1;
            Console.Write($"\x1b[{cursorRow};{cursorColumn}H\x1b[?25h");
        }

        private static void WriteClipped(
            string styledText,
            string plainText,
            int width,
            bool appendNewLine = true)
        {
            int availableWidth = Math.Max(1, width - 1);
            Console.Write(plainText.Length <= availableWidth
                ? styledText
                : plainText[..availableWidth]);
            Console.Write("\x1b[K");

            if (appendNewLine)
            {
                Console.WriteLine();
            }
        }

        private static string Sanitize(string text)
        {
            StringBuilder result = new(text.Length);
            foreach (char character in text)
            {
                result.Append(character switch
                {
                    '\t' => '→',
                    _ when char.IsControl(character) => '�',
                    _ => character
                });
            }
            return result.ToString();
        }

        private static int GetContentHeight()
        {
            return Math.Max(1, Console.WindowHeight - 3);
        }

        private static string DetectNewLine(string text)
        {
            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] == '\r')
                {
                    return index + 1 < text.Length && text[index + 1] == '\n'
                        ? "\r\n"
                        : "\r";
                }

                if (text[index] == '\n')
                {
                    return "\n";
                }
            }

            return Environment.NewLine;
        }
    }
}
