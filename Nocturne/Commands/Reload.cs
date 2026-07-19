using Nocturne.Utils;

namespace Nocturne.Commands
{
    public class Reload : SlashCommand
    {
        public override string Description
        {
            get
            {
                return "Reload profile.";
            }
        }

        protected override void Execute(string[] args, Shell shell)
        {
            Profile.Load();
        }
    }
}