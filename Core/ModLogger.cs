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
    public static void Debug(
#if DEBUG
        object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
#else 
        object data)
#endif
    {
#if DEBUG
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogDebug($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
#else 
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogDebug($"{data}"));
#endif
    }

    public static void Info(
#if DEBUG
        object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
#else 
        object data)
#endif
    {
#if DEBUG
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogInfo($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
#else 
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogInfo($"{data}"));
#endif
    }

    public static void Message(
#if DEBUG
        object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
#else 
        object data)
#endif
    {
#if DEBUG
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogMessage($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
#else 
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogMessage($"{data}"));
#endif
    }

    public static void Warning(
#if DEBUG
        object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
#else 
        object data)
#endif
    {
#if DEBUG
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogWarning($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
#else 
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogWarning($"{data}"));
#endif
    }

    public static void Error(
#if DEBUG
        object data,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string file = "")
#else 
        object data)
#endif
    {
#if DEBUG
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogError($"[{Path.GetFileName(file)}:{lineNumber}] {data}"));
#else 
        CoroutineDispatcher.RunOnMainThreadOrCurrent(() => Log.LogError($"{data}"));
#endif
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
        public static void Warning(string message) => Message("[WARNING] " + message);
        /// Sends an error message to the game console
        public static void Error(string message) => CommandConsole.LogError(Prefix + message);
    }
}