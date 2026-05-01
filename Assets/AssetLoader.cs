using UnityEngine;
using System.Reflection;

namespace ResourcefulHands.Assets;

public static class AssetLoader
{
    private static AssetBundle? _bundle;
    public static Texture2D? CorruptionTexture { get; private set; }
    public static Texture2D? Icon { get; private set; }

    public static void LoadBundle()
    {
        if (_bundle != null) return;

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ResourcefulHands.rh_assets.bundle");
        if (stream != null)
        {
            _bundle = AssetBundle.LoadFromStream(stream);
            CorruptionTexture = _bundle.LoadAsset<Texture2D>("Corruption1");
            Icon = _bundle.LoadAsset<Texture2D>("icon");
            _bundle.LoadAllAssets();
        }
    }

    public static void Unload() => _bundle?.Unload(true);
}