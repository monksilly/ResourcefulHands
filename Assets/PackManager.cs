using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
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

    public static List<ICosmeticPack> ActiveCosmeticPacks => CosmeticPacks.Where(pack => pack.IsActive).ToList();
    public static List<ICosmeticPack> CosmeticPacks { get; private set; } = [];
    
    private static List<CosmeticHandPack> _handCosmetics = [];
    private static List<CosmeticVoicePack> _voiceCosmetics = [];
    
    private const string HandJsonFileName = "cosmetic-handitem-settings.json";
    private const string VoiceJsonFileName = "cosmetic-voice-settings.json";
    
    private const string CardBackgroundFileName = "card-background.png";
    private const string CardForegroundFileName = "card-foreground.png";
    
    public static void ScanCosmeticsAtPluginsFolder()
    {
        string pluginsPath = BepInEx.Paths.PluginPath;
        if (!Directory.Exists(pluginsPath))
            return;

        string[] jsonFiles = Directory.GetFiles(pluginsPath, "*.json", SearchOption.AllDirectories);

        Debug.Log("Scanning For Cosmetics Scattered Across BepInEx Plugins...");

        foreach (var jsonFilePath in jsonFiles)
        {
            var jsonFileName = Path.GetFileName(jsonFilePath);
            var pluginFolder = Path.GetDirectoryName(jsonFilePath);
            string jsonContent = File.ReadAllText(jsonFilePath);
            
            switch (jsonFileName)
            {
                case HandJsonFileName:
                    // Read the file
                    var cosmeticHandData =
                        JsonConvert.DeserializeObject<Cosmetic_HandItem.Cosmetic_HandItem_Data>(jsonContent);

                    // Detect if duplicate exists
                    if (CL_CosmeticManager.cosmeticHandDict.ContainsKey(cosmeticHandData.id))
                    {
                        ModLogger.Warning($"Cosmetic hand id: {cosmeticHandData.id}, already exists");
                        continue;
                    }
                    
                    // If not, register on the official cosmetic system
                    CL_CosmeticManager.CreateHandCosmetics(pluginFolder, new List<string>(){ jsonFilePath });
                    
                    ModLogger.Info("Registered hand pack at: " + pluginFolder);
                    break;
                case VoiceJsonFileName:
                    // Read the file
                    var cosmeticVoiceData =
                        JsonConvert.DeserializeObject<Cosmetic_Voice.Cosmetic_Voice_Data>(jsonContent);

                    // Detect if duplicate exists
                    if (CL_CosmeticManager.cosmeticVoiceDict.ContainsKey(cosmeticVoiceData.id))
                    {
                        ModLogger.Warning($"Cosmetic voice id: {cosmeticVoiceData.id}, already exists");
                        continue;
                    }
                    
                    // If not, register on the official cosmetic system
                    CL_CosmeticManager.CreateVoiceCosmetics(pluginFolder, new List<string>(){ jsonFilePath });
                    
                    ModLogger.Info("Registered voice pack at: " + pluginFolder);
                    break;
                default:
                    break;
            }
        }
    }

    #region Pack Creation
    public static bool CreateHandPack(Cosmetic_HandItem.Cosmetic_HandItem_Data cosmeticHandData, string folderDirectory, out CosmeticHandPack cosmeticHandPack)
    {
        cosmeticHandPack = null;
        if (cosmeticHandData == null)
            return false;

        cosmeticHandPack = new ();
        cosmeticHandPack.CosmeticInfo = new Cosmetic_Info
        {
            id = cosmeticHandData.id,
            cosmeticName = cosmeticHandData.cosmeticName,
            tag = "hand",
            author = cosmeticHandData.author,
            description = cosmeticHandData.description,
            unlock = cosmeticHandData.unlock
        };
        cosmeticHandPack.CosmeticData = cosmeticHandData;
        cosmeticHandPack.IsActive = true;
        
        var icon = LoadTextureFromFile(Path.Combine(folderDirectory, CardForegroundFileName));
        if (icon != null)
            cosmeticHandPack.Icon = icon;
        
        CosmeticPacks.Add(cosmeticHandPack);
        _handCosmetics.Add(cosmeticHandPack);
        return true;
    }
    
    public static bool CreateVoicePack(Cosmetic_Voice.Cosmetic_Voice_Data cosmeticVoiceData, string folderDirectory, out CosmeticVoicePack cosmeticVoicePack)
    {
        cosmeticVoicePack = null;
        if (cosmeticVoiceData == null)
            return false;

        cosmeticVoicePack = new ();
        cosmeticVoicePack.CosmeticInfo = new Cosmetic_Info
        {
            id = cosmeticVoiceData.id,
            cosmeticName = cosmeticVoiceData.cosmeticName,
            tag = "voice",
            author = cosmeticVoiceData.author,
            description = cosmeticVoiceData.description,
            unlock = cosmeticVoiceData.unlock
        };
        cosmeticVoicePack.CosmeticData = cosmeticVoiceData;
        cosmeticVoicePack.IsActive = true;
        
        var icon = LoadTextureFromFile(Path.Combine(folderDirectory, CardForegroundFileName));
        if (icon != null)
            cosmeticVoicePack.Icon = icon; 
        
        CosmeticPacks.Add(cosmeticVoicePack);
        _voiceCosmetics.Add(cosmeticVoicePack);
        return true;
    }
    #endregion

    #region Base Game Cosmetic Registering
    public static bool RegisterExistingHandCosmetic(Cosmetic_HandItem cosmeticHand, out CosmeticHandPack cosmeticHandPack)
    {
        cosmeticHandPack = null;
        if (cosmeticHand == null  || cosmeticHand.cosmeticData == null)
            return false;

        cosmeticHandPack = new ();
        cosmeticHandPack.CosmeticInfo = cosmeticHand.cosmeticInfo;
        cosmeticHandPack.CosmeticData = cosmeticHand.cosmeticData;
        cosmeticHandPack.IsActive = true;
        
        if (cosmeticHandPack.CosmeticInfo.cardForeground != null)
        {
            cosmeticHandPack.Icon = cosmeticHand.cosmeticInfo.cardForeground.texture;
        }
        
        CosmeticPacks.Add(cosmeticHandPack);
        _handCosmetics.Add(cosmeticHandPack);
        return true;
    }
    
    public static bool RegisterExistingVoiceCosmetic(Cosmetic_Voice cosmeticVoice, out CosmeticVoicePack cosmeticVoicePack)
    {
        cosmeticVoicePack = null;
        if (cosmeticVoice == null || cosmeticVoice.cosmeticData == null)
            return false;

       cosmeticVoicePack = new ();
       cosmeticVoicePack.CosmeticInfo = cosmeticVoice.cosmeticInfo;
       cosmeticVoicePack.CosmeticData = cosmeticVoice.cosmeticData;
       cosmeticVoicePack.IsActive = true;
        
        if (cosmeticVoicePack.CosmeticInfo.cardForeground != null)
        {
            cosmeticVoicePack.Icon = cosmeticVoice.cosmeticInfo.cardForeground.texture;
        }
        
        CosmeticPacks.Add(cosmeticVoicePack);
        _voiceCosmetics.Add(cosmeticVoicePack);
        return true;
    }
    #endregion
    
    public static void GatherCosmetics()
    {
        CosmeticPacks.Clear(); 
        _handCosmetics.Clear(); 
        _voiceCosmetics.Clear();
        
        foreach (Cosmetic_HandItem cosmeticHand in CL_CosmeticManager.cosmeticHands)
        {
            var vanillaData = cosmeticHand.cosmeticData;
            if (vanillaData == null)
            {
                ModLogger.Warning("Unable to gather hand cosmetic...");
                continue;
            }
            
            ModLogger.Debug($"Getting data from: {vanillaData.cosmeticName}");

            if (RegisterExistingHandCosmetic(cosmeticHand, out var mappedPack))
            {
                ModLogger.Info($"Gathered hand cosmetic with ID: {mappedPack.CosmeticData.id}");
            }
            else
            {
                ModLogger.Info($"Failed to register cosmetic, ID: {cosmeticHand.cosmeticInfo.id}");
            }
        }
        
        foreach (Cosmetic_Voice cosmeticVoice in CL_CosmeticManager.cosmeticVoices)
        {
            var vanillaData = cosmeticVoice.cosmeticData;
            if (vanillaData == null)
            {
                ModLogger.Warning("Unable to gather voice cosmetic...");
                continue;
            }
            
            ModLogger.Debug($"Getting data from: {vanillaData.cosmeticName}");

            if (RegisterExistingVoiceCosmetic(cosmeticVoice, out var mappedPack))
            {
                ModLogger.Info($"Gathered voice cosmetic with ID: {mappedPack.CosmeticData.id}");
            }
            else
            {
                ModLogger.Info($"Failed to register voice cosmetic, ID: {cosmeticVoice.cosmeticInfo.id}");
            }
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