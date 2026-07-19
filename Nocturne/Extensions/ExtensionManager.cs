using System.Reflection;
using System.Runtime.Loader;
using Nocturne.Utils;

namespace Nocturne.Extensions
{
    public static class ExtensionManager
    {
        private static readonly object SyncRoot = new();
        private static readonly Dictionary<string, ExtensionPackage> Packages =
            new(StringComparer.OrdinalIgnoreCase);

        static ExtensionManager()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();
        }

        public static readonly string ExtensionsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "nocturne_extensions");

        public static IReadOnlyList<ExtensionInfo> InstalledExtensions
        {
            get
            {
                lock (SyncRoot)
                {
                    return Packages.Values
                        .SelectMany(package => package.GetInfo())
                        .OrderBy(extension => extension.Name)
                        .ToArray();
                }
            }
        }

        public static void LoadInstalled()
        {
            Directory.CreateDirectory(ExtensionsPath);
            string[] extensionFiles = Directory.GetFiles(
                ExtensionsPath,
                "*.dll",
                SearchOption.TopDirectoryOnly);
            Logger.Log(
                $"[EXTENSION] Found {extensionFiles.Length} installed DLL(s) in {ExtensionsPath}.");

            foreach (string filePath in extensionFiles)
            {
                try
                {
                    Load(filePath);
                }
                catch (Exception exception)
                {
                    string fullPath = Path.GetFullPath(filePath);

                    lock (SyncRoot)
                    {
                        Packages[fullPath] = ExtensionPackage.Failed(
                            fullPath,
                            exception.Message);
                    }

                    Logger.Log(
                        $"[EXTENSION] Failed to load {Path.GetFileName(filePath)}: " +
                        exception.Message);
                    Console.Error.WriteLine(Colors.BrightRed(
                        $"Failed to load extension {Path.GetFileName(filePath)}: " +
                        exception.Message));
                }
            }
        }

        public static IReadOnlyList<ExtensionInfo> Install(string sourcePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

            string fullSourcePath = Path.GetFullPath(sourcePath);
            if (!File.Exists(fullSourcePath))
            {
                throw new FileNotFoundException(
                    "The extension DLL was not found.",
                    fullSourcePath);
            }

            if (!Path.GetExtension(fullSourcePath)
                .Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "An extension must be a DLL file.",
                    nameof(sourcePath));
            }

            _ = AssemblyName.GetAssemblyName(fullSourcePath);
            Directory.CreateDirectory(ExtensionsPath);

            string destination = Path.Combine(
                ExtensionsPath,
                Path.GetFileName(fullSourcePath));
            if (File.Exists(destination))
            {
                throw new IOException(
                    $"The extension \"{Path.GetFileName(destination)}\" is already installed.");
            }

            File.Copy(fullSourcePath, destination);
            Logger.Log(
                $"[EXTENSION] Copied {fullSourcePath} to {destination}.");

            try
            {
                Load(destination);
                return GetInfo(destination);
            }
            catch
            {
                lock (SyncRoot)
                {
                    Packages.Remove(Path.GetFullPath(destination));
                }

                File.Delete(destination);
                throw;
            }
        }

        public static void Uninstall(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            string fullPath = Path.GetFullPath(filePath);
            ExtensionPackage? package;
            Logger.Log(
                $"[EXTENSION] Uninstalling {Path.GetFileName(fullPath)}.");

            lock (SyncRoot)
            {
                Packages.Remove(fullPath, out package);
            }

            package?.Unload();

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            Logger.Log(
                $"[EXTENSION] Uninstalled {Path.GetFileName(fullPath)}.");
        }

        public static void Shutdown()
        {
            ExtensionPackage[] packages;

            lock (SyncRoot)
            {
                packages = Packages.Values.ToArray();
                Packages.Clear();
            }

            Logger.Log(
                $"[EXTENSION] Shutting down {packages.Length} extension package(s).");
            foreach (ExtensionPackage package in packages)
            {
                package.Unload();
            }
        }

        private static void Load(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);
            Logger.Log(
                $"[EXTENSION] Loading {Path.GetFileName(fullPath)}.");

            lock (SyncRoot)
            {
                if (Packages.TryGetValue(fullPath, out ExtensionPackage? existing) &&
                    existing.IsLoaded)
                {
                    return;
                }
            }

            ExtensionLoadContext loadContext = new(fullPath);
            List<LoadedExtension> loadedExtensions = [];

            try
            {
                Assembly assembly;
                using (FileStream stream = new(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                {
                    assembly = loadContext.LoadFromStream(stream);
                }

                Type[] extensionTypes;
                try
                {
                    extensionTypes = assembly.GetTypes()
                        .Where(type =>
                            !type.IsAbstract &&
                            !type.IsInterface &&
                            typeof(INocturneExtension).IsAssignableFrom(type))
                        .ToArray();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    string details = string.Join(
                        "; ",
                        exception.LoaderExceptions
                            .Where(error => error is not null)
                            .Select(error => error!.Message));
                    throw new InvalidOperationException(
                        $"Could not inspect the extension types. {details}",
                        exception);
                }

                if (extensionTypes.Length == 0)
                {
                    throw new InvalidDataException(
                        "The DLL does not contain an INocturneExtension implementation.");
                }

                string version =
                    assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                        ?.InformationalVersion ??
                    assembly.GetName().Version?.ToString() ??
                    "unknown";
                string extensionDirectory = Path.GetDirectoryName(fullPath)!;

                foreach (Type extensionType in extensionTypes)
                {
                    INocturneExtension extension =
                        (INocturneExtension)(Activator.CreateInstance(extensionType) ??
                        throw new InvalidOperationException(
                            $"Could not create {extensionType.FullName}."));
                    string extensionName = extension.Name?.Trim() ?? "";
                    if (extensionName.Length == 0)
                    {
                        throw new InvalidOperationException(
                            $"{extensionType.FullName} has an empty extension name.");
                    }

                    ExtensionContext context = new(
                        extensionDirectory,
                        extensionName);

                    try
                    {
                        extension.Initialize(context);
                    }
                    catch
                    {
                        context.UnregisterCommands();

                        try
                        {
                            extension.Shutdown();
                        }
                        catch
                        {
                        }

                        throw;
                    }

                    loadedExtensions.Add(new LoadedExtension(
                        extension,
                        context,
                        new ExtensionInfo(
                            extensionName,
                            extension.Description?.Trim() ?? "",
                            version,
                            fullPath,
                            true,
                            null)));
                    Logger.Log(
                        $"[EXTENSION] Initialized {extensionName} ({version}).");
                }

                ExtensionPackage package = new(
                    fullPath,
                    loadContext,
                    loadedExtensions,
                    null);

                lock (SyncRoot)
                {
                    Packages[fullPath] = package;
                }

                Logger.Log(
                    $"[EXTENSION] Loaded {loadedExtensions.Count} extension(s) " +
                    $"from {Path.GetFileName(fullPath)}.");
            }
            catch
            {
                foreach (LoadedExtension loaded in loadedExtensions)
                {
                    loaded.Context.UnregisterCommands();

                    try
                    {
                        loaded.Instance.Shutdown();
                    }
                    catch
                    {
                    }
                }

                loadContext.Unload();
                throw;
            }
        }

        private static IReadOnlyList<ExtensionInfo> GetInfo(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);

            lock (SyncRoot)
            {
                return Packages.TryGetValue(fullPath, out ExtensionPackage? package)
                    ? package.GetInfo()
                    : [];
            }
        }

        private sealed record LoadedExtension(
            INocturneExtension Instance,
            ExtensionContext Context,
            ExtensionInfo Info);

        private sealed class ExtensionPackage(
            string filePath,
            ExtensionLoadContext? loadContext,
            List<LoadedExtension> extensions,
            string? error)
        {
            public bool IsLoaded => loadContext is not null;

            public static ExtensionPackage Failed(string filePath, string error)
            {
                return new ExtensionPackage(filePath, null, [], error);
            }

            public IReadOnlyList<ExtensionInfo> GetInfo()
            {
                if (extensions.Count != 0)
                {
                    return extensions.Select(extension => extension.Info).ToArray();
                }

                return
                [
                    new ExtensionInfo(
                        Path.GetFileNameWithoutExtension(filePath),
                        "",
                        "unknown",
                        filePath,
                        false,
                        error)
                ];
            }

            public void Unload()
            {
                foreach (LoadedExtension extension in extensions)
                {
                    extension.Context.UnregisterCommands();

                    try
                    {
                        extension.Instance.Shutdown();
                    }
                    catch (Exception exception)
                    {
                        Logger.Log(
                            $"Extension shutdown failed: {exception.Message}");
                    }
                }

                extensions.Clear();
                loadContext?.Unload();
            }
        }

        private sealed class ExtensionLoadContext(string extensionPath)
            : AssemblyLoadContext(isCollectible: true)
        {
            private readonly AssemblyDependencyResolver resolver = new(extensionPath);
            private readonly string directory = Path.GetDirectoryName(extensionPath)!;

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                if (assemblyName.Name ==
                    typeof(INocturneExtension).Assembly.GetName().Name)
                {
                    return null;
                }

                string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
                assemblyPath ??= Path.Combine(
                    directory,
                    (assemblyName.Name ?? "") + ".dll");

                return File.Exists(assemblyPath)
                    ? LoadFromAssemblyPath(assemblyPath)
                    : null;
            }

            protected override nint LoadUnmanagedDll(string unmanagedDllName)
            {
                string? libraryPath =
                    resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

                return libraryPath is null
                    ? nint.Zero
                    : LoadUnmanagedDllFromPath(libraryPath);
            }
        }
    }
}
