namespace Nocturne.Terminal
{
    public sealed class RemoveEnvironment : TerminalCommand
    {
        protected override void Execute(string arguments, Shell shell)
        {
            Environment.SetEnvironmentVariable(arguments, null);
        }
    }
}