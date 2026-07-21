using Nocturne.Utils;

namespace Nocturne.Commands
{
    public abstract class SlashCommand
    {
        public static readonly Dictionary<string, SlashCommand> Commands =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["/help"] = new Help(),
                ["/version"] = new Version(),
                ["/editor"] = new Editor(),
                ["/elevate"] = new Elevate(),
                ["/presets"] = new Presets(),
                ["/reload"] = new Reload(),
                ["/theme"] = new Theme(),
                ["/extension"] = new Extension()
            };

        public static readonly Dictionary<string, SlashCommand> ExtensionCommands =
            new(StringComparer.OrdinalIgnoreCase);

        public abstract string Description { get; }

        public virtual string? ExtensionName => null;

        public static bool TryExecute(string input, Shell shell)
        {
            int separator = input.IndexOf(' ');
            string name = separator < 0 ? input : input[..separator];

            if (!Commands.TryGetValue(name, out SlashCommand? command) &&
                !ExtensionCommands.TryGetValue(name, out command))
            {
                Logger.Log($"[COMMAND] Unknown slash command: {name}");
                return false;
            }

            string source = command.ExtensionName is null
                ? "Nocturne"
                : $"extension {command.ExtensionName}";
            Logger.Log($"[COMMAND] Executing {name} ({source}).");

            string arguments = separator < 0 ? "" : input[(separator + 1)..].TrimStart();
            command.Execute(arguments.Split(" "), shell);
            return true;
        }

        protected abstract void Execute(string[] args, Shell shell);
    }
}
