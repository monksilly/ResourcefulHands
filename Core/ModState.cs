using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace ResourcefulHands.Core;

public static class ModState
{
    private static int _mainThreadId;
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

    public static void Initialize()
    {
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public static bool IsDemo()
    {
        try {
            return Steamworks.SteamClient.AppId.Value == 3218540;
        } catch { return false; }
    }
}