namespace Nocturne.Terminal
{
    public sealed class GetEnvironment : TerminalCommand
    {
        protected override void Execute(string arguments, Shell shell)
        {
            Console.WriteLine(Environment.GetEnvironmentVariable(arguments));
        }
    }
}