using System.Collections.Generic;

namespace ResourcefulHands.Assets;

public class PackManager
{
    public List<Cosmetic_HandItem> handCosmetics = [];
    public List<Cosmetic_Voice> voiceCosmetics = [];

    public void GatherHands()
    {
        handCosmetics = CL_CosmeticManager.cosmeticHands;
    }

    public void GatherVoices()
    {
        voiceCosmetics = CL_CosmeticManager.cosmeticVoices;
    }
}