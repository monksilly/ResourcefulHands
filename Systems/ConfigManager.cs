using WKLib.API.Config;

namespace ResourcefulHands.Systems;

public static class ConfigManager
{
    public static ConfigValue<bool> ExpandVanillaAudioOverrides = new ConfigValue<bool>(Plugin.WKLibAPI, nameof(ExpandVanillaAudioOverrides), true);
}