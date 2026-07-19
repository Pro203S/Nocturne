using Nocturne.Utils;

namespace Nocturne.Terminal
{
    public sealed class RemoveEnvironment : TerminalCommand
    {
        protected override void Execute(string arguments, Shell shell)
        {
            Environment.SetEnvironmentVariable(arguments, null);
            Logger.Log($"[ENV] Removed environment variable {arguments}.");
        }
    }
}
