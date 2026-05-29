using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using ResourcefulHands.Assets;
using ResourcefulHands.Systems;
using UnityEngine;

namespace ResourcefulHands.Core;

public static class RHConfig
{
    // --- GENERAL ---
    // Lazy loading
    public static ConfigEntry<bool>? LazyManip = null;
    // Use old sprite replacer
    public static ConfigEntry<bool>? UseOldSprReplace = null;
    // Don't load outdated packs
    public static ConfigEntry<bool>? UseOutdatedPacks = null;
    // Expand vanilla audio overrides
    public static ConfigEntry<bool>? ExpandVanillaAudioOverrides = null;
    
    // --- DEBUG STUFF ---
    // Colored console
    public static ConfigEntry<bool>? ColorConsole = null;
    // Always debug mode
    public static ConfigEntry<bool>? AlwaysDebug = null;
    
    public const int MaxEmotes = 10;

    public static ConfigEntry<KeyCode> EmoteWheelKey { get; private set; }
    public static ConfigEntry<KeyCode> EmoteWheelKeyAlt { get; private set; }
    public static ConfigEntry<bool> ToggleWheel { get; private set; }
    public static ConfigEntry<KeyCode> EmoteLeftKey { get; private set; }
    public static ConfigEntry<KeyCode> EmoteLeftKeyAlt { get; private set; }
    public static ConfigEntry<KeyCode> EmoteRightKey { get; private set; }
    public static ConfigEntry<KeyCode> EmoteRightKeyAlt { get; private set; }
    public static ConfigEntry<bool> ToggleEmotes { get; private set; }
    public static ConfigEntry<string> LeftEmote { get; private set; }
    public static ConfigEntry<string> RightEmote { get; private set; }
    public static ConfigEntry<float> EmoteVolume { get; private set; }


    // Config folder stuff
    public static string PacksFolder => Path.Combine(Paths.ConfigPath, "RHPacks");
    public static string GenericFolder => Path.Combine(Paths.ConfigPath, "RHConfig");

    public static class PackPrefs
    {
        [System.Serializable]
        internal class PrefsObject
        {
            [JsonProperty(NullValueHandling=NullValueHandling.Include)]
            public string[] disabledPacks = [];
            [JsonProperty(NullValueHandling=NullValueHandling.Include)]
            public string[] packOrder = [];
            [JsonProperty(NullValueHandling=NullValueHandling.Include)]
            public string leftHandPack = string.Empty;
            [JsonProperty(NullValueHandling=NullValueHandling.Include)]
            public string rightHandPack = string.Empty;
            
            public static PrefsObject? FromJson(string json) => JsonConvert.DeserializeObject<PrefsObject>(json);
            public string ToJson() => JsonConvert.SerializeObject(this);
        }
        
        public static string[] DisabledPacks = [];
        public static string[] PackOrder = [];
        
        public static string LeftHandPack = string.Empty;
        public static ResourcePack? GetLeftHandPack()
        {
            return ResourcePacksManager.LoadedPacks.FirstOrDefault(pack => pack.guid == LeftHandPack && pack.IsActive);
        }
        
        public static string RightHandPack = string.Empty;
        public static ResourcePack? GetRightHandPack()
        {
            return ResourcePacksManager.LoadedPacks.FirstOrDefault(pack => pack.guid == RightHandPack && pack.IsActive);
        }

        internal static string GetFile()
        {
            string path = Path.Combine(GenericFolder, "prefs.json");
            if(!File.Exists(path))
                File.WriteAllText(path, "");

            return path;
        }
        
        public static void Load()
        {
            string path = GetFile();
            
            PrefsObject prefs = PrefsObject.FromJson(File.ReadAllText(path)) ?? new PrefsObject();
            DisabledPacks = prefs.disabledPacks;
            PackOrder = prefs.packOrder;
            LeftHandPack = prefs.leftHandPack;
            RightHandPack = prefs.rightHandPack;
            
            // apparently this was a quickfix but i don't see anything wrong with it at a glance so
            // goodbye to-do!
            if (!string.IsNullOrEmpty(LeftHandPack))
                RHSpriteManager.OverrideHands(LeftHandPack, true);
            if (!string.IsNullOrEmpty(RightHandPack))
                RHSpriteManager.OverrideHands(RightHandPack, false);
        }

        public static void Save()
        {
            string path = GetFile();
            
            PrefsObject prefs = new PrefsObject
            {
                disabledPacks = DisabledPacks,
                packOrder = PackOrder,
                leftHandPack = LeftHandPack,
                rightHandPack = RightHandPack
            };
            
            File.WriteAllText(path, prefs.ToJson());
        }
    }
    
    internal static void InitConfigs(ConfigFile Config)
    {
        ModLogger.Info("Initialising configs...");
        
        // Bind configs
        ModLogger.Debug("Binding configs with bepinex...");
        
        // General
        LazyManip = Config.Bind(
            "General",
            "Lazy Loading",
            true,
            $"When enabled every pack doesn't get reloaded when reordering or enabling/disabling packs in the settings menu."
        );
        ModLogger.Debug("Bound LazyManip");
        UseOldSprReplace = Config.Bind(
            "General",
            "Use Old Sprite Replacer",
            false,
            $"A new sprite replacer (the thing that lets you have custom hands) has been added, hopefully this should improve performance. However, if you do have issues with this new replacer, turn this on to disable it."
        );
        ModLogger.Debug("Bound UseOldSprReplace");
        UseOutdatedPacks = Config.Bind(
            "General",
            "Load outdated packs",
            true,
            $"When enabled packs that are made with an older pack-version/game-version won't be loaded."
        );
        ModLogger.Debug("Bound UseOutdatedPacks");
        
        ExpandVanillaAudioOverrides = Config.Bind(
            "General",
            "Expand vanilla audio overrides",
            true,
            $"When enabled allows vanilla voice cosmetics to modify more sounds outside of just player and movement sounds."
        );
        ModLogger.Debug("Bound ExpandVanillaAudioOverrides");
        
        // Debugging
        ColorConsole = Config.Bind(
            "Debugging",
            "Color Console",
            // decided to disable by default because it's a bit prestigious to have rh do it automatically
            // instead people could turn it on to help see errors in the console i guess, also i like the looks
            false, 
            $"When enabled certain logs are given colors, disable if this is causing issues. Additionally, only works on windows."
        );
        ModLogger.Debug("Bound ColorConsole");
        AlwaysDebug = Config.Bind(
            "Debugging",
            "Always debug mode",
            false,
            $"When enabled pack debug mode is always enabled unless toggled via the command ({RHCommands.ToggleDebug})."
        );
        ModLogger.Debug("Bound AlwaysDebug");
        
        EmoteWheelKey = Config.Bind("Emotes", "Emote Wheel", KeyCode.None, new ConfigDescription(string.Empty, null, "InputKeyboard", "InputMouse"));
        ModLogger.Debug("Bound Emote Wheel");
        EmoteWheelKeyAlt = Config.Bind("Emotes", "Emote Wheel Gamepad", KeyCode.None, new ConfigDescription(string.Empty, null, "InputGamepad"));
        ModLogger.Debug("Bound Emote Wheel Gamepad");
        ToggleWheel = Config.Bind("Emotes", "Toggle Wheel", false);
        ModLogger.Debug("Bound Toggle Wheel");
        EmoteLeftKey = Config.Bind("Emotes", "Emote Key Left", KeyCode.None, new ConfigDescription(string.Empty, null, "InputKeyboard", "InputMouse"));
        ModLogger.Debug("Bound Emote Key Left");
        EmoteLeftKeyAlt = Config.Bind("Emotes", "Emote Key Left Gamepad", KeyCode.None, new ConfigDescription(string.Empty, null, "InputGamepad"));
        ModLogger.Debug("Bound Emote Key Left Gamepad");
        EmoteRightKey = Config.Bind("Emotes", "Emote Key Right", KeyCode.None, new ConfigDescription(string.Empty, null, "InputKeyboard", "InputMouse"));
        ModLogger.Debug("Bound Emote Key Right");
        EmoteRightKeyAlt = Config.Bind("Emotes", "Emote Key Right Gamepad", KeyCode.None, new ConfigDescription(string.Empty, null, "InputGamepad"));
        ModLogger.Debug("Bound Emote Key Right Gamepad");
        ToggleEmotes = Config.Bind("Emotes", "Toggle Emotes", false);
        ModLogger.Debug("Bound Toggle Emotes");
        LeftEmote = Config.Bind("Emotes", "Left Emote", string.Empty, new ConfigDescription(string.Empty, null, "Hidden"));
        ModLogger.Debug("Bound Left Emote");
        RightEmote = Config.Bind("Emotes", "Right Emote", string.Empty, new ConfigDescription(string.Empty, null, "Hidden"));
        ModLogger.Debug("Bound Right Emote");
        EmoteVolume = Config.Bind("Emotes", "Emote Volume", 1.0f, new ConfigDescription(string.Empty, new AcceptableValueRange<float>(0,1)));
        ModLogger.Debug("Bound Emote Volume");
        
        ModLogger.Debug("Checking generic folder...");
        if (!Directory.Exists(GenericFolder))
            Directory.CreateDirectory(GenericFolder);
        
        ModLogger.Debug("Loading packs prefs...");
        PackPrefs.Load();
        Application.quitting += () =>
        {
            ModLogger.Info("Saving pack prefs...");
            ResourcePacksManager.SavePackOrder();
            ResourcePacksManager.SaveDisabledPacks();
            PackPrefs.Save();
        };
    }
}