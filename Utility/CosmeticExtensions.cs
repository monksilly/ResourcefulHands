using System.Collections.Generic;
using Newtonsoft.Json;

namespace ResourcefulHands.Utility;

public class CosmeticPack
{
    #region Vanilla Settings
    
    [JsonProperty("id")]
    public string ID { get; set; } = null!;

    [JsonProperty("cosmeticName")]
    public string CosmeticName { get; set; } = null!;

    [JsonProperty("description")]
    public string Author { get; set; } = null!;
    
    [JsonProperty("credits")]
    public string Credits { get; set; } = null!;
    
    // Can be null!
    [JsonProperty("unlock", NullValueHandling = NullValueHandling.Ignore)]
    public string? Unlock { get; set; }

    [JsonProperty("allowedSpecialtyPoses")]
    public List<string> AllowedSpecialtyPoses { get; set; } = [];
    
    [JsonProperty("palettes", NullValueHandling = NullValueHandling.Ignore)]
    public List<PaletteEntry>? Palettes { get; set; }
    
    public List<SwapSpriteEntry>? SwapSprites { get; set; }
    public List<InteractSwapEntry>? InteractSwaps { get; set; }
    public List<SecondaryTextureEntry>? GlobalSecondary { get; set; }
    #endregion
}

public class PaletteEntry
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;
    
    [JsonProperty("title")]
    public string Title { get; set; } = null!;
}

public class SwapSpriteEntry
{
    [JsonProperty("spriteName")]
    public string SpriteName { get; set; } = null!;
    
    [JsonProperty("replacementSpriteNames")]
    public List<string> ReplacementSpriteNames { get; set; } = null!;
    
    [JsonProperty("framerate")]
    public float Framerate { get; set; }
    
    [JsonProperty("loopTimeOffset",  NullValueHandling = NullValueHandling.Ignore)]
    public float? LoopTimeOffset { get; set; }

    [JsonProperty("materialBase")]
    public string MaterialBase { get; set; } = "";

    [JsonProperty("hand", NullValueHandling = NullValueHandling.Ignore)]
    public int Hand { get; set; }
    
    [JsonProperty("requiredStateTags", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> RequiredStateTags { get; set; } = [];
    
    [JsonProperty("requiredItemNames",  NullValueHandling = NullValueHandling.Ignore)]
    public List<string> RequiredItemNames { get; set; } = [];
    
    [JsonProperty("opacity",  NullValueHandling = NullValueHandling.Ignore)]
    public float Opacity { get; set; } = 1f;
}

public class InteractSwapEntry
{
    public string ReplacementSpriteName { get; set; } = null!;
}

public class SecondaryTextureEntry
{
    public List<string> SecondaryTextureNames { get; set; } = null!;
}