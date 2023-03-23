using Verse;

namespace BetterRomance
{
    internal static class LogUtil
    {
        private static string WrapMessage(string message) => $"<color=#1116e4>[WayBetterRomance]</color> {message}";

        internal static void Message(string message) => Log.Message(WrapMessage(message));
        internal static void Warning(string message) => Log.Warning(WrapMessage(message));
        internal static void WarningOnce(string message, int key) => Log.WarningOnce(WrapMessage(message), key);
        internal static void Error(string message) => Log.Error(WrapMessage(message));
        internal static void ErrorOnce(string message, int key) => Log.ErrorOnce(WrapMessage(message), key);
    }
}
