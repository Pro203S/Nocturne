using System.Text;
using Nocturne.Discord;
using Nocturne.Extensions;
using Nocturne.Utils;

namespace Nocturne
{
    public class Program
    {
        public static readonly string Version = "v0.0.0";
        async static Task Main(string[] args)
        {
            RunSafely(Console.Clear);
            RunSafely(() => Console.OutputEncoding = Encoding.UTF8);
            RunSafely(ConsoleFeatures.EnableAnsiColors);

            Shell shell = new()
            {
                Cwd = RunSafely(
                    () => args.ElementAtOrDefault(0) ??
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".")
            };

            RunSafely(Profile.Load);
            Logger.Log($"[SYSTEM] Starting Nocturne {Version} (CWD: {shell.Cwd}).");
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
