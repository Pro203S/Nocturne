using Nocturne.Interaction;
using Nocturne.Utils;

namespace Nocturne.Commands
{
    public class Theme : SlashCommand
    {
        public override string Description
        {
            get
            {
                return "Sets the console theme.";
            }
        }

        protected override void Execute(string[] args, Shell shell)
        {
            SelectorItem selectedTheme = Selector.Select([
                new() {
                    Name = "bash",
                    Value = "bash",
                    Description = "Set the console theme to Bash."
                },
                new() {
                    Name = "cmd",
                    Value = "cmd",
                    Description = "Set the console theme to cmd."
                },
                new() {
                    Name = "nocturne",
                    Value = "nocturne",
                    Description = "Set the console theme to Nocturne."
                }
            ]);

            if (!File.Exists(Profile.FilePath))
            {
                Profile.Load();
            }

            string[] lines = File.ReadAllLines(Profile.FilePath);
            string themeSetting = $"set NOCTURNE_THEME={selectedTheme.Value}";
            bool found = false;

            for (int i = 0; i < lines.Length; i++)
            {
                ReadOnlySpan<char> line = lines[i].AsSpan().Trim();

                if (!line.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ReadOnlySpan<char> assignment = line[4..].Trim();

                if (assignment.Length > 1 && assignment[0] == '"' && assignment[^1] == '"')
                {
                    assignment = assignment[1..^1];
                }

                int separator = assignment.IndexOf('=');

                if (separator > 0 &&
                    assignment[..separator].Trim().Equals(
                        "NOCTURNE_THEME".AsSpan(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = themeSetting;
                    found = true;
                }
            }

            if (!found)
            {
                Array.Resize(ref lines, lines.Length + 1);
                lines[^1] = themeSetting;
            }

            File.WriteAllLines(Profile.FilePath, lines);

            Profile.Load();
        }
    }
}
