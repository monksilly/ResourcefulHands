using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ResourcefulHands.Assets;
using ResourcefulHands.Core;
using ResourcefulHands.Systems;
using ResourcefulHands.UI;
using ResourcefulHands.Utility;
using UnityEngine;
using WKLib.API.Audio;
using WKLib.API.Input;
using WKLib.Core.Classes;
using Random = UnityEngine.Random;

namespace ResourcefulHands.Patches;

[HarmonyPatch]
public class ResourcefulHandsPatches
{

    [HarmonyPatch(typeof(CL_CosmeticManager))]
    public static class CL_CosmeticManager_Patches
    {
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
            CosmeticManager.ScanCosmeticsAtPluginsFolder(); // First we add the cosmetics to the base game
            CosmeticManager.GatherCosmetics(); // Then we register all the cosmetics
        }

        [HarmonyPostfix]
        [HarmonyPatch("CreateHandCosmetics")]
        static void CreateHandCosmeticsPostfix(CL_CosmeticManager __instance, string subdir, List<string> jsonList,
            Dictionary<string, Cosmetic_HandItem> ___cosmeticHandDict)
        {
            // Converts the vanilla Cosmetic_HandItem_Data into our extended version
            foreach (string jsonFile in jsonList)
            {
                string json = File.ReadAllText(jsonFile);
                ExtendedHandItemData? extendedCosmeticHandItemData =
                    JsonConvert.DeserializeObject<ExtendedHandItemData>(json);

                if (extendedCosmeticHandItemData == null)
                    continue;

                LoadEmotesAssets(extendedCosmeticHandItemData, subdir);
                CosmeticManager.HandCosmeticExtendedData.Add(extendedCosmeticHandItemData.id, extendedCosmeticHandItemData);
            }
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

        private static void LoadEmotesAssets(ExtendedHandItemData handData, string subdir)
        {
            if (handData.emotes == null || handData.emotes.Count == 0)
                return;

            bool loadLinear = handData.palettes is { Count: > 0 };

            for (int emoteIndex = 0; emoteIndex < handData.emotes.Count; emoteIndex++)
            {
                var emote = handData.emotes[emoteIndex];

                // Loading Sprites
                List<string> spritesToLoad = new List<string>();
                if (!string.IsNullOrEmpty(emote.spriteName))
                    spritesToLoad.Add(emote.spriteName);
                if (emote.SpriteNames is { Count: > 0 })
                    spritesToLoad.AddRange(emote.SpriteNames);

                emote.Sprites = new List<Sprite>();
                foreach (string spriteName in spritesToLoad)
                {
                    var newSprite = RuntimeSpriteImporter.LoadSpriteFromFile(
                        Path.Combine(subdir, "Sprites", spriteName + ".png"), linear: loadLinear);
                    newSprite.name = spriteName;
                    emote.Sprites.Add(newSprite);
                }

                // Loading Sounds
                List<string> soundsToLoad = new List<string>();
                if (!string.IsNullOrEmpty(emote.Sound))
                    soundsToLoad.Add(emote.Sound);
                if (emote.SoundFiles is { Count: > 0 })
                    soundsToLoad.AddRange(emote.SoundFiles);

                emote.SoundClips = new List<AudioClip?>();
                for (int clipIndex = 0; clipIndex < soundsToLoad.Count; clipIndex++)
                {
                    string soundFile = soundsToLoad[clipIndex];
                    emote.SoundClips.Add(null);

                    var index = clipIndex;
                    StaticCoroutine.Start(AudioUtils.LoadAudioClipFromFile(
                        Path.Combine(subdir, "Sounds", soundFile + ".wav"),
                        clip => { emote.SoundClips[index] = clip; }));
                }
            }
        }
    }

    [HarmonyPatch(typeof(ENT_Player))]
    public static class ENT_Player_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch("HandAnimation")]
        public static void HandAnimationPostfix(ENT_Player __instance, ENT_Player.Hand curhand, bool interacting,
            bool canInteract)
        {
            if(HandExtensions.TryGet(curhand, out var handExtension))
                handExtension.ApplySprite();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ENT_Player.Awake))]
        public static void AwakePostfix()
        {
            new GameObject("EmoteWheel").AddComponent<EmoteWheel>();
        }
    }

    [HarmonyPatch(typeof(ViewSway))]
    public static class ViewSway_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ViewSway.Start))]
        static void StartPostfix(ViewSway __instance)
        {
            if(!HandExtensions.TryGet(__instance.hand, out var handExtension)) return;
            handExtension.originalScale = __instance.hand.handModel.localScale;
            if(handExtension.baseScaleFactor == Vector3.zero)
                handExtension.baseScaleFactor = __instance.hand.handModel.localScale;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ViewSway.Update))]
        static void UpdatePrefix(ViewSway __instance)
        {
            if(!HandExtensions.TryGet(__instance.hand, out var handExtension)) return;
            handExtension.originalOffset = __instance.targetOffset;
            handExtension.originalRotation = __instance.swayRot;

            handExtension.ApplyOffset();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ViewSway.Update))]
        static void UpdatePostfix(ViewSway __instance)
        {
            if(!HandExtensions.TryGet(__instance.hand, out var handExtension)) return;
            
            handExtension.ApplyRotation();
            
            __instance.targetOffset = handExtension.originalOffset;
            __instance.swayRot = handExtension.originalRotation;
        }
    }
    
    [HarmonyPatch(typeof(ENT_Player.Hand))]
    public static class Hand_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ENT_Player.Hand.Initialize))]
        static void InitializePost(ENT_Player.Hand __instance)
        {
            __instance.handBase.gameObject.AddComponent<HandExtensions>();
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ENT_Player.Hand.SetScale))]
        static bool UpdatePrefix(ENT_Player.Hand __instance, Vector3 scale)
        {
            if(!HandExtensions.TryGet(__instance, out var handExtension)) return true;
            handExtension.originalScale = scale;
            return false;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ENT_Player.Hand.ScaleUpdate))]
        static void LateUpdatePost(ENT_Player.Hand __instance)
        {
            if(!HandExtensions.TryGet(__instance, out var handExtension)) return;
            handExtension.ApplyScale();
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