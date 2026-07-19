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
            Console.WriteLine("Available commands in the {0} 🌙", Colors.Bold(Colors.BrightWhite("Nocturne shell")));
            Console.WriteLine();
            foreach (var command in Commands)
            {
                Console.WriteLine("{0} {1}{2}{3}", Colors.Dim("-"), Colors.Bold(Colors.White(command.Key)), Colors.Dim(": "), Colors.BrightWhite(command.Value.Description));
            }
            Console.WriteLine();

            if (ExtensionCommands.Count != 0)
            {
                Console.WriteLine(Colors.BrightWhite("Commands from extensions:"));
                Console.WriteLine();

                foreach (var command in ExtensionCommands)
                {
                    string source = command.Value.ExtensionName is null
                        ? ""
                        : Colors.Dim($" [{command.Value.ExtensionName}]");

                    Console.WriteLine(
                        "{0} {1}{2}{3}{4}",
                        Colors.Dim("-"),
                        Colors.Bold(Colors.White(command.Key)),
                        source,
                        Colors.Dim(": "),
                        Colors.BrightWhite(command.Value.Description));
                }

                Console.WriteLine();
            }
        }
    }
}
