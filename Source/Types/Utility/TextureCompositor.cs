using UnityEngine;

namespace ResourcefulHands;

public static class TextureCompositor
{
    public static Texture2D CreatePackCard(Texture2D packIcon, Texture2D layoutReference)
    {
        var result = new Texture2D(layoutReference.width, layoutReference.height, TextureFormat.RGBA32, false);
        
        // Make the entire canvas 100% transparent
        var clearPixels = new Color[result.width * result.height];
        for (var i = 0; i < clearPixels.Length; i++) 
        {
            clearPixels[i] = Color.clear; 
        }
        result.SetPixels(clearPixels);

        // Calculate positioning (Icon center at 1/3 of total width)
        var targetX = (layoutReference.width / 3) - (packIcon.width / 2);
        var targetY = (layoutReference.height / 2) - (packIcon.height / 2);

        // Place icon onto the canvas
        for (var x = 0; x < packIcon.width; x++)
        {
            for (var y = 0; y < packIcon.height; y++)
            {
                var px = targetX + x;
                var py = targetY + y;

                // Bounds check
                if (px < 0 || px >= result.width || py < 0 || py >= result.height) continue;

                var iconCol = packIcon.GetPixel(x, y);
                
                // Transfer pixels directly to maintain original transparency/colors
                result.SetPixel(px, py, iconCol);
            }
        }

        result.Apply();
        result.filterMode = FilterMode.Point; // Keep pixel art sharp
        return result;
    }

    public static Texture2D? SpriteToTexture(Sprite sprite)
    {
        if (!sprite) return null;

        // Temporary RenderTexture
        var tmp = RenderTexture.GetTemporary(
            sprite.texture.width,
            sprite.texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        // Blit the texture
        Graphics.Blit(sprite.texture, tmp);

        // Set the active RenderTexture so we can read from it
        var previous = RenderTexture.active;
        RenderTexture.active = tmp;

        // Create a Texture2D and read the pixels
        var readableText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
    
        // Read only the area defined by the sprite
        readableText.ReadPixels(new Rect(sprite.textureRect.x, sprite.textureRect.y, sprite.textureRect.width, sprite.textureRect.height), 0, 0);
        readableText.Apply();

        // Cleanup
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);

        return readableText;
    }
    
    public static Sprite? TextureToSprite(Texture2D tex, float ppu = 100f)
    {
        if (!tex) return null;

        // Create a rect that covers the entire texture
        var rect = new Rect(0, 0, tex.width, tex.height);
    
        // Create the sprite centered (0.5, 0.5)
        var newSprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), ppu);
    
        // Matches the name of the texture for easier debugging in UnityExplorer
        newSprite.name = tex.name; 
    
        return newSprite;
    }
}