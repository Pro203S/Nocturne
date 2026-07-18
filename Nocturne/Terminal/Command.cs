namespace Nocturne.Terminal
{
    public abstract class TerminalCommand
    {
        private static readonly Dictionary<string, TerminalCommand> Commands =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["cd"] = new ChangeDirectory(),
                ["get"] = new GetEnvironment(),
                ["set"] = new SetEnvironment(),
                ["unset"] = new RemoveEnvironment(),
                ["echo"] = new Echo()
            };

        public static bool TryExecute(string input, Shell shell)
        {
            int separator = input.IndexOf(' ');
            string name = separator < 0 ? input : input[..separator];

            if (!Commands.TryGetValue(name, out TerminalCommand? command))
            {
                return false;
            }

            string arguments = separator < 0 ? "" : input[(separator + 1)..].TrimStart();
            command.Execute(arguments, shell);
            return true;
        }

        protected abstract void Execute(string arguments, Shell shell);
    }
}
