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