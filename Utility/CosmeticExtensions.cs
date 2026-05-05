using System.Collections.Generic;

namespace ResourcefulHands.Utility;

public class CosmeticSettings
{
    public string ID { get; set; } = null!;
    public List<SwapSpriteEntry>? SwapSprites { get; set; }
    public List<InteractSwapEntry>? InteractSwaps { get; set; }
    public List<SecondaryTextureEntry>? GlobalSecondary { get; set; }
}

public class SwapSpriteEntry
{
    public string SpriteName { get; set; } = null!;
    public List<string> ReplacementSpriteNames { get; set; } = null!;
}

public class InteractSwapEntry
{
    public string ReplacementSpriteName { get; set; } = null!;
}

public class SecondaryTextureEntry
{
    public List<string> SecondaryTextureNames { get; set; } = null!;
}