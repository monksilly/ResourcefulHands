using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using ResourcefulHands.Assets;
using ResourcefulHands.Core;
using ResourcefulHands.Patches;
using ResourcefulHands.Systems;
using ResourcefulHands.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ResourcefulHands;

// TODO: test for/fix crash when quitting game (unsure but this has happened at-least twice, possible due to the use of DebugTools.cs?)

[BepInPlugin(Guid, Name, Version)] // Resourceful Hands
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "monksilly.resourcefulhands";
    public const string Name = "Resourceful Hands";
    public const string Version = "0.11.0";

    public const string DeprecatedRHGuid = "triggeredidiot.wkd.resourcefulhands";
    
    public GameObject? ofHolder;
    
    private static AssetBundle? _assets;
    public static AssetBundle? Assets
    {
        get
        {
            if (_assets) return _assets;
            
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = $"ResourcefulHands.rh_assets.bundle";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
                _assets = AssetBundle.LoadFromStream(stream);

            CorruptionTexture = (_assets?.LoadAsset<Texture2D>("Corruption1"));
            Icon = (_assets?.LoadAsset<Texture2D>("icon"));
            IconGray = (_assets?.LoadAsset<Texture2D>("gray_icon"));
            
            return _assets;
        }
        private set => _assets = value;
    }
    public static Texture2D? CorruptionTexture;
    public static Texture2D? Icon;
    public static Texture2D? IconGray;
    
    public static Plugin Instance { get; private set; } = null!;

    private Harmony? Harmony { get; set; }

    // TODO: remove jank
    internal static int targetFps = 60;
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;
    private static int _mainThreadId;
    public void Awake()
    {
        ModLogger.InitLog(Logger);
        
        ModLogger.Debug("Patching...");
        Harmony = new Harmony(Guid);
        Harmony.PatchAll();
        // Check for old RH and disable if found
        CheckDeprecation();
        // Checks if the mod is in any compatible version!
        VersionChecker.Check();
        
        Task.Run(ResourcePacksManager.InitLoad);
        _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        
        Instance = this;
        RHConfig.InitConfigs();

        ModLogger.Debug("Hooking loaded event...");
        var hasLoadedIntro = false;
        SceneManager.sceneLoaded += SceneHandler.OnSceneLoaded;
        
        ModLogger.Info("Resourceful Hands has loaded!");
    }

    private void CheckDeprecation()
    {
        if (!Chainloader.PluginInfos.ContainsKey(DeprecatedRHGuid)) return;
        
        var oldModInfo = Chainloader.PluginInfos[DeprecatedRHGuid];
        ModLogger.Warning($"Detected deprecated mod [{oldModInfo.Metadata.Name}]. Disabling it...");

        var oldModInstance = oldModInfo.Instance;

        if (oldModInstance == null) return;
        
        oldModInstance.enabled = false;
                
        Harmony.UnpatchID(DeprecatedRHGuid);
                
        Destroy(oldModInstance.gameObject);
    }
}
// amongus sungus