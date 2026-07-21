using System.Text;
using Nocturne.Discord;
using Nocturne.Extensions;
using Nocturne.Utils;

namespace Nocturne
{
    public class Program
    {
        public static readonly string Version = "v1.0.0";
        async static Task Main(string[] args)
        {
            Console.WriteLine(Colors.Dim("[ Nocturne " + Version + " ]\n"));
            RunSafely(() => Console.OutputEncoding = Encoding.UTF8);
            RunSafely(ConsoleFeatures.EnableAnsiColors);
            RunSafely(Profile.Load);

            Logger.Log("[SYSTEM] Received arguments: " + string.Join(", ", args));

            Shell shell = new()
            {
                Cwd = RunSafely(
                    () => args.ElementAtOrDefault(0) ??
                        Environment.CurrentDirectory ??
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".")
            };

            RunSafely(ExtensionManager.LoadInstalled);

            RunSafely(() =>
            {
                if (!Convert.ToBoolean(Environment.GetEnvironmentVariable("NOCTURNE_DISCORD_RPC"))) return;

                RPC.Initialize();
            });

            await Update.Run();
            Logger.Log("[SYSTEM] Initialization complete.");

            for (; ; )
            {
                RunSafely(shell.Run);
            }
        }

        private static void RunSafely(Action action)
        {
            try
            {
                action();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                ReportException(exception);
            }
        }

        private static T RunSafely<T>(Func<T> action, T fallback)
        {
            try
            {
                return action();
            }
            catch (OperationCanceledException)
            {
                return fallback;
            }
            catch (Exception exception)
            {
                ReportException(exception);
                return fallback;
            }
        }

        private static void ReportException(Exception exception)
        {
            try
            {
                Logger.Log(
                    $"[ERROR] {exception.GetType().Name}: {exception.Message}");
                Console.Error.WriteLine(Colors.BrightRed(exception.Message));
            }
            catch
            {
                // An error while reporting an exception must not terminate the shell.
            }
        }
    }
}
