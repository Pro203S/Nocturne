using System.Text;
using Nocturne.Utils;

namespace Nocturne
{
    public class Program
    {
        static void Main(string[] args)
        {
            RunSafely(Console.Clear);
            RunSafely(() => Console.OutputEncoding = Encoding.UTF8);

            Shell shell = new()
            {
                Cwd = RunSafely(
                    () => args.ElementAtOrDefault(0) ??
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".")
            };

            RunSafely(Profile.Load);

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
