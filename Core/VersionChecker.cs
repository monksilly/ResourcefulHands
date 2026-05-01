using UnityEngine;

namespace ResourcefulHands.Core;

public static class VersionChecker
{
    private static readonly WKVersion MinVersion = new("0.55i");
    private static readonly WKVersion MaxVersion = new("0.55m");

    public static void Check()
    {
        WKVersion current = new(Application.version);
        if (current < MinVersion)
            ModLogger.Error($"Mod incompatible! Game too old ({Application.version}). Need {MinVersion}.");
        else if (current > MaxVersion)
            ModLogger.Warning($"Mod made for {MaxVersion}, but game is {Application.version}. Expect issues.");
    }
}