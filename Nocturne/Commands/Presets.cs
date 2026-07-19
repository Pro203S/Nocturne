using Nocturne.Utils;

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
                return "Use or manage your presets.";
            }
        }

        protected override void Execute(string[] args, Shell shell)
        {
            try
            {
                var presets = GetPresets();


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

            string[] rawArgs = lines[1].Split(',');
            Dictionary<string, string> dic = [];

            foreach (string rawArg in rawArgs)
            {
                string[] kv = rawArg.Split('|');
                dic[kv[0]] = kv[1];
            }

            p.Arguments = dic;

            return p;
        }
    }
}