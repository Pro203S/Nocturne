using System.Runtime.InteropServices;

namespace Nocturne.Utils
{
    public static class ConsoleFeatures
    {
        private const int StdOutputHandle = -11;
        private const int StdErrorHandle = -12;
        private const uint EnableVirtualTerminalProcessing = 0x0004;

        public static void EnableAnsiColors()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            EnableAnsiColors(GetStdHandle(StdOutputHandle));
            EnableAnsiColors(GetStdHandle(StdErrorHandle));
        }

        private static void EnableAnsiColors(nint handle)
        {
            if (handle == 0 || handle == -1 ||
                !GetConsoleMode(handle, out uint mode))
            {
                return;
            }

            _ = SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetStdHandle(int standardHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleMode(nint consoleHandle, out uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleMode(nint consoleHandle, uint mode);
    }
}
