namespace Nocturne.Extensions
{
    public interface INocturneExtension
    {
        string Name { get; }

        string Description
        {
            get
            {
                return "";
            }
        }

        void Initialize(ExtensionContext context);

        void Shutdown()
        {
        }
    }
}
