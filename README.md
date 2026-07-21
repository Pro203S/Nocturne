# Nocturne 🌙

[한국어](README.ko.md)

Nocturne is a shell that brings a special experience to the familiar Windows terminal.

> Nocturne is under active development. Commands, configuration, and extension
> APIs may change between releases.

## Features

- Slash commands
- DLL extensibility
- Discord RPC integration
- Built-in file editor

## Installation

Nocturne supports Windows only.

ARM64, x86, and x64 are supported.

1. Download the archive for your architecture from [GitHub Releases](https://github.com/Pro203S/Nocturne/releases).
2. Extract the archive.
3. Run `Nocturne.exe`.

No separate .NET Runtime installation is required.

## Using Nocturne with Windows Terminal

For more information about Windows Terminal, see [microsoft/terminal](https://github.com/microsoft/terminal).

You can create a new Windows Terminal profile for Nocturne.

[Learn about Windows Terminal profiles](https://learn.microsoft.com/en-us/windows/terminal/customize-settings/profile-general)

## Customization

When Nocturne runs for the first time, it creates a **nocturne.ns** file in `%UserProfile%`.

Edit `nocturne.ns` to customize the shell or add commands to run when the shell starts.

## Extensions

You can create Nocturne extension DLLs to add custom slash commands.

See the [extension guide](/docs/en/extensions.md) to learn how to create one.
