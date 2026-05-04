using System.Collections.Generic;
using HarmonyLib;
using ResourcefulHands.Core;
using ResourcefulHands.Systems;
using UnityEngine;

namespace ResourcefulHands.Patches;

[HarmonyPatch]
public class UT_AudioClipHandlerPatch
{
    [HarmonyPatch(typeof(UT_AudioClipHandler), nameof(UT_AudioClipHandler.Initialize)), HarmonyPostfix]
    public static void UT_AudioClipHandler_Initialize(UT_AudioClipHandler __instance)
    {
	    if (!ConfigManager.EnableAudioOverrides)
		    return;
	    
	    ModLogger.Debug(__instance.gameObject.name + ": Initialize");
	    if (!CL_CosmeticManager.initialized)
	    {
			ModLogger.Debug(__instance.gameObject.name + ": Cosmetic system not initialized.");
			return;
	    }
        
        if (__instance.overrides == null)
        {
	        __instance.overrides = new List<AudioClipHandlerOverride>();
        }

        if (__instance.groupDictionary == null)
        {
		    ModLogger.Debug(__instance.gameObject.name + ": GroupDictionary is null");
	        return;
        }
        
        var newClipHandlerOverride = new AudioClipHandlerOverride
        {
	        setOverrides = new List<AudioClipHandlerOverride.AudioSetOverride>()
        };
        
        foreach (Cosmetic_Voice cosmetic_Voice in CL_CosmeticManager.GetActiveVoiceCosmetics())
        {
	        var sets = cosmetic_Voice?.cosmeticData?.clipHandlerOverride?.setOverrides;
	        if (sets == null)
	        {
			    ModLogger.Debug(__instance.gameObject.name + ": " + $"{cosmetic_Voice.cosmeticInfo.id} audio set is null");
		        continue;
	        }
	        
	        foreach (var audioSet in cosmetic_Voice.cosmeticData.clipHandlerOverride.setOverrides)
	        {
		        if (__instance.groupDictionary.ContainsKey(audioSet.groupName))
		        {
				    ModLogger.Debug(__instance.gameObject.name + ": Added audio override " + $"{audioSet.groupName}:{audioSet.setName}");
			        newClipHandlerOverride.setOverrides.Add(audioSet);
		        }
	        }
        }

	    ModLogger.Debug(__instance.gameObject.name + ": Added overrides");
        __instance.overrides.Add(newClipHandlerOverride);
    }
}