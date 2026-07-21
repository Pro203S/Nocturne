using Nocturne.Utils;

namespace Nocturne.Commands
{
    public class Version : SlashCommand
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
            Console.WriteLine(Colors.BrightWhite(Program.Version));
        }
    }
}
