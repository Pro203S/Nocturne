using DiscordRPC;
using Nocturne.Utils;

namespace Nocturne.Discord
{
    public class RPC
    {
        public static readonly string CLIENT_ID = "1528387551449059480";

        private static readonly DiscordRpcClient rpc = new(CLIENT_ID);

        public static void Initialize()
        {
            rpc.OnReady += (sender, e) =>
            {
                Logger.Log($"[RPC] Connected to Discord (User: {e.User.DisplayName})");
            };

            rpc.OnPresenceUpdate += (sender, e) =>
            {
                Logger.Log("[RPC] Presence updated.");
            };

            rpc.Initialize();

            rpc.SetPresence(new RichPresence()
            {
                Details = "Working on " + Environment.MachineName,
                State = "Writing commands",
                Timestamps = Timestamps.Now
            });

            AppDomain.CurrentDomain.ProcessExit += (sender, ev) =>
            {
                rpc.Dispose();
            };
        }
    }
}