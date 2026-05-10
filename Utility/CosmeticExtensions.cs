using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace ResourcefulHands.Utility;

public interface ICosmeticPack
{
    string id { get; set; }
    string cosmeticName { get; set; }
    string author { get; set; }
    string description { get; set; }
    Texture2D Icon { get; set; }
    
    bool IsActive { get; set; }
}

public class CosmeticHandPack : Cosmetic_HandItem.Cosmetic_HandItem_Data, ICosmeticPack
{
    public SwapSpriteEntry? SwapSprites { get; set; }
    public List<InteractSwapEntry>? InteractSwaps { get; set; }
    public List<SecondaryTextureEntry>? GlobalSecondary { get; set; }
    public string id { get; set; }
    public string cosmeticName { get; set; }
    public string author { get; set; }
    public string description { get; set; }
    public Texture2D Icon { get; set; } = new Texture2D(1, 1);
    public bool IsActive { get; set; }
}

public class CosmeticVoicePack : Cosmetic_Voice.Cosmetic_Voice_Data, ICosmeticPack
{
    public string id { get; set; }
    public string cosmeticName { get; set; }
    public string author { get; set; }
    public string description { get; set; }
    public Texture2D Icon { get; set; }
    public bool IsActive { get; set; }
}

public class PaletteEntry : Cosmetic_HandItem.Cosmetic_HandItem_Data.ColorPalette {}

public class SwapSpriteEntry : Cosmetic_HandItem.SwapSprite {}

public class InteractSwapEntry : Cosmetic_HandItem.InteractSwap {}

public class SecondaryTextureEntry : Cosmetic_HandItem.SwapSprite.SecondaryTextures
{
    public List<string> SecondaryTextureNames { get; set; } = null!;
}