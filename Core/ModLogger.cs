using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace ResourcefulHands.Core;

public static class ModLogger
{
    public static ManualLogSource Log = null!;

    public static void InitLog(ManualLogSource log)
    {
        Log = log;
    }
    
    private const string Prefix = "[Resourceful Hands] ";

    [Conditional("DEBUG")]
    public static void Debug(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogInfo($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }

    public static void Info(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogInfo($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }

    public static void Message(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "") 
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogMessage($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }

    public static void Warning(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogWarning($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }

    public static void Error(object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
    {
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogError($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
    }

    /// <summary>
    /// This class is used to print logs to the game's dev console not just the unity/bepinex one.
    /// </summary>
    public static class Player
    {
        private static void Message(string message) => CommandConsole.Log(Prefix + message);
        
        /// Sends an info message to the game console
        public static void Info(string message) => Message(message);
        /// Sends a warning message to the game console
        public static void Warning(string message) => Message(Prefix + "[WARNING] " + message);
        /// Sends an error message to the game console
        public static void Error(string message) => CommandConsole.LogError(Prefix + message);
    }
}