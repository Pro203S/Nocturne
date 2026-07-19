namespace Nocturne.Commands
{
    public abstract class SlashCommand
    {
        public static readonly Dictionary<string, SlashCommand> Commands =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["/help"] = new Help(),
                ["/editor"] = new Editor(),
                ["/elevate"] = new Elevate(),
                ["/presets"] = new Presets(),
                ["/reload"] = new Reload(),
                ["/theme"] = new Theme(),
                ["/extension"] = new Extension()
            };

        public abstract string Description { get; }

        public static bool TryExecute(string input, Shell shell)
        {
            int separator = input.IndexOf(' ');
            string name = separator < 0 ? input : input[..separator];

            if (!Commands.TryGetValue(name, out SlashCommand? command))
            {
                return false;
            }

            string arguments = separator < 0 ? "" : input[(separator + 1)..].TrimStart();
            command.Execute(arguments.Split(" "), shell);
            return true;
        }

        protected abstract void Execute(string[] args, Shell shell);
    }
}
