namespace Nocturne
{
    public class Program
    {
        static void Main(string[] args)
        {
            string? initialPath = args.ElementAtOrDefault(0);
            Console.WriteLine(initialPath);
        }
    }
}