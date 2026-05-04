using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    
    public static Plugin Instance { get; private set; } = null!;

    private Harmony? Harmony { get; set; }

    // TODO: remove jank
    internal static int TargetFps = 60;
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
// amongus sungus bongus