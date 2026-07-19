using Nocturne.Utils;

namespace Nocturne.Interaction
{
    public struct SelectorItem
    {
        public string Name;
        public object Value;
        public string Description;
    }

    public static class Selector
    {
        public static SelectorItem Select(SelectorItem[] items)
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

            bool treatControlCAsInput = Console.TreatControlCAsInput;

            try
            {
                Console.TreatControlCAsInput = true;
                Console.CursorVisible = false;

                int selectedIndex = 0;
                bool rendered = false;

                while (true)
                {
                    if (rendered)
                    {
                        MoveToStart(items.Length);
                    }

                    Render(items, selectedIndex);
                    rendered = true;

                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape ||
                        key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        Clear(items.Length);
                        throw new OperationCanceledException();
                    }

                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            selectedIndex = (selectedIndex - 1 + items.Length) % items.Length;
                            break;

                        case ConsoleKey.DownArrow:
                            selectedIndex = (selectedIndex + 1) % items.Length;
                            break;

                        case ConsoleKey.Enter:
                            Clear(items.Length);
                            return items[selectedIndex];
                    }
                }
            }
            finally
            {
                Console.TreatControlCAsInput = treatControlCAsInput;
                Console.CursorVisible = true;
            }
        }

        private static void Render(SelectorItem[] items, int selectedIndex)
        {
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

        private static void Clear(int itemCount)
        {
            MoveToStart(itemCount);
            Console.Write("\x1b[J");
        }

        private static void MoveToStart(int itemCount)
        {
            Console.Write($"\x1b[{itemCount + 2}A\r");
        }
    }
}
