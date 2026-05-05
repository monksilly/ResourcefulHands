using System.Collections.Generic;
using ResourcefulHands.Patches;
using ResourcefulHands.Systems;
using UnityEngine;

namespace ResourcefulHands.Assets;

public static class AssetRefresher
{
    public static List<AudioSource> AllAudioSources = [];
    
    public static void RefreshAll()
    {
        RefreshTextures();
        RefreshSounds();
    }
    
    public static void RefreshTextures()
    {
        RHSpriteManager.ClearSpriteCache();
    }
    
    private static void RefreshSounds()
    {

        foreach (var audioSource in AllAudioSources)
        {
            // TODO: Remove if not needed
            //AudioSourcePatches.SwapClip(audioSource);
            if (audioSource.isPlaying && audioSource.time < 0.1 && audioSource.enabled &&
                audioSource.gameObject.activeInHierarchy)
                RHDebugTools.QueueSound(audioSource.clip);
        }
    }
}