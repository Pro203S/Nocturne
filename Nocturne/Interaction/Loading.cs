using System.Diagnostics;
using Nocturne.Utils;

namespace Nocturne.Interaction
{
    public sealed class Loading : IDisposable
    {
        private static readonly string[] Frames =
        [
            "⠋",
            "⠙",
            "⠹",
            "⠸",
            "⠼",
            "⠴",
            "⠦",
            "⠧",
            "⠇",
            "⠏"
        ];

        private static readonly (int Red, int Green, int Blue)[] RainbowColors =
        [
            (255, 95, 86),
            (255, 159, 67),
            (255, 217, 61),
            (155, 225, 93),
            (46, 213, 115),
            (46, 217, 195),
            (84, 160, 255),
            (95, 111, 255),
            (165, 94, 234),
            (255, 107, 214)
        ];

        private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(80);
        private static readonly object ConsoleSyncRoot = new();
        private static Loading? activeLoading;

        private readonly object syncRoot = new();
        private CancellationTokenSource? cancellation;
        private Task? animationTask;
        private bool outputRedirected;
        private bool? previousCursorVisible;
        private bool disposed;

        public bool IsRunning
        {
            get
            {
                lock (syncRoot)
                {
                    return cancellation is not null;
                }
            }
        }

        public void Start(string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            lock (syncRoot)
            {
                ObjectDisposedException.ThrowIf(disposed, this);

                if (cancellation is not null)
                {
                    throw new InvalidOperationException("Loading is already running.");
                }

                outputRedirected = Console.IsOutputRedirected;
                cancellation = new CancellationTokenSource();

                if (outputRedirected)
                {
                    Console.WriteLine(message);
                    return;
                }

                previousCursorVisible = GetCursorVisible();
                SetCursorVisible(false);

                CancellationToken token = cancellation.Token;
                lock (ConsoleSyncRoot)
                {
                    activeLoading = this;
                }

                animationTask = Task.Run(() => AnimateAsync(message, token), token);
            }
        }

        public void Stop(string? message = null)
        {
            if (message is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(message);
            }

            lock (syncRoot)
            {
                ObjectDisposedException.ThrowIf(disposed, this);

                if (cancellation is null)
                {
                    throw new InvalidOperationException("Loading is not running.");
                }

                StopAnimation();

                if (outputRedirected)
                {
                    if (message is not null)
                    {
                        Console.WriteLine(message);
                    }
                }
                else
                {
                    lock (ConsoleSyncRoot)
                    {
                        Deactivate();
                        ClearLine();

                        if (message is not null)
                        {
                            Console.WriteLine(Colors.BrightGreen("✓") + " " + message);
                        }
                    }

                    RestoreCursorVisibility();
                }
            }
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (disposed)
                {
                    return;
                }

                if (cancellation is not null)
                {
                    StopAnimation();

                    if (!outputRedirected)
                    {
                        lock (ConsoleSyncRoot)
                        {
                            Deactivate();
                            ClearLine();
                        }

                        RestoreCursorVisibility();
                    }
                }

                disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        internal static void WriteLine(string message)
        {
            lock (ConsoleSyncRoot)
            {
                if (activeLoading is not null)
                {
                    ClearLine();
                }

                Console.WriteLine(message);
            }
        }

        private static async Task AnimateAsync(string message, CancellationToken token)
        {
            Stopwatch elapsed = Stopwatch.StartNew();
            int frameIndex = 0;

            try
            {
                while (true)
                {
                    var color = RainbowColors[frameIndex % RainbowColors.Length];
                    string duration = elapsed.Elapsed.TotalSeconds >= 1
                        ? Colors.Dim($" ({(int)elapsed.Elapsed.TotalSeconds}s)")
                        : string.Empty;

                    lock (ConsoleSyncRoot)
                    {
                        ClearLine();
                        Console.Write(
                            Colors.Rgb(
                                color.Red,
                                color.Green,
                                color.Blue,
                                Frames[frameIndex]) +
                            " " +
                            message +
                            duration);
                    }

                    frameIndex = (frameIndex + 1) % Frames.Length;
                    await Task.Delay(FrameInterval, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // Rendering failures must not fault the caller's work.
            }
        }

        private void Deactivate()
        {
            if (ReferenceEquals(activeLoading, this))
            {
                activeLoading = null;
            }
        }

        private void StopAnimation()
        {
            cancellation!.Cancel();

            try
            {
                animationTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // Ignore failures from the background rendering task.
            }
            finally
            {
                animationTask = null;
                cancellation.Dispose();
                cancellation = null;
            }
        }

        private void RestoreCursorVisibility()
        {
            if (previousCursorVisible.HasValue)
            {
                SetCursorVisible(previousCursorVisible.Value);
                previousCursorVisible = null;
            }
        }

        private static bool? GetCursorVisible()
        {
            try
            {
                return Console.CursorVisible;
            }
            catch
            {
                return null;
            }
        }

        private static void SetCursorVisible(bool visible)
        {
            try
            {
                Console.CursorVisible = visible;
            }
            catch
            {
            }
        }

        private static void ClearLine()
        {
            Console.Write("\r\x1b[2K");
        }
    }
}
