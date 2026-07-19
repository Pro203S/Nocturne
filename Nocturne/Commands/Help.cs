using Nocturne.Utils;

namespace Nocturne.Commands
{
    public class Help : SlashCommand
    {
        public override string Description
        {
            get
            {
                return "Shows the help message.";
            }
        }

        protected override void Execute(string[] args, Shell shell)
        {
            Console.WriteLine("Available commands in {0} 🌙", Colors.Bold(Colors.BrightWhite("Nocturne shell")));
            Console.WriteLine();
            foreach (var command in Commands)
            {
                Console.WriteLine("{0} {1}{2}{3}", Colors.Dim("-"), Colors.Bold(Colors.White(command.Key)), Colors.Dim(": "), Colors.BrightWhite(command.Value.Description));
            }
            Console.WriteLine();
        }
    }
}