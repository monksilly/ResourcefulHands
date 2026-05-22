using System.Collections.Generic;
using System.Linq;
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
    
    public ExtendedHandItemData ExtendedCosmeticData { get; set; }
    
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

public class ExtendedHandItemData : Cosmetic_HandItem.Cosmetic_HandItem_Data
{
    public new List<EmoteEntry>? emotes { get; set; } = null!;
}

public enum SoundPlayMode
{
    Random,
    Sequential
}

public class EmoteEntry : Cosmetic_HandItem.Cosmetic_HandItem_Data.HandEmote
{
    public Vector3 Scale { get; set; } = Vector3.one;
    public float Rotation { get; set; }
    public string Sound { get; set; } = null!;
    public List<string> SoundFiles { get; set; } = null!;
    public List<AudioClip?> SoundClips { get; set; } = null!;
    public SoundPlayMode SoundPlayMode { get; set; } = SoundPlayMode.Random;
    public bool SoundLoop { get; set; } = false;
}

public class PaletteEntry : Cosmetic_HandItem.Cosmetic_HandItem_Data.ColorPalette {}

public class SwapSpriteEntry : Cosmetic_HandItem.SwapSprite {}

public class InteractSwapEntry : Cosmetic_HandItem.InteractSwap {}

public class SecondaryTextureEntry : Cosmetic_HandItem.SwapSprite.SecondaryTextures
{
    public List<string> SecondaryTextureNames { get; set; } = null!;
}


