using Nocturne.Utils;

namespace Nocturne.Interaction
{
    public struct SelecterItem
    {
        public string Name;
        public string Value;
        public string Description;
    }

    public static class Selector
    {
        public static SelecterItem Select(SelecterItem[] items)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (items.Length == 0)
            {
                throw new ArgumentException("At least one item is required.", nameof(items));
            }

            if (Console.IsInputRedirected)
            {
                throw new InvalidOperationException("Selector requires an interactive console.");
            }

            if (Console.CursorLeft != 0)
            {
                Console.WriteLine();
            }

            try
            {
                Console.CursorVisible = false;

                int selectedIndex = 0;
                int startTop = Console.CursorTop;

                Console.CancelKeyPress += CancelKeyPressed;

                while (true)
                {
                    Render(items, selectedIndex, startTop);

                    switch (Console.ReadKey(intercept: true).Key)
                    {
                        case ConsoleKey.UpArrow:
                            selectedIndex = (selectedIndex - 1 + items.Length) % items.Length;
                            break;

                        case ConsoleKey.DownArrow:
                            selectedIndex = (selectedIndex + 1) % items.Length;
                            break;

                        case ConsoleKey.Enter:
                            Console.WriteLine();
                            Console.CursorVisible = true;
                            return items[selectedIndex];
                    }
                }
            }
            finally
            {
                Console.CancelKeyPress -= CancelKeyPressed;
            }
        }

        private static void CancelKeyPressed(object? sender, ConsoleCancelEventArgs ev)
        {
            ev.Cancel = true;
        }

        private static void Render(SelecterItem[] items, int selectedIndex, int startTop)
        {
            Console.SetCursorPosition(0, startTop);

            for (int i = 0; i < items.Length; i++)
            {
                Console.Write(Colors.BrightWhite(i == selectedIndex ? "> " : "  "));
                Console.Write(i == selectedIndex ? Colors.BrightGreen(items[i].Name) : items[i].Name);
                Console.WriteLine("\x1b[K");
            }

            Console.WriteLine();
            Console.Write(Colors.Dim(items[selectedIndex].Description));
            Console.WriteLine("\x1b[K");
            Console.Write("\x1b[K");
        }
    }
}
