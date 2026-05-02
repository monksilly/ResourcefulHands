using UnityEngine;
using UnityEngine.SceneManagement;
using ResourcefulHands.Assets;
using ResourcefulHands.UI;

namespace ResourcefulHands.Systems;

public static class SceneHandler
{
    private static bool _hasLoadedIntro;

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CosmeticSystem.EnsureExists();
        
        if (!scene.name.ToLower().Contains("intro") && !_hasLoadedIntro)
        {
            _hasLoadedIntro = true;
            InitializeFirstTimeLoad();
        }

        if (!_hasLoadedIntro) return;

        // Refresh logic
        UpdateGlobalSystems();
    }

    private static void InitializeFirstTimeLoad()
    {
        AssetLoader.LoadBundle();
        SpriteSystem.StartReplacementThreads();
        RHDebugTools.Create();
    }

    private static void UpdateGlobalSystems()
    {
        if (ResourcePacksManager.HasPacksChanged)
            ResourcePacksManager.ReloadPacks(false, () => RHSettingsManager.ShowNoticeOld("Packs auto-reloaded!"));

        RHCommands.RefreshCommands();
        RHSettingsManager.LoadCustomSettings();
        AssetRefresher.RefreshAll();
    }
}