using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ResourcefulHands.Core;
using UnityEngine;

namespace ResourcefulHands.EmbedResources;

public static class RHResources
{
    private static Dictionary<string, Texture2D> _textureResources = new();
    
    public static void InitResources()
    {
        ModLogger.Debug("Loading Resources");
        var assembly = Assembly.GetExecutingAssembly();
        
        var names = assembly.GetManifestResourceNames();

        foreach (var name in names)
        {
            if (name.EndsWith(".png"))
            {
                using Stream? stream = assembly.GetManifestResourceStream(name);
                if(stream == null) continue;

                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                
                Texture2D texture = new Texture2D(2, 2);
                if(!texture.LoadImage(buffer))
                    continue;
                texture.filterMode = FilterMode.Point;
                
                string ressourcePath = name.Remove(0, "ResourcefulHands.EmbedResources.".Length);
                ressourcePath = ressourcePath.Remove(ressourcePath.Length - ".png".Length).Replace(".", "/");
                        
                _textureResources.Add(ressourcePath, texture);
                ModLogger.Debug($"Loaded {ressourcePath}");
            }
        }
    }

    public static Texture2D? TryGetTexture(string ressourcePath)
    {
        return _textureResources.GetValueOrDefault(ressourcePath);
    }
}