using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using ResourcefulHands.Core;
using ResourcefulHands.UI;
using UnityEngine;

namespace ResourcefulHands.Patches;


[HarmonyPatch]
public class ResourcefulHandsPatches
{
    public static Traverse Trv(object obj) => Traverse.Create(obj);
    
    [HarmonyPatch(typeof(CL_CosmeticManager))]
    public static class CL_CosmeticManager_Patches
    {
        
        [HarmonyPostfix]
        [HarmonyPatch("ScanForCosmetics")]
        public static void ScanForCosmeticsPostfix()
        {
            ScanHandCosmetics();
            ScanVoiceCosmetics();
        }

        private static void ScanHandCosmetics()
        {
            string cosmeticFolderPath = BepInEx.Paths.PluginPath;
            Debug.Log("Scanning For Hand Cosmetics Scattered Across BepInEx Plugins...");
            
            var methodInfo = AccessTools.Method(typeof(CL_CosmeticManager), "CreateHandCosmetics");
            var actionDelegate = AccessTools.MethodDelegate<Action<string, List<string>>>(methodInfo);
            
            var scanResult = CL_CosmeticManager.ScanSubfoldersForJson(
                cosmeticFolderPath, 
                actionDelegate, 
                "cosmetic-handitem-settings.json"
            );
            Debug.Log("Finished Scanning For Hand Cosmetics From BepInEx Plugins: " + scanResult);
        }

        private static void ScanVoiceCosmetics()
        {
            string cosmeticFolderPath = BepInEx.Paths.PluginPath;
            Debug.Log("Scanning For Voice Cosmetics Scattered Across BepInEx Plugins...");
            
            var methodInfo = AccessTools.Method(typeof(CL_CosmeticManager), "CreateVoiceCosmetics");
            var actionDelegate = AccessTools.MethodDelegate<Action<string, List<string>>>(methodInfo);
            
            var scanResult = CL_CosmeticManager.ScanSubfoldersForJson(
                cosmeticFolderPath, 
                actionDelegate, 
                "cosmetic-voice-settings.json"
            );
            Debug.Log("Finished Scanning For Voice Cosmetics From BepInEx Plugins: " + scanResult);
        }
        
    }
}

public class StaticCoroutine : MonoBehaviour 
{
    private static StaticCoroutine _instance = null!;
    public static void Start(System.Collections.IEnumerator routine)
    {
        if (_instance == null)
        {
            _instance = new GameObject("RH_StaticCoroutine").AddComponent<StaticCoroutine>();
            DontDestroyOnLoad(_instance.gameObject);
        }
        _instance.StartCoroutine(routine);
    }
}