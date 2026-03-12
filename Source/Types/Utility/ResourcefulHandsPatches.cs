using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ResourcefulHands;

[HarmonyPatch]
public class ResourcefulHandsPatches
{
    public static Traverse Trv(object obj) => Traverse.Create(obj);
    
    [HarmonyPatch(typeof(CL_CosmeticManager))]
    public static class CL_CosmeticManager_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(CL_CosmeticManager.Initialize))]
        public static void Postfix()
        {
            // Trigger the Vanilla Scan with a delay to ensure lists are allocated
            StaticCoroutine.Start(DelayedVanillaScan());
            
            if (OF_CosmeticPage.instance && OF_CosmeticPage.instance.IsReady)
            {
                InjectToManager();
            }
        }

        private static System.Collections.IEnumerator DelayedVanillaScan()
        {
            RHLog.Info("[VanillaScan] Waiting for Manager to fully populate lists...");
            var manager = Trv(typeof(CL_CosmeticManager));
    
            // Wait until the game has actually finished its own AddRange calls
            // We check loadedCosmetics because it's the last one initialized
            float timer = 0;
            while (timer < 5f) // Cap at 5 seconds so we don't loop forever if something breaks
            {
                var loadedList = manager.Field<List<Cosmetic_Base>>("loadedCosmetics").Value;
                if (loadedList != null && loadedList.Count > 0)
                {
                    RHLog.Info($"[VanillaScan] Manager is ready with {loadedList.Count} items. Starting scan...");
                    ScanVanillaCosmeticsInPlugins();
                    yield break;
                }
                timer += 0.2f;
                yield return new WaitForSeconds(0.2f);
            }
            RHLog.Error("[VanillaScan] Timed out waiting for CL_CosmeticManager!");
        }

        public static void ScanVanillaCosmeticsInPlugins()
        {
            string pluginPath = BepInEx.Paths.PluginPath;
            RHLog.Info($"[VanillaScan] Searching {pluginPath}...");

            if (!Directory.Exists(pluginPath))
            {
                RHLog.Error($"[VanillaScan] Directory does not exist: {pluginPath}");
                return;
            }

            // Use AllDirectories to ensure we find it even if nested
            string[] jsonFiles;
            try 
            {
                jsonFiles = Directory.GetFiles(pluginPath, "cosmetic-handitem-settings.json", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                RHLog.Error($"[VanillaScan] Failed to read filesystem: {ex.Message}");
                return;
            }

            RHLog.Info($"[VanillaScan] Found {jsonFiles.Length} potential vanilla hand JSONs.");

            foreach (string jsonPath in jsonFiles)
            {
                // Get the directory containing the JSON
                string folder = Path.GetDirectoryName(jsonPath);
                RHLog.Info($"[VanillaScan] Attempting load from: {folder}");
        
                LoadVanillaHand(folder, jsonPath);
            }
        }
        
        private static void LoadVanillaHand(string subdir, string jsonPath)
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                var data = JsonUtility.FromJson<Cosmetic_HandItem.Cosmetic_HandItem_Data>(jsonContent);
                if (data == null || string.IsNullOrEmpty(data.id)) return;

                // Use Traverse with explicit private access
                var manager = Traverse.Create(typeof(CL_CosmeticManager));
                
                var handsDict = manager.Field<Dictionary<string, Cosmetic_HandItem>>("cosmeticHandDict").Value;
                var handsList = manager.Field<List<Cosmetic_HandItem>>("cosmeticHands").Value;
                var loadedList = manager.Field<List<Cosmetic_Base>>("loadedCosmetics").Value;

                if (handsDict == null || handsList == null || loadedList == null)
                {
                    RHLog.Error("[VanillaScan] Lists went null during the load call. This shouldn't happen.");
                    return;
                }

                if (handsDict.ContainsKey(data.id)) return;

                // Create Instance
                Cosmetic_HandItem item = ScriptableObject.CreateInstance<Cosmetic_HandItem>();
                item.cosmeticData = data;
                item.showInCosmeticsWindow = true; 
                
                item.cosmeticInfo = new Cosmetic_Info {
                    id = data.id,
                    cosmeticName = data.cosmeticName,
                    tag = "hand",
                    author = data.author,
                    description = data.description,
                    unlock = data.unlock
                };

                // Load card textures (Using false for absolute BepInEx paths)
                try
                {
                    item.cosmeticInfo.cardBackground = RuntimeSpriteImporter.LoadSpriteFromFile(
                        Path.Combine(subdir, "card-background.png"), 100f, FilterMode.Bilinear, true, false);
                }
                catch
                {
                    RHLog.Warning($"Pack {item.cosmeticInfo.cosmeticName}'s card-background failed to load");
                }

                try
                {
                    item.cosmeticInfo.cardForeground = RuntimeSpriteImporter.LoadSpriteFromFile(
                        Path.Combine(subdir, "card-foreground.png"), 100f, FilterMode.Bilinear, true, false);
                }
                catch
                {
                    RHLog.Warning($"Pack {item.cosmeticInfo.cosmeticName}'s card-foreground failed to load");
                }

                // Call internal Initialize
                Traverse.Create(item).Method("Initialize").GetValue();

                // Match the decompiler's manual sprite mapping
                var spriteRes = RuntimeSpriteImporter.LoadSpritesFromFolder(Path.Combine(subdir, "Sprites"), false);
                var interactRes = RuntimeSpriteImporter.LoadSpritesFromFolder(Path.Combine(subdir, "Interacts"), false);

                if (item.cosmeticData.swapSprites != null && spriteRes.spriteDict != null)
                {
                    foreach (var swap in item.cosmeticData.swapSprites)
                    {
                        swap.replacementSprites = new List<Sprite>();
                        if (swap.replacementSpriteNames == null) continue;
                        foreach (var name in swap.replacementSpriteNames)
                        {
                            if (spriteRes.spriteDict.TryGetValue(name, out var s))
                                swap.replacementSprites.Add(s);
                        }
                    }
                }

                if (item.cosmeticData.interactSwaps != null && interactRes.spriteDict != null)
                {
                    foreach (var iSwap in item.cosmeticData.interactSwaps)
                    {
                        if (interactRes.spriteDict.TryGetValue(iSwap.replacementSpriteName, out var s))
                            iSwap.replacementSprite = s;
                    }
                }

                // Add to the Game's Lists
                handsDict.Add(data.id, item);
                handsList.Add(item);
                loadedList.Add(item); 
                
                // Final sync with save data
                if (SettingsManager.settings?.cosmeticSaveData != null)
                    SettingsManager.settings.cosmeticSaveData.FillNewCosmeticInfo(item);

                RHLog.Info($"[VanillaScan] Successfully injected: {data.cosmeticName}");
            }
            catch (Exception ex)
            {
                RHLog.Error($"[VanillaScan] Failed loading {subdir}: {ex}");
            }
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
                RHLog.Info("RH Cosmetics dynamically injected into UI.");
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