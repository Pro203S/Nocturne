using Nocturne.Interaction;
using Nocturne.Utils;
using System.Text.RegularExpressions;

namespace Nocturne.Commands
{
    struct Preset
    {
        public string Name;
        public Dictionary<string, string> Arguments;
        public string FullCommand;
    }
    public class Presets : SlashCommand
    {
        public static readonly string PresetsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "nocturne_presets");

        public override string Description
        {
            get
            {
                return "Runs or manages your presets.";
            }
        }

        protected override void Execute(string[] args, Shell shell)
        {
            try
            {
                var presets = GetPresets();

                var item = Selector.Select([
                    .. presets.Select((v) => new SelectorItem()
                    {
                        Name = v.Name,
                        Value = v,
                        Description = "Run the preset " + v.Name
                    }),
                    new SelectorItem()
                    {
                        Name = "Manage presets",
                        Value = "/managepresets",
                        Description = "Add, edit, or delete a preset"
                    }
                ]);

                if (item.Value is not string)
                {
                    var val = (Preset)item.Value;

                    var finalCommand = val.FullCommand;

                    foreach (string key in val.Arguments.Keys)
                    {
                        string value = val.Arguments[key];

                        Console.WriteLine("Enter a value for the argument " + Colors.BrightWhite(key) + ".");
                        finalCommand = finalCommand.Replace("${" + key + "}", ShellUtils.ReadLine());
                        Console.WriteLine();
                    }

                    Console.WriteLine("Running command: " + Colors.BrightWhite(finalCommand));
                    Profile.Execute(finalCommand, shell.Cwd);
                    return;
                }

                string work = (string)Selector.Select([
                    new SelectorItem()
                    {
                        Name = "Add a preset",
                        Value = "add",
                        Description = "Add a preset"
                    },
                    new SelectorItem()
                    {
                        Name = "Edit a preset",
                        Value = "edit",
                        Description = "Edit a preset"
                    },
                    new SelectorItem()
                    {
                        Name = "Delete a preset",
                        Value = "delete",
                        Description = "Delete a preset"
                    }
                ]).Value;

                if (work == "add")
                {
                    Console.WriteLine("Enter a name for the preset.");
                    string? name = ShellUtils.ReadLine();
                    if (string.IsNullOrEmpty(name))
                    {
                        Console.WriteLine(Colors.Red("The preset name cannot be empty."));
                        return;
                    }
                    Console.WriteLine();

                    Console.WriteLine("Enter the preset's full command. (Line breaks are not supported.)");
                    Console.WriteLine($"You can reference arguments using {Colors.BrightWhite("${name}")}.");
                    string? fullCommand = ShellUtils.ReadLine();
                    if (string.IsNullOrEmpty(fullCommand))
                    {
                        Console.WriteLine(Colors.Red("The full command cannot be empty."));
                        return;
                    }
                    Console.WriteLine();

                    AddPreset(new Preset
                    {
                        Name = name,
                        Arguments = GetArguments(fullCommand),
                        FullCommand = fullCommand
                    });
                    return;
                }

                if (work == "edit")
                {
                    if (presets.Length == 0)
                    {
                        Console.WriteLine(Colors.Red("There are no presets to edit."));
                        return;
                    }

                    Preset preset = (Preset)Selector.Select([
                        .. presets.Select(value => new SelectorItem
                        {
                            Name = value.Name,
                            Value = value,
                            Description = "Edit the preset " + value.Name
                        })
                    ]).Value;

                    Console.WriteLine("Enter a new full command for the preset. (Line breaks are not supported.)");
                    Console.WriteLine($"You can reference arguments using {Colors.BrightWhite("${name}")}.");
                    string? fullCommand = ShellUtils.ReadLine();
                    if (string.IsNullOrEmpty(fullCommand))
                    {
                        Console.WriteLine(Colors.Red("The full command cannot be empty."));
                        return;
                    }
                    Console.WriteLine();

                    preset.FullCommand = fullCommand;
                    preset.Arguments = GetArguments(fullCommand);
                    EditPreset(preset);
                    return;
                }

                if (work == "delete")
                {
                    if (presets.Length == 0)
                    {
                        Console.WriteLine(Colors.Red("There are no presets to delete."));
                        return;
                    }

                    Preset preset = (Preset)Selector.Select([
                        .. presets.Select(value => new SelectorItem
                        {
                            Name = value.Name,
                            Value = value,
                            Description = "Delete the preset " + value.Name
                        })
                    ]).Value;

                    DeletePreset(preset.Name);
                    return;
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine(Colors.Red("Cannot find the preset."));
            }
        }

        private static Preset[] GetPresets()
        {
            if (!Directory.Exists(PresetsPath))
                Directory.CreateDirectory(PresetsPath);

            List<Preset> presets = [];
            string[] presetPaths = Directory.GetFiles(PresetsPath);

            foreach (var preset in presetPaths)
            {
                presets.Add(GetPreset(preset));
            }

            return [.. presets];
        }

        private static Preset GetPreset(string presetPath)
        {
            if (!File.Exists(presetPath)) throw new FileNotFoundException();

            string[] lines = File.ReadAllLines(presetPath);

            Preset p = new()
            {
                Name = lines[0],
                FullCommand = lines[2]
            };

            Dictionary<string, string> dic = [];

            if (!string.IsNullOrEmpty(lines[1]))
            {
                foreach (string rawArg in lines[1].Split(','))
                {
                    string[] kv = rawArg.Split('|');
                    dic[kv[0]] = kv[1];
                }
            }

            p.Arguments = dic;

            return p;
        }

        private static Dictionary<string, string> GetArguments(string fullCommand)
        {
            Dictionary<string, string> arguments = [];
            foreach (Match match in Regex.Matches(fullCommand, @"\$\{([^{}]+)\}"))
            {
                arguments.TryAdd(match.Groups[1].Value, string.Empty);
            }

            return arguments;
        }

        private static void AddPreset(Preset p)
        {
            string arguments = string.Join(",", p.Arguments.Select(argument => $"{argument.Key}|{argument.Value}"));
            File.WriteAllLines(Path.Combine(PresetsPath, p.Name), [p.Name, arguments, p.FullCommand]);
        }

        private static void EditPreset(Preset p)
        {
            AddPreset(p);
        }

        private static void DeletePreset(string presetName)
        {
            File.Delete(Path.Combine(PresetsPath, presetName));
        }
    }
}
