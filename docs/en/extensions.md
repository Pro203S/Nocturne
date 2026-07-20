# Nocturne extension guide

[한국어](../ko/extensions.md)

Nocturne extensions are .NET DLLs that run in the Nocturne process. An
extension implements `INocturneExtension` and registers one or more slash
commands during initialization.

> Only install extensions you trust. Extensions are not sandboxed: they run
> with the same filesystem, network, environment, and process permissions as
> Nocturne.

## Requirements

- Windows
- .NET 10 SDK
- A source checkout of Nocturne

The extension contract is currently part of the Nocturne application assembly,
not a separately versioned NuGet SDK. Target `net10.0-windows`, reference the
Nocturne project, and rebuild and test your extension when the host is updated.

## Create an extension

The following example assumes that `HelloNocturne` is created in the root of a
Nocturne source checkout, next to the `Nocturne` project directory.

Create the class library:

```powershell
dotnet new classlib -n HelloNocturne -f net10.0
```

Replace `HelloNocturne/HelloNocturne.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference
      Include="..\Nocturne\Nocturne.csproj"
      Private="false" />
  </ItemGroup>
</Project>
```

Replace `Class1.cs` with:

```csharp
using Nocturne;
using Nocturne.Extensions;

namespace HelloNocturne;

public sealed class HelloExtension : INocturneExtension
{
    public string Name => "Hello Nocturne";

    public string Description => "Adds a friendly /hello command.";

    public void Initialize(ExtensionContext context)
    {
        context.RegisterCommand(
            "hello",
            "Greet a person and show the current directory.",
            (args, shell) =>
            {
                string target = string.Join(' ', args).Trim();
                if (target.Length == 0)
                {
                    target = Environment.UserName;
                }

                Console.WriteLine($"Hello, {target}!");
                Console.WriteLine($"Current directory: {shell.Cwd}");
            });
    }

    public void Shutdown()
    {
        // Release extension-owned resources here.
    }
}
```

The extension class must be concrete and have a public parameterless
constructor. An implicit constructor, as in the example, is sufficient.

Build the extension:

```powershell
dotnet build HelloNocturne/HelloNocturne.csproj -c Release
```

The extension DLL is written to:

```text
HelloNocturne\bin\Release\net10.0-windows\HelloNocturne.dll
```

## Install and manage extensions

Run these commands inside Nocturne:

```text
/extension install "C:\path\to\HelloNocturne.dll"
/extension list
/hello Nocturne
/extension remove "Hello Nocturne"
```

`/extension uninstall <name>` is an alias for `remove`. A relative install path
is resolved from the shell's current directory. Running `/extension` without an
operation opens the interactive manager.

During installation, Nocturne:

1. validates that the selected file is a managed DLL;
2. copies it to `%USERPROFILE%\nocturne_extensions`;
3. loads and initializes every extension type in the DLL; and
4. makes registered commands immediately available.

No restart is required. On later starts, Nocturne automatically loads every
top-level DLL in the extension directory. You can also install manually by
copying an extension DLL into that directory and restarting Nocturne.

Nocturne does not overwrite an installed DLL with the same filename. Remove the
old extension before installing its replacement.

## Extension API

### `INocturneExtension`

| Member | Purpose |
| --- | --- |
| `string Name` | Required, non-empty name shown by `/extension list` and `/help` |
| `string Description` | Optional description; defaults to an empty string |
| `Initialize(ExtensionContext context)` | Called once when the extension is loaded |
| `Shutdown()` | Optional cleanup callback called when the package is unloaded |

The displayed version comes from the DLL's informational version, then its
assembly version. If neither is present, Nocturne displays `unknown`.

### `ExtensionContext`

| Member | Purpose |
| --- | --- |
| `ExtensionDirectory` | Absolute directory containing the installed DLL |
| `ExtensionName` | The current extension's `Name` |
| `RegisterCommand(name, description, execute)` | Register a command attributed to the current extension |
| `RegisterCommand(name, description, from, execute)` | Register a command with custom source attribution |

The leading `/` in a command name is optional. Names are case-insensitive,
cannot contain whitespace, and cannot duplicate a built-in or another
extension command.

The callback receives `string[] args` and the active `Shell`. Use `shell.Cwd` to
read the current working directory. Arguments are split on spaces by the
current command dispatcher; there is no automatic quote or option parser, so
an extension should normalize or parse its arguments when needed.

Registered commands are automatically removed when the extension unloads.
Exceptions thrown by command callbacks are reported by the shell without
terminating its main loop.

## Lifecycle and packaging

A DLL may contain more than one `INocturneExtension` implementation. Nocturne
creates and initializes all of them as one package. They share the DLL version
and file path, and removing any extension from that package unloads and removes
the entire DLL.

Initialization is package-wide: if any extension type fails to initialize,
Nocturne shuts down the types already initialized from that DLL, unregisters
their commands, and rejects the package. An installation that fails is also
removed from the extension directory.

When a loaded package is removed or Nocturne shuts down normally, Nocturne
unregisters its commands, calls `Shutdown()` on its extension instances, and
unloads its collectible load context.

### Dependencies

The installer currently copies only the selected extension DLL, while the
startup scanner treats every top-level DLL in the extension directory as an
extension candidate. For a clean installation, distribute a single extension
assembly with no additional private DLL dependencies. The reference to
Nocturne itself is supplied by the host.

If an extension requires extra managed or native files, treat that packaging
scenario as unsupported by the current installer and test it carefully. A
future extension SDK or package format may formalize dependency deployment.

## Troubleshooting

### The extension DLL was not found

Relative paths are resolved from the current directory shown by Nocturne. Use
an absolute path or change to the DLL's directory before installing.

### The DLL does not contain an implementation

Make sure at least one concrete type implements `INocturneExtension`. The type
must have a public parameterless constructor.

### A command is already registered

Choose a unique command name. Built-in and extension command names are matched
case-insensitively.

### The extension appears as `failed`

Run `/extension list` to see the loader error. Set `NOCTURNE_VERBOSE=true` in
`%USERPROFILE%\nocturne.ns` and run `/reload` to display more lifecycle
details. After correcting the extension, remove the failed DLL and install the
new build.

### The extension works on one build but not another

Nocturne does not yet negotiate an extension API version. Rebuild against the
target Nocturne source revision and test the result with that host version.

## Release checklist

- Target `net10.0-windows`.
- Use a unique extension and command name.
- Set meaningful version metadata in the project.
- Keep `Initialize` fast and release owned resources in `Shutdown`.
- Avoid extra private DLL dependencies with the current installer.
- Test installation, restart loading, command execution, and removal.
- Clearly state the Nocturne revision or release used for the build.
