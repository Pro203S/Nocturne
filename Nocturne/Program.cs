using System.Text;
using Nocturne.Utils;

namespace Nocturne
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();
            Console.OutputEncoding = Encoding.UTF8;

            Shell shell = new()
            {
                Cwd = args.ElementAtOrDefault(0) ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            Profile.Load();

            if (Convert.ToBoolean(Environment.GetEnvironmentVariable("NOCTURNE_HELP_MSG")))
            {
                Console.Write(
                    "Welcome to {0} 🌙\n\nType {1} to get started.\n\n",
                    Colors.Bold(Colors.BrightWhite("Nocturne shell")),
                    Colors.Bold(Colors.BrightYellow("/help"))
                );
            }

            for (; ; )
            {
                shell.Run();
            }
        }
    }
}
