using ResourcefulHands.Core;
using UnityEngine;

namespace ResourcefulHands.Systems;

public static class CosmeticSystem
{
    private static GameObject? _holder;

    public static void EnsureExists()
    {
        if (_holder != null) return;

        _holder = new GameObject("RHCosmeticSystem");
        _holder.AddComponent<OF_CosmeticPage>();
        Object.DontDestroyOnLoad(_holder);
        
        ModLogger.Info("Official Cosmetic Integration initialized.");
    }
}