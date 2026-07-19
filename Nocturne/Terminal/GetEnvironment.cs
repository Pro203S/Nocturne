using Nocturne.Utils;

namespace Nocturne.Terminal
{
    public sealed class GetEnvironment : TerminalCommand
    {
        protected override void Execute(string arguments, Shell shell)
        {
            Logger.Log($"[ENV] Read environment variable {arguments}.");
            Console.WriteLine(Environment.GetEnvironmentVariable(arguments));
        }
    }
}
