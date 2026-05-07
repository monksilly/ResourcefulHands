using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using ResourcefulHands.Core;
using ResourcefulHands.Systems;
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
        // Prevent Processing the same folder more than once
        private static readonly HashSet<string> ProcessedFolders = [];
        
        [HarmonyPrefix]
        [HarmonyPatch("ScanForCosmetics")]
        public static bool ScanForCosmeticsPrefix()
        {
            FixStructures();

            return true;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch("ScanForCosmetics")]
        public static void ScanForCosmeticsPostfix()
        {
            ScanHandCosmetics();
            ScanVoiceCosmetics();
        }

        private static void FixStructures()
        {
            ModLogger.Info("Harmony Patch: Starting Cosmetic Structure Fix..."); 

            string? pluginsPath = Path.GetDirectoryName(BepInEx.Paths.PluginPath);

            if (!Directory.Exists(pluginsPath)) return;
        
            // Find all directories that contain the specific JSON
            var allDirectories = Directory.GetDirectories(pluginsPath, "*", SearchOption.AllDirectories);
            
            foreach (var dir in allDirectories)
            {
                if (File.Exists(Path.Combine(dir, "cosmetic-handitem-settings.json")))
                {
                    CosmeticStructureRepairer.FixModStructure(dir);
                }
            }
        }
        
        private static void ScanHandCosmetics()
        {
            string pluginsPath = BepInEx.Paths.PluginPath;
            
            if (!Directory.Exists(pluginsPath)) return;
            
            string[] pluginFolders =  Directory.GetDirectories(pluginsPath, "*", SearchOption.AllDirectories);
            
            Debug.Log("Scanning For Hand Cosmetics Scattered Across BepInEx Plugins...");
            
            var methodInfo = AccessTools.Method(typeof(CL_CosmeticManager), "CreateHandCosmetics");
            var actionDelegate = AccessTools.MethodDelegate<Action<string, List<string>>>(methodInfo);
            

            foreach (var pluginFolder in pluginFolders)
            {
                if (File.Exists(Path.Combine(pluginFolder, "cosmetic-handitem-settings.json")))
                {
                    string parentFolder = Directory.GetParent(pluginFolder)!.FullName;

                    var scanResult = CL_CosmeticManager.ScanSubfoldersForJson(
                        parentFolder, 
                        actionDelegate, 
                        "cosmetic-handitem-settings.json"
                    );
                    ModLogger.Info($"Found pack at {pluginFolder}");
                }
            }
            
            Debug.Log("Finished Scanning For Hand Cosmetics From BepInEx Plugins.");
        }

        private static void ScanVoiceCosmetics()
        {
            string pluginsPath = BepInEx.Paths.PluginPath;
            
            if (!Directory.Exists(pluginsPath)) return;
            
            string[] pluginFolders =  Directory.GetDirectories(pluginsPath, "*", SearchOption.AllDirectories);
            
            Debug.Log("Scanning For Voice Cosmetics Scattered Across BepInEx Plugins...");
            
            var methodInfo = AccessTools.Method(typeof(CL_CosmeticManager), "CreateVoiceCosmetics");
            var actionDelegate = AccessTools.MethodDelegate<Action<string, List<string>>>(methodInfo);
            

            foreach (var pluginFolder in pluginFolders)
            {
                if (File.Exists(Path.Combine(pluginFolder, "cosmetic-voice-settings.json")))
                {
                    string parentFolder = Directory.GetParent(pluginFolder)!.FullName;
                    
                    var scanResult = CL_CosmeticManager.ScanSubfoldersForJson(
                        parentFolder, 
                        actionDelegate, 
                        "cosmetic-voice-settings.json"
                    );
                    
                    ModLogger.Info("Found Voice Pack at: " + pluginFolder);
                }
            }
            
            Debug.Log("Finished Scanning For Voice Cosmetics From BepInEx Plugins.");
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