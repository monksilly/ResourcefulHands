using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using ResourcefulHands.Assets;
using ResourcefulHands.Core;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable InconsistentNaming
// for harmony special method/param names

// NOTE: some debug related patches are in DebugTools.cs
namespace ResourcefulHands.Patches;

// thanks McArdellje
[HarmonyPatch(typeof(Image))]
public static class ImagePatches
{
    [HarmonyPatch("activeSprite", MethodType.Getter)]
    [HarmonyPostfix]
    public static void Getter_sprite_Postfix(Image __instance, ref Sprite __result) {
        // TODO: fix left/right ui sprites not working
        if (!__result)
            return;
        
        if (__result.texture.name == "hand-sheet")
        {
            // TODO: Temporary block
            return;
            // cache the original texture
            OriginalAssetTracker.textures.TryAdd(__result.texture.name, __result.texture);

            var spriteTexName = __result.texture.name;
            var handId = string.Equals(__instance.gameObject.name, "Interact_L", StringComparison.CurrentCultureIgnoreCase) ? 0 : 1;
            
            var prefix = RHSpriteManager.GetHandPrefix(handId);
            var newSpriteTexName = spriteTexName;
            
            // if there isnt a pack associated to a l/r hand then dont replace the l/r hand
            if ((RHConfig.PackPrefs.GetLeftHandPack() == null && handId == 0)||
                (RHConfig.PackPrefs.GetRightHandPack() == null && handId == 1))
            {
                var originalSpr = OriginalAssetTracker.GetFirstSpriteFromTextureName(spriteTexName);
                if(originalSpr is not null)
                    __result = originalSpr;
                return;
            }
            
            if(!newSpriteTexName.StartsWith(prefix))
                newSpriteTexName = prefix + newSpriteTexName;

            string oldName = __result.name;
            if (!__result.name.StartsWith(prefix))
                __result.name = prefix + __result.name;
            
            ResourcePack? myPack = handId == 0 ? RHConfig.PackPrefs.GetLeftHandPack() : RHConfig.PackPrefs.GetRightHandPack();
            Sprite? newSpr = RHSpriteManager.GetReplacementSprite(__result, newSpriteTexName);
            __result.name = oldName;
            
            if (myPack != null && !(myPack.Textures.ContainsKey(newSpriteTexName) || myPack.Textures.ContainsKey(spriteTexName)))
            {
                Sprite? originalSpr = OriginalAssetTracker.GetFirstSpriteFromTextureName(spriteTexName);
                if(originalSpr is not null)
                    __result = originalSpr;
                return;
            }
            if (newSpr is not null && newSpr != __result)
            {
                __result = newSpr;
                return;
            }
        }
        
        __result = RHSpriteManager.GetReplacementSprite(__result) ?? __result;
    }
}

[HarmonyPatch(typeof(AudioSource))]
internal static class AudioSourcePatches
{
    private static void Cache(AudioClip? clip)
    {
        if(clip == null) 
            return;

        OriginalAssetTracker.sounds.TryAdd(clip.name, clip);
    }
    
    // Setters and Getters for clip are not needed,
    // Patching the play functions is the better and 100% working way
    
    // Patch parameterless Play()
    [HarmonyPatch(nameof(AudioSource.Play), [])]
    [HarmonyPrefix]
    private static void Play_NoArgs_Postfix(AudioSource __instance)
        => SwapClip(__instance);

    // Patch Play(double delay)
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(double) })]
    [HarmonyPrefix]
    private static void Play_DelayDouble_Postfix(AudioSource __instance)
        => SwapClip(__instance);

    // Patch Play(ulong delaySamples)
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(ulong) })]
    [HarmonyPrefix]
    private static void Play_DelayUlong_Postfix(AudioSource __instance)
        => SwapClip(__instance);
    
    // Patch PlayOneShot(AudioClip)
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip))]
    [HarmonyPrefix]
    private static void PlayOneShot_ClipOnly_Postfix(AudioSource __instance, ref AudioClip __0)
    {
        // if the original is already cached this will just silently fail
        Cache(__instance.clip);
        
        var clip = ResourcePacksManager.GetSoundFromPacks(__instance.clip.name);
        if (clip is not null)
            __0 = clip;
    }

    // Patch PlayOneShot(AudioClip, float volumeScale)
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip), typeof(float))]
    [HarmonyPrefix]
    private static void PlayOneShot_ClipAndVolume_Postfix(AudioSource __instance, ref AudioClip __0)
    {
        // if the original is already cached this will just silently fail
        Cache(__instance.clip);
        
        var clip = ResourcePacksManager.GetSoundFromPacks(__instance.clip.name);
        if (clip is not null)
            __0 = clip;
    }
    
    // Shared logic
    internal static void SwapClip(AudioSource src)
    {
        if (src?.clip is null) 
            return;

        // if the original is already cached this will just silently fail
        Cache(src.clip);
        var clip = ResourcePacksManager.GetSoundFromPacks(src.clip.name);
        if (clip is null) 
            return;

        src.clip = clip;
    }
}