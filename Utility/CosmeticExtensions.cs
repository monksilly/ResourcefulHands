using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace ResourcefulHands.Utility;

public interface ICosmeticPack
{
    public Cosmetic_Info CosmeticInfo { get; set; }

    public Texture2D Icon { get; set; }
    
    public bool IsActive { get; set; }
}

public class CosmeticHandPack : ICosmeticPack
{
    public Cosmetic_HandItem.Cosmetic_HandItem_Data CosmeticData { get; set; }
    
    public Cosmetic_Info CosmeticInfo { get; set; }
    
    public Texture2D Icon { get; set; }
    
    public bool IsActive { get; set; }
}

public class CosmeticVoicePack : ICosmeticPack
{
    public Cosmetic_Voice.Cosmetic_Voice_Data CosmeticData { get; set; }

    public Cosmetic_Info CosmeticInfo { get; set; }
    
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