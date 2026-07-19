using Nocturne.Utils;

namespace Nocturne.Terminal
{
    public sealed class SetEnvironment : TerminalCommand
    {
        protected override void Execute(string arguments, Shell shell)
        {
            string[] kv = arguments.Split("=");
            Environment.SetEnvironmentVariable(kv[0], kv[1]);
            Logger.Log($"[ENV] Set environment variable {kv[0]}.");
        }
    }
}
