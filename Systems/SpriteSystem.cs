using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourcefulHands.Patches;
using ResourcefulHands.Core;

namespace ResourcefulHands.Systems;

public static class SpriteSystem
{
    private static SpriteRenderer[] _activeRenderers = [];
    private static float _lastDiscoveryTime;
    private const float DiscoveryInterval = 1.0f / 60.0f; // 60fps polling

    public static void StartReplacementThreads()
    {
        if (RHConfig.UseOldSprReplace)
        {
            ModLogger.Log.LogInfo("Using Legacy Sprite Replacement.");
            CoroutineDispatcher.AddToUpdate(UpdateSpritesLegacy);
        }
        else
        {
            ModLogger.Log.LogInfo("Starting Modern Sprite Discovery Service.");
            CoroutineDispatcher.Dispatch(SpriteDiscoveryRoutine());
            CoroutineDispatcher.AddToUpdate(UpdateSpritesModern);
        }
    }

    /// <summary>
    /// This is where you'd hook into the game's official cosmetics.
    /// Call this whenever a cosmetic change happens.
    /// </summary>
    public static void SyncWithOfficialCosmetics()
    {
        // Example: If the game has a 'CosmeticManager', find it and apply your textures
        var cosmeticPage = Object.FindAnyObjectByType<OF_CosmeticPage>();
        if (cosmeticPage != null)
        {
            ModLogger.Log.LogDebug("Syncing with Official Cosmetic System...");
            // Add logic here to inject your custom textures into the official slots
        }
    }

    private static IEnumerator SpriteDiscoveryRoutine()
    {
        while (true)
        {
            _activeRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            _lastDiscoveryTime = Time.time;
            yield return new WaitForSeconds(DiscoveryInterval);
        }
    }

    private static void UpdateSpritesModern()
    {
        // Safety check: if the discovery routine dies, restart it
        if (Time.time - _lastDiscoveryTime > DiscoveryInterval * 32.0f)
        {
            ModLogger.Log.LogWarning("Sprite discovery thread stalled. Restarting...");
            CoroutineDispatcher.Dispatch(SpriteDiscoveryRoutine());
        }

        foreach (var sr in _activeRenderers)
        {
            if (sr != null) SpriteRendererPatches.Patch(sr);
        }
    }

    private static void UpdateSpritesLegacy()
    {
        var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var sr in renderers)
        {
            SpriteRendererPatches.Patch(sr);
        }
    }
}