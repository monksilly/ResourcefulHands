using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ResourcefulHands.Core;
using ResourcefulHands.Utility;
using UnityEngine;
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
    private static List<CosmeticVoicePack> _voiceCosmetics = [];
    
    public static bool RegisterHandCosmetic(CosmeticHandPack handPack, string fgCardPath = "")
    {
        if (_handCosmetics.Contains(handPack)) return false;

        handPack.cosmeticName = $"{handPack.cosmeticName} (HAND)";
        
        if (!string.IsNullOrEmpty(fgCardPath))
        {
            var icon = LoadTextureFromFile(fgCardPath);
            if (icon != null)
                handPack.Icon = icon;
        }
        
        ModLogger.Info($"Pack {handPack.id} has been registered");
        _handCosmetics.Add(handPack);
        CosmeticPacks.Add(handPack);
        return true;
    }

    public static bool RegisterVoiceCosmetics(CosmeticVoicePack voicePack, string fgCardPath = "")
    {
        if (_voiceCosmetics.Contains(voicePack)) return false;

        voicePack.cosmeticName = $"{voicePack.cosmeticName} (VOICE)";
        
        if (!string.IsNullOrEmpty(fgCardPath))
        {
            var icon = LoadTextureFromFile(fgCardPath);
            if (icon != null)
                voicePack.Icon = icon;
        }
        
        ModLogger.Info($"Pack {voicePack.id} has been registered");
        _voiceCosmetics.Add(voicePack);
        CosmeticPacks.Add(voicePack);
        return true;
    }

    private static CosmeticHandPack CopyVanillaData(Cosmetic_HandItem.Cosmetic_HandItem_Data vanillaData)
    {
        return new CosmeticHandPack()
        {
            id = vanillaData.id,
            author = vanillaData.author,
            description = vanillaData.description,
            cosmeticName = vanillaData.cosmeticName,
            unlock = vanillaData.unlock,
            emotes = vanillaData.emotes,
            palettes = vanillaData.palettes,
            allowedSpecialtyPoses = vanillaData.allowedSpecialtyPoses,
            useGlobalSecondary = vanillaData.useGlobalSecondary,
            globalSecondary = vanillaData.globalSecondary,
            globalMaterialSwap = vanillaData.globalMaterialSwap,
            globalMaterialBase = vanillaData.globalMaterialBase,
            forceGlobalMaterialOntoHands = vanillaData.forceGlobalMaterialOntoHands,
            useCustomStaminaColor = vanillaData.useCustomStaminaColor,
            customStaminaColor = vanillaData.customStaminaColor,
            useCustomStaminaIconColor = vanillaData.useCustomStaminaIconColor,
            customStaminaIconColor = vanillaData.customStaminaIconColor,
            useCustomStaminaOutlineColor = vanillaData.useCustomStaminaOutlineColor,
            customStaminaOutlineColor = vanillaData.customStaminaOutlineColor,
            outlineMaskColor = vanillaData.outlineMaskColor,
            swapSprites = vanillaData.swapSprites,
            interactSwaps = vanillaData.interactSwaps,
        
            // Custom Interface settings
            IsActive = false
        };
    }
    
    public static void GatherGameHandCosmetics()
    {
        foreach (Cosmetic_HandItem cosmeticHandPack in CL_CosmeticManager.cosmeticHands)
        {
            var vanillaData = cosmeticHandPack.cosmeticData;
            if (vanillaData == null)
            {
                ModLogger.Warning("Unable to gather Vanilla Hand cosmetic...");
                continue;
            }
            
            ModLogger.Debug($"Going to get data from: {vanillaData.cosmeticName}");
            
            CosmeticHandPack mappedPack = CopyVanillaData(vanillaData);
            
            if (cosmeticHandPack.cosmeticInfo.cardForeground != null)
            {
                mappedPack.Icon = cosmeticHandPack.cosmeticInfo.cardForeground.texture;
            }
            
            ModLogger.Info($"Gathered Vanilla Hand cosmetic with ID: {mappedPack.id}");
            
            RegisterHandCosmetic(mappedPack);
        }
    }
    
    public static bool IncludesPack(CosmeticHandPack handPack) => _handCosmetics.Contains(handPack);

    public static List<CosmeticHandPack> GetHandPacks() => _handCosmetics;

    public static void ShowPopup(string text, float timeToShow)
    {
        WKLib.API.UI.UIUtility.ShowPopupForTime(text, timeToShow);
    }
    
    public static Texture2D? LoadTextureFromFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            // Create a temporary texture. 
            // The size (2, 2) will be replaced by the actual image dimensions on LoadImage.
            Texture2D tex = new Texture2D(2, 2);
            if (ImageConversion.LoadImage(tex, fileData))
            {
                return tex;
            }
        }
        return null;
    }
}