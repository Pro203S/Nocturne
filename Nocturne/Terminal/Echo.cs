namespace Nocturne.Terminal
{
    public sealed class Echo : TerminalCommand
    {
        protected override void Execute(string arguments, Shell shell)
        {
            Console.WriteLine(arguments);
        }
    }
}