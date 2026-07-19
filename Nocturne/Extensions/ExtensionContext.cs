using Nocturne.Commands;
using Nocturne.Utils;

namespace Nocturne.Extensions
{
    public sealed class ExtensionContext
    {
        private readonly List<string> registeredCommands = [];

        internal ExtensionContext(
            string extensionDirectory,
            string extensionName)
        {
            ExtensionDirectory = extensionDirectory;
            ExtensionName = extensionName;
        }

        public string ExtensionDirectory { get; }

        public string ExtensionName { get; }

        public void RegisterCommand(
            string name,
            string description,
            Action<string[], Shell> execute)
        {
            RegisterCommand(name, description, ExtensionName, execute);
        }

        public void RegisterCommand(
            string name,
            string description,
            string from,
            Action<string[], Shell> execute)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(from);
            ArgumentNullException.ThrowIfNull(execute);

            string commandName = name.Trim();
            if (!commandName.StartsWith('/'))
            {
                commandName = "/" + commandName;
            }

            if (commandName.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException(
                    "An extension command name cannot contain whitespace.",
                    nameof(name));
            }

            DelegateSlashCommand command = new(
                description?.Trim() ?? "",
                from.Trim(),
                execute);

            lock (SlashCommand.ExtensionCommands)
            {
                if (SlashCommand.Commands.ContainsKey(commandName) ||
                    !SlashCommand.ExtensionCommands.TryAdd(commandName, command))
                {
                    throw new InvalidOperationException(
                        $"The command \"{commandName}\" is already registered.");
                }
            }

            registeredCommands.Add(commandName);
            Logger.Log(
                $"[EXTENSION] {ExtensionName} registered command {commandName}.");
        }

        internal void UnregisterCommands()
        {
            lock (SlashCommand.ExtensionCommands)
            {
                foreach (string command in registeredCommands)
                {
                    SlashCommand.ExtensionCommands.Remove(command);
                    Logger.Log(
                        $"[EXTENSION] {ExtensionName} unregistered command {command}.");
                }
            }

            registeredCommands.Clear();
        }

        private sealed class DelegateSlashCommand(
            string description,
            string extensionName,
            Action<string[], Shell> execute) : SlashCommand
        {
            public override string Description { get; } = description;

            public override string ExtensionName { get; } = extensionName;

            protected override void Execute(string[] args, Shell shell)
            {
                execute(args, shell);
            }
        }
    }
}
