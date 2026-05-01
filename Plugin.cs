using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BepInEx;
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

[BepInDependency(WKLib.WKLibPlugin.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(Guid, Name, Version)] // Resourceful Hands
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "monksilly.resourcefulhands";
    public const string Name = "Resourceful Hands";
    public const string Version = "0.11.0";

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
        VersionChecker.Check();
        
        Task.Run(ResourcePacksManager.InitLoad);
        _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        
        Instance = this;
        RHConfig.InitConfigs();
        
        ModLogger.Debug("Patching...");
        Harmony = new Harmony(Guid);
        Harmony.PatchAll();

        ModLogger.Debug("Hooking loaded event...");
        var hasLoadedIntro = false;
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            InitOfficialCosmeticSystem();
            targetFps = Application.targetFrameRate;
            ModLogger.Debug("Evaluating newly loaded scene...");
            if(!scene.name.ToLower().Contains("intro") && !hasLoadedIntro)
            {
                hasLoadedIntro = true;
                ModLogger.Info("Loading internal assets...");
                Assets?.LoadAllAssets();

                if (RHConfig.UseOldSprReplace)
                {
                    ModLogger.Info("Hooking sprite replacer...");
                    CoroutineDispatcher.AddToUpdate(() =>
                    {
                        var spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                        
                        foreach (var sr in spriteRenderers)
                            SpriteRendererPatches.Patch(sr);
                    });
                }
                else // TODO: eventually improve this to edit animators or sum?
                {
                    ModLogger.Info("Queuing sprite replacer...");
                    CoroutineDispatcher.RunOnMainThread(() => //create isolated local context
                    {
                        var spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                        var lastTick = Time.time;
                        
                        // currently the old tick speed was too slow and caused 
                        // sprites to flicker when spawned in (which i was trying to fix with InstantiatePatches)
                        // so i've reduced it until we have a better solution
                        const float tickSpace = 1.0f/60.0f;
                        
                        Coroutine? c = null;
                        void CreateCoroutine()
                        {
                            if (c != null)
                                CoroutineDispatcher.StopDispatch(c);
                            
                            IEnumerator PollSpriteRenderers()
                            {
                                while (true)
                                {
                                    spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                                    lastTick = Time.time;
                                    yield return new WaitForSeconds(tickSpace);
                                }
                                // ReSharper disable once IteratorNeverReturns
                            }
                            c = CoroutineDispatcher.Dispatch(PollSpriteRenderers());
                        }
                        CreateCoroutine();
                        
                        CoroutineDispatcher.AddToUpdate(() =>
                        {
                            if (Time.time - lastTick > tickSpace * 32.0f)
                            {
                                ModLogger.Warning("Sprite finder thread has been dead for a while, restarting...");
                                CreateCoroutine();
                            }
                            
                            foreach (var sr in spriteRenderers)
                                SpriteRendererPatches.Patch(sr);
                        });
                    });
                }
                
                ModLogger.Info("Loading debug tools...");
                RHDebugTools.Create();
            }
            
            if (!hasLoadedIntro)
                return;
            
            ModLogger.Info("Checking packs state...");
            if (ResourcePacksManager.HasPacksChanged)
                ResourcePacksManager.ReloadPacks(callback:(() =>
                { RHSettingsManager.ShowNotice("Packs have been auto reloaded!"); }));
            
            ModLogger.Info("Refreshing custom commands...");
            RHCommands.RefreshCommands();

            ModLogger.Info("Loading settings menu...");
            RHSettingsManager.LoadCustomSettings();
            
            ModLogger.Info("Refreshing assets...");
            // RefreshAllAssets();
        };
        
        ModLogger.Message("Resourceful Hands has loaded!");
    }

    private void InitOfficialCosmeticSystem()
    {
        if (ofHolder) return;

        ofHolder = new GameObject()
        {
            name = "RHCosmeticSystem"
        };
        ofHolder.AddComponent<OF_CosmeticPage>();
        DontDestroyOnLoad(ofHolder);
    }
}
// amongus sungus