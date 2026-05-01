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
            string cosmeticFolderPath = BepInEx.Paths.PluginPath;
            Debug.Log("Scanning For Hand Cosmetics Scattered Across BepInEx Plugins...");
            
            var methodInfo = AccessTools.Method(typeof(CL_CosmeticManager), "CreateHandCosmetics");
            var actionDelegate = AccessTools.MethodDelegate<Action<string, List<string>>>(methodInfo);
            
            var scanResult = CL_CosmeticManager.ScanSubfoldersForJson(
                cosmeticFolderPath, 
                actionDelegate, 
                "cosmetic-handitem-settings.json"
            );
            Debug.Log("Finished Scanning For Hand Cosmetics From BepInEx Plugins: " + scanResult?.ToString());
            
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CL_CosmeticManager.Initialize))]
        public static void PostfixInitialize()
        {
            // TODO: Remove completely, as unneeded
            // Trigger the Vanilla Scan with a delay to ensure lists are allocated
            // StaticCoroutine.Start(DelayedVanillaScan());
            
            if (OF_CosmeticPage.instance && OF_CosmeticPage.instance.IsReady)
            {
                InjectToManager();
            }
        }

        private static System.Collections.IEnumerator DelayedVanillaScan()
        {
            ModLogger.Info("[VanillaScan] Waiting for Manager to fully populate lists...");
            var manager = Trv(typeof(CL_CosmeticManager));
    
            // Wait until the game has actually finished its own AddRange calls
            // We check loadedCosmetics because it's the last one initialized
            float timer = 0;
            while (timer < 5f) // Cap at 5 seconds so we don't loop forever if something breaks
            {
                var loadedList = manager.Field<List<Cosmetic_Base>>("loadedCosmetics").Value;
                if (loadedList != null && loadedList.Count > 0)
                {
                    ModLogger.Info($"[VanillaScan] Manager is ready with {loadedList.Count} items. Starting scan...");
                    // TODO: Remove this
                    // ScanVanillaCosmeticsInPlugins();
                    yield break;
                }
                timer += 0.2f;
                yield return new WaitForSeconds(0.2f);
            }
            ModLogger.Error("[VanillaScan] Timed out waiting for CL_CosmeticManager!");
        }
        
        public static void InjectToManager()
        {
            var manager = Traverse.Create(typeof(CL_CosmeticManager));
            var handsDict = manager.Field<Dictionary<string, Cosmetic_HandItem>>("cosmeticHandDict").Value;
            var handsList = manager.Field<List<Cosmetic_HandItem>>("cosmeticHands").Value;
            var loadedList = manager.Field<List<Cosmetic_Base>>("loadedCosmetics").Value;

            if (handsDict == null) return;

            foreach (var hand in OF_CosmeticPage.instance.RHHands)
            {
                if (hand is Cosmetic_HandItem handItem && !handsDict.ContainsKey(handItem.cosmeticInfo.id))
                {
                    handsList.Add(handItem);
                    handsDict.Add(handItem.cosmeticInfo.id, handItem);
                    loadedList.Add(handItem);
                    Traverse.Create(handItem).Method("Initialize").GetValue();
                    SettingsManager.settings.cosmeticSaveData.FillNewCosmeticInfo(handItem);
                }
            }
        }
    }

    [HarmonyPatch(typeof(UI_CosmeticsMenu))]
    public static class UI_CosmeticsMenu_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        public static void Postfix(UI_CosmeticsMenu __instance)
        {
            StaticCoroutine.Start(WaitForRHReady(__instance));
        }

        private static System.Collections.IEnumerator WaitForRHReady(UI_CosmeticsMenu menu)
        {
            while (OF_CosmeticPage.instance == null || !OF_CosmeticPage.instance.IsReady)
            {
                yield return new WaitForSeconds(0.1f);
            }

            CL_CosmeticManager_Patches.InjectToManager();

            var handPageTemplate = menu.cosmeticPages.Find(p => p.cosmeticType == "hand");
            if (handPageTemplate != null)
            {
                var rhPage = new UI_CosmeticsMenu.CosmeticPage
                {
                    name = "Only RH",
                    cosmeticType = "rh_custom",
                    pageHolder = handPageTemplate.pageHolder,
                };

                menu.cosmeticPages.Add(rhPage);
                menu.FillCosmeticPage(OF_CosmeticPage.instance.RHHands, "Only RH", rhPage);
                ModLogger.Info("RH Cosmetics dynamically injected into UI.");
            }
        }
    }
}

public class StaticCoroutine : MonoBehaviour 
{
    private static StaticCoroutine _instance;
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