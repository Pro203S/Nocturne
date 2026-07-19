using System.Text;
using Nocturne.Interaction;
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

            //var loading = new Loading();
            //loading.Start("Checking update...");

            Logger.Log("Verbose");

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
                Console.Error.WriteLine(Colors.BrightRed(exception.Message));
            }
            catch
            {
                // An error while reporting an exception must not terminate the shell.
            }
        }
    }
}
