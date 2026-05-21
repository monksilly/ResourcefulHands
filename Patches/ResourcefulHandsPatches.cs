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
using WKLib.API.Input;

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
            PackManager.ScanCosmeticsAtPluginsFolder(); // First we add the cosmetics to the base game
            PackManager.GatherCosmetics(); // Then we register all the cosmetics
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
                PackManager.HandCosmeticExtendedData.Add(extendedCosmeticHandItemData.id, extendedCosmeticHandItemData);
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

                if (string.IsNullOrEmpty(emote.spriteName))
                    continue;

                var emoteSprite = RuntimeSpriteImporter.LoadSpriteFromFile(
                    Path.Combine(subdir, "Sprites", emote.spriteName + ".png"), linear: loadLinear);
                emoteSprite.name = emote.spriteName;

                emote.sprite = emoteSprite;

                if (!string.IsNullOrEmpty(emote.Sound))
                {
                    StaticCoroutine.Start(AudioUtils.LoadAudioClipFromFile(
                        Path.Combine(subdir, "Sounds", emote.Sound + ".wav"),
                        clip => { emote.SoundClip = clip; }));
                }
            }
        }
    }

    [HarmonyPatch(typeof(ENT_Player))]
    public static class ENT_Player_Patches
    {
        private class HandState
        {
            public int CurrentEmote = -1;
            public Vector3 EmoteOffset;
        }

        private static HandState[]? _handStates;
        
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        static void AwakePostfix(ENT_Player __instance)
        {
            _handStates = new HandState[__instance.hands.Length];
            for(int i=0 ;i<_handStates.Length;i++)
                _handStates[i] = new HandState();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch("HandAnimation")]
        public static void HandAnimationPostfix(ENT_Player __instance, ENT_Player.Hand curhand, bool interacting,
            bool canInteract)
        {
            ApplyEmotes(curhand, interacting, canInteract);
        }

        private static void ApplyEmotes(ENT_Player.Hand hand, bool interacting, bool canInteract)
        {
            if (hand.currentCosmetics == null || hand.currentCosmetics.Count == 0 || _handStates == null)
                return;
            
            if (interacting || !canInteract || !hand.IsFree())
            {
                if (_handStates[hand.id].CurrentEmote != -1)
                {
                    if(_handStates[hand.id].EmoteOffset == hand.GetViewSway().targetOffset)
                        hand.GetViewSway().targetOffset = Vector3.zero;
                    _handStates[hand.id].CurrentEmote = -1;
                }

                return;
            }

            bool isLeft = hand.id == 0;
            var keyBinds = isLeft ? RHConfig.EmoteKeysLeft : RHConfig.EmoteKeysRight;

            foreach (var cosmetic in hand.currentCosmetics)
            {
                if(!PackManager.HandCosmeticPacksDict.TryGetValue(cosmetic.cosmeticData.id, out var pack))
                    continue;

                if (pack.ExtendedCosmeticData.emotes == null || pack.ExtendedCosmeticData.emotes.Count == 0)
                    continue;

                bool playingEmote = false;
                for (int i = 0; i < Mathf.Min(pack.ExtendedCosmeticData.emotes.Count, RHConfig.MaxEmotes); i++)
                {
                    if (keyBinds[i].Value == KeyCode.None) continue;
                    if (!InputUtility.GetKeyDown(keyBinds[i].Value)) continue;

                    var emote = pack.ExtendedCosmeticData.emotes[i];

                    hand.SetSprite(emote.sprite);
                    hand.GetViewSway().targetOffset =
                        Vector3.Scale(emote.position, hand.handSprite.transform.localScale);

                    if (emote.SoundClip && _handStates[hand.id].CurrentEmote == -1)
                        AudioManager.PlaySound(emote.SoundClip, hand.handModel);

                    _handStates[hand.id].CurrentEmote = i;
                    _handStates[hand.id].EmoteOffset = hand.GetViewSway().targetOffset;
                    playingEmote = true;
                    break;
                }

                if (!playingEmote)
                {
                    if (_handStates[hand.id].CurrentEmote != -1)
                    {
                        if(_handStates[hand.id].EmoteOffset == hand.GetViewSway().targetOffset)
                            hand.GetViewSway().targetOffset = Vector3.zero;
                        _handStates[hand.id].CurrentEmote = -1;
                    }
                }
            }
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