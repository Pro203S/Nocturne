using Nocturne.Interaction;

namespace Nocturne.Utils
{
    public class Logger
    {
        /// <summary>
        /// Verbose 모드일때 로그함
        /// </summary>
        public static void Log(string message)
        {
            try
            {
                if (Convert.ToBoolean(Environment.GetEnvironmentVariable("NOCTURNE_VERBOSE")))
                {
                    Loading.WriteLine($" {Colors.Dim("•")} {Colors.White(message)}");
                }
            }
            catch
            {

            }
        }
    }
}
