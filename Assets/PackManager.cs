using System.Collections.Generic;
using ResourcefulHands.Core;
using ResourcefulHands.Utility;
using WKLib.API;

namespace ResourcefulHands.Assets;

public static class PackManager
{
    public static List<CosmeticHandPack> ActiveHandPacks { get; private set; }

    public static List<CosmeticHandPack> HandPacks
    {
        get => _handCosmetics; 
    }

    public static List<ICosmeticPack> ActiveCosmeticPacks { get; private set; } = [];
    public static List<ICosmeticPack> CosmeticPacks { get; private set; } = [];
    
    private static List<CosmeticHandPack> _handCosmetics = [];
    private static List<Cosmetic_Voice> _voiceCosmetics = [];
    
    public static bool RegisterHandCosmetic(CosmeticHandPack handPack)
    {
        if (_handCosmetics.Contains(handPack)) return false;
        
        ModLogger.Info($"Pack {handPack.id} has been registered");
        _handCosmetics.Add(handPack);
        CosmeticPacks.Add(handPack);
        return true;
    }

    public static void GatherGameHandCosmetics()
    {
        foreach (var cosmeticHandPack in CL_CosmeticManager.cosmeticHands)
            _handCosmetics.Add((cosmeticHandPack.cosmeticData as CosmeticHandPack)!);
    }
    
    public static bool IncludesPack(CosmeticHandPack handPack) => _handCosmetics.Contains(handPack);

    public static List<CosmeticHandPack> GetHandPacks() => _handCosmetics;

    public static void ShowPopup(string text, float timeToShow)
    {
        WKLib.API.UI.UIUtility.ShowPopupForTime(text, timeToShow);
    }
}