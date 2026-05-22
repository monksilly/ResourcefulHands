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
    private class HandState
    {
        public EmoteEntry? CurrentEmote;

        public int EmoteSoundIndex;
        public AudioSource? EmoteAudioSource;

        public Vector3 OriginalOffset;
        public Vector3 BaseScaleFactor;
        public Vector3 OriginalScale;
        public Quaternion OriginalRotation;

        public Vector3 GetOffset(float side)
        {
            if (CurrentEmote != null)
                return new Vector3(CurrentEmote.position.x * -side, CurrentEmote.position.y, CurrentEmote.position.z);

            return OriginalOffset;
        }

        public Vector3 GetScale(Vector3 currentScale)
        {
            if (CurrentEmote != null)
                return Vector3.Lerp(currentScale, Vector3.Scale(CurrentEmote.Scale, BaseScaleFactor), Time.deltaTime * 6f);

            return OriginalScale;
        }

        public Quaternion GetRotation(float side)
        {
            if (CurrentEmote != null)
                return Quaternion.Euler(0, 0, CurrentEmote.Rotation * side);

            return OriginalRotation;
        }

        public AudioClip? GetEmoteSound()
        {
            if (EmoteAudioSource && EmoteAudioSource.isPlaying) EmoteAudioSource.Stop();
            if (CurrentEmote == null) return null;
            if (CurrentEmote.SoundClips == null || CurrentEmote.SoundClips.Count == 0) return null;

            switch (CurrentEmote.SoundPlayMode)
            {
                case SoundPlayMode.Random:
                    return CurrentEmote.SoundClips[Random.Range(0, CurrentEmote.SoundClips.Count)];
                case SoundPlayMode.Sequential:
                    var clip = CurrentEmote.SoundClips[EmoteSoundIndex];
                    EmoteSoundIndex = (EmoteSoundIndex + 1) % CurrentEmote.SoundClips.Count;
                    return clip;
            }

            return null;
        }

        public void StopEmote()
        {
            if (CurrentEmote == null) return;

            ModLogger.Debug("Stopped Emote " + CurrentEmote.name);
            if (CurrentEmote.SoundLoop)
                EmoteAudioSource?.Stop();
            EmoteAudioSource = null;
            CurrentEmote = null;
        }
    }

    private static HandState[]? _handStates;

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
        [HarmonyPatch("Awake")]
        static void AwakePostfix(ENT_Player __instance)
        {
            _handStates = new HandState[__instance.hands.Length];
            for (int i = 0; i < _handStates.Length; i++)
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
                _handStates[hand.id].StopEmote();
                return;
            }

            bool isLeft = hand.id == 0;
            var keyBinds = isLeft ? RHConfig.EmoteKeysLeft : RHConfig.EmoteKeysRight;

            foreach (var cosmetic in hand.currentCosmetics)
            {
                if (!PackManager.HandCosmeticPacksDict.TryGetValue(cosmetic.cosmeticData.id, out var pack))
                    continue;

                if (pack.ExtendedCosmeticData.emotes == null || pack.ExtendedCosmeticData.emotes.Count == 0)
                    continue;

                bool playingEmote = false;
                for (int i = 0; i < Mathf.Min(pack.ExtendedCosmeticData.emotes.Count, RHConfig.MaxEmotes); i++)
                {
                    if (keyBinds[i].Value == KeyCode.None) continue;
                    if (!InputUtility.GetKey(keyBinds[i].Value)) continue;

                    var emote = pack.ExtendedCosmeticData.emotes[i];

                    if (!emote.sprite) continue;

                    bool changedEmote = _handStates[hand.id].CurrentEmote != emote;

                    hand.SetSprite(emote.sprite);
                    _handStates[hand.id].CurrentEmote = emote;
                    if (changedEmote)
                        _handStates[hand.id].EmoteAudioSource = AudioUtility.PlaySound(
                            _handStates[hand.id].GetEmoteSound(), hand.handModel.position, hand.handModel,
                            loop: emote.SoundLoop, bypassEffects: true, spatial:0, mixerType: AudioMixerType.Sfx);

                    playingEmote = true;
                    break;
                }

                if (playingEmote) continue;

                _handStates[hand.id].StopEmote();
            }
        }
    }

    [HarmonyPatch(typeof(ViewSway))]
    public static class ViewSway_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ViewSway.Start))]
        static void StartPostfix(ViewSway __instance)
        {
            if (_handStates == null) return;
            _handStates[__instance.hand.id].OriginalScale = __instance.hand.handModel.localScale;
            _handStates[__instance.hand.id].BaseScaleFactor = __instance.hand.handModel.localScale;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ViewSway.Update))]
        static void UpdatePrefix(ViewSway __instance)
        {
            if (_handStates == null) return;
            var handState = _handStates[__instance.hand.id];
            handState.OriginalOffset = __instance.targetOffset;
            handState.OriginalRotation = __instance.swayRot;

            float handSide = __instance.hand.id == 0 ? -1f : 1f;
            __instance.targetOffset = handState.GetOffset(handSide);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ViewSway.Update))]
        static void UpdatePostfix(ViewSway __instance)
        {
            if (_handStates == null) return;
            var handState = _handStates[__instance.hand.id];
            float handSide = __instance.hand.id == 0 ? -1f : 1f;
            
            __instance.targetOffset = handState.OriginalOffset;
            __instance.transform.localRotation = Quaternion.Lerp(__instance.transform.localRotation,
                handState.GetRotation(handSide) *
                Quaternion.Euler(Vector3.ClampMagnitude(Random.insideUnitSphere * __instance.shakeAmount * 30f,
                    20.5f)) * Quaternion.Euler(0.0f, 0.0f,
                    Mathf.Sin(__instance.rockAmount + Time.time) + __instance.parameters.bobBaseRotation * handSide),
                Time.deltaTime * 6f);
        }
    }
    
    [HarmonyPatch(typeof(ENT_Player.Hand))]
    public static class Hand_Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ENT_Player.Hand.SetScale))]
        static bool UpdatePrefix(ENT_Player.Hand __instance, Vector3 scale)
        {
            if (_handStates == null) return true;
            _handStates[__instance.id].OriginalScale = scale;
            return false;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ENT_Player.Hand.ScaleUpdate))]
        static void LateUpdatePost(ENT_Player.Hand __instance)
        {
            if (_handStates == null) return;
            var handState = _handStates[__instance.id];
            __instance.handModel.localScale = handState.GetScale(__instance.handModel.localScale);
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