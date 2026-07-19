using Nocturne.Interaction;

namespace Nocturne.Commands
{
    public class Extension : SlashCommand
    {
        public override string Description
        {
            get
            {
                return "Install or manage extensions.";
            }
        }

        protected override void Execute(string[] args, Shell shell)
        {
            var work = (string)Selector.Select([
                new SelectorItem()
                {
                    Name = "Install new extension",
                    Value = "install",
                    Description = "Install new extension"
                },
                new SelectorItem()
                {
                    Name = "List installed extensions",
                    Value = "list",
                    Description = "List installed extensions"
                },
                new SelectorItem()
                {
                    Name = "Uninstall an extension",
                    Value = "remove",
                    Description = "Uninstall an extension"
                }
            ]).Value;


        }
    }
}
