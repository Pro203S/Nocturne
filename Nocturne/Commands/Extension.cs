using Nocturne.Extensions;
using Nocturne.Interaction;
using Nocturne.Utils;

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
            string operation = args.ElementAtOrDefault(0)?.Trim() ?? "";

            if (operation.Length == 0)
            {
                operation = (string)Selector.Select(
                [
                    new SelectorItem
                    {
                        Name = "Install new extension",
                        Value = "install",
                        Description = "Install an extension DLL"
                    },
                    new SelectorItem
                    {
                        Name = "List installed extensions",
                        Value = "list",
                        Description = "List installed and loaded extensions"
                    },
                    new SelectorItem
                    {
                        Name = "Uninstall an extension",
                        Value = "remove",
                        Description = "Unload and remove an extension"
                    }
                ]).Value;
            }

            switch (operation.ToLowerInvariant())
            {
                case "install":
                    Install(args.Skip(1).ToArray(), shell);
                    return;

                case "list":
                    List();
                    return;

                case "remove":
                case "uninstall":
                    Remove(args.Skip(1).ToArray());
                    return;

                default:
                    Console.WriteLine(Colors.BrightYellow(
                        "Usage: /extension [install <dll> | list | remove <name>]"));
                    return;
            }
        }

        private static void Install(string[] args, Shell shell)
        {
            string sourcePath = string.Join(' ', args).Trim().Trim('"');

            if (sourcePath.Length == 0)
            {
                Console.WriteLine("Enter the path to the extension DLL.");
                sourcePath = (ShellUtils.ReadLine() ?? "").Trim().Trim('"');
            }

            if (sourcePath.Length == 0)
            {
                throw new OperationCanceledException();
            }

            sourcePath = Path.GetFullPath(sourcePath, shell.Cwd);
            IReadOnlyList<ExtensionInfo> installed =
                ExtensionManager.Install(sourcePath);

            foreach (ExtensionInfo extension in installed)
            {
                Console.WriteLine(
                    $"{Colors.BrightGreen("Installed")} " +
                    $"{Colors.Bold(Colors.BrightWhite(extension.Name))} " +
                    Colors.Dim(extension.Version));
            }
        }

        private static void List()
        {
            IReadOnlyList<ExtensionInfo> extensions =
                ExtensionManager.InstalledExtensions;

            if (extensions.Count == 0)
            {
                Console.WriteLine(Colors.Dim("No extensions are installed."));
                return;
            }

            foreach (ExtensionInfo extension in extensions)
            {
                string status = extension.IsLoaded
                    ? Colors.BrightGreen("loaded")
                    : Colors.BrightRed("failed");

                Console.WriteLine(
                    $"{Colors.Bold(Colors.BrightWhite(extension.Name))} " +
                    $"{Colors.Dim(extension.Version)} [{status}]");

                if (extension.Description.Length != 0)
                {
                    Console.WriteLine("  " + extension.Description);
                }

                if (extension.Error is not null)
                {
                    Console.WriteLine("  " + Colors.BrightRed(extension.Error));
                }
            }
        }

        private static void Remove(string[] args)
        {
            IReadOnlyList<ExtensionInfo> extensions =
                ExtensionManager.InstalledExtensions;

            if (extensions.Count == 0)
            {
                Console.WriteLine(Colors.Dim("No extensions are installed."));
                return;
            }

            string query = string.Join(' ', args).Trim().Trim('"');
            ExtensionInfo selected;

            if (query.Length == 0)
            {
                selected = (ExtensionInfo)Selector.Select(
                [
                    .. extensions.Select(extension => new SelectorItem
                    {
                        Name = extension.Name,
                        Value = extension,
                        Description = extension.Description.Length == 0
                            ? Path.GetFileName(extension.FilePath)
                            : extension.Description
                    })
                ]).Value;
            }
            else
            {
                selected = extensions.FirstOrDefault(extension =>
                    extension.Name.Equals(query, StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileNameWithoutExtension(extension.FilePath)
                        .Equals(query, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException(
                        $"The extension \"{query}\" is not installed.");
            }

            ExtensionManager.Uninstall(selected.FilePath);
            Console.WriteLine(
                $"{Colors.BrightGreen("Removed")} " +
                Colors.Bold(Colors.BrightWhite(selected.Name)));
        }
    }
}
