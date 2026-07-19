using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;
using Nocturne.Interaction;

namespace Nocturne.Utils
{
    public class Update
    {
        public static async Task Run()
        {
            using Loading spinner = new();
            spinner.Start("Checking update...");

            try
            {
                using HttpClient net = new();
                net.DefaultRequestHeaders.UserAgent.ParseAdd($"Nocturne/{Program.Version}");

                Logger.Log("[UPDATE] Requesting to https://api.github.com/repos/Pro203S/Nocturne/releases...");
                string rawData = await net.GetStringAsync("https://api.github.com/repos/Pro203S/Nocturne/releases");
                spinner.Stop();
                
                JArray obj = (JArray)JToken.Parse(rawData);

                JObject? latestObject = (JObject?)obj.ElementAtOrDefault(0);
                if (latestObject == null)
                {
                    return;
                }

                string? tag_name = (string?)latestObject["tag_name"];
                if (tag_name == null)
                {
                    return;
                }

                Logger.Log("[UPDATE] Latest tag name: " + tag_name);

                if (Program.Version == tag_name)
                {
                    Logger.Log("[UPDATE] Update not needed. Skipping!");
                    return;
                }

                string runtime = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "win-x64",
                    Architecture.X86 => "win-x86",
                    Architecture.Arm64 => "win-arm64",
                    _ => throw new PlatformNotSupportedException("No update is available for this architecture.")
                };

                JObject? asset = latestObject["assets"]?
                    .OfType<JObject>()
                    .FirstOrDefault(item => string.Equals(
                        (string?)item["name"],
                        $"{runtime}.zip",
                        StringComparison.OrdinalIgnoreCase));
                string? downloadUrl = (string?)asset?["browser_download_url"];
                if (downloadUrl == null)
                {
                    throw new InvalidOperationException($"The {runtime} update package was not found.");
                }

                string? executablePath = Environment.ProcessPath;
                if (executablePath == null)
                {
                    throw new InvalidOperationException("Could not locate the running executable.");
                }

                string updateRoot = Path.Combine(
                    Path.GetTempPath(),
                    $"Nocturne-update-{Guid.NewGuid():N}");
                string archivePath = Path.Combine(updateRoot, "update.zip");
                string stagingPath = Path.Combine(updateRoot, "staging");
                Directory.CreateDirectory(stagingPath);

                spinner.Start($"Downloading {tag_name}...");
                using (HttpResponseMessage response = await net.GetAsync(
                    downloadUrl,
                    HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    await using Stream source = await response.Content.ReadAsStreamAsync();
                    await using FileStream destination = new(
                        archivePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        81920,
                        true);
                    await source.CopyToAsync(destination);
                }

                ZipFile.ExtractToDirectory(archivePath, stagingPath, true);
                spinner.Stop();

                string updaterPath = Path.Combine(updateRoot, "update.ps1");
                string restartArguments = string.Join(
                    " ",
                    Environment.GetCommandLineArgs().Skip(1).Select(QuoteArgument));
                string encodedArguments = Convert.ToBase64String(
                    Encoding.Unicode.GetBytes(restartArguments));
                File.WriteAllText(
                    updaterPath,
                    """
                    param(
                        [int]$ParentProcessId,
                        [string]$Source,
                        [string]$Destination,
                        [string]$Executable,
                        [string]$EncodedArguments,
                        [string]$UpdateRoot
                    )

                    Wait-Process -Id $ParentProcessId -ErrorAction SilentlyContinue
                    Copy-Item -Path (Join-Path $Source '*') -Destination $Destination -Recurse -Force
                    $arguments = [Text.Encoding]::Unicode.GetString(
                        [Convert]::FromBase64String($EncodedArguments))
                    if ($arguments.Length -eq 0) {
                        Start-Process -FilePath $Executable -WorkingDirectory $Destination
                    } else {
                        Start-Process -FilePath $Executable -ArgumentList $arguments -WorkingDirectory $Destination
                    }
                    Remove-Item -LiteralPath $UpdateRoot -Recurse -Force
                    """);

                ProcessStartInfo updater = new()
                {
                    FileName = "powershell.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                updater.ArgumentList.Add("-NoProfile");
                updater.ArgumentList.Add("-ExecutionPolicy");
                updater.ArgumentList.Add("Bypass");
                updater.ArgumentList.Add("-File");
                updater.ArgumentList.Add(updaterPath);
                updater.ArgumentList.Add(Environment.ProcessId.ToString());
                updater.ArgumentList.Add(stagingPath);
                updater.ArgumentList.Add(AppContext.BaseDirectory);
                updater.ArgumentList.Add(executablePath);
                updater.ArgumentList.Add(encodedArguments);
                updater.ArgumentList.Add(updateRoot);

                Process.Start(updater);
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                if (spinner.IsRunning)
                    spinner.Stop();

                Console.WriteLine(Colors.Red(e.Message));
            }
        }

        private static string QuoteArgument(string argument)
        {
            if (argument.Length != 0 && !argument.Any(char.IsWhiteSpace) && !argument.Contains('"'))
            {
                return argument;
            }

            StringBuilder quoted = new("\"");
            int backslashes = 0;

            foreach (char character in argument)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (character == '"')
                {
                    quoted.Append('\\', backslashes * 2 + 1);
                    quoted.Append('"');
                    backslashes = 0;
                    continue;
                }

                quoted.Append('\\', backslashes);
                quoted.Append(character);
                backslashes = 0;
            }

            quoted.Append('\\', backslashes * 2);
            quoted.Append('"');
            return quoted.ToString();
        }
    }
}
