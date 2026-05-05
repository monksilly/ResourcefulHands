using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ResourcefulHands.Core;
using ResourcefulHands.Utility;

namespace ResourcefulHands.Systems;

public static class CosmeticStructureRepairer
{
    // Prevent Processing the same folder more than once
    private static readonly HashSet<string> ProcessedFolders = [];
    
    private static readonly HashSet<string> ReservedNames = [
        "icon",
        "manifest",
        "readme",
        "card-foreground",
        "card-background"
    ];
    
    /// <summary>
    /// Checks if a directory is missing the standard folder structure.
    /// </summary>
    public static bool NeedsFixing(string modFolderPath)
    {
        return !Directory.Exists(Path.Combine(modFolderPath, "Sprites")) ||
               !Directory.Exists(Path.Combine(modFolderPath, "Interacts"));
    }

    /// <summary>
    /// Reads the JSON files and tries to repair them.
    /// <param name="modFolderPath">The Directory where the xxx-xxx-settings.json resides</param>
    /// <param name="jsonFileName">The name of the .json file, e.g. cosmetic-handitem-settings.json</param>
    /// </summary>
    public static void FixModStructure(string modFolderPath, string jsonFileName = "cosmetic-handitem-settings.json")
    {
        if (string.IsNullOrEmpty(modFolderPath) || !Directory.Exists(modFolderPath))
        {
            ModLogger.Warning($"[Fixer] Provided path is invalid: {modFolderPath}");
            return;
        }
        
        if (ProcessedFolders.Contains(modFolderPath)) return;

        try
        {
            string jsonPath = Path.Combine(modFolderPath, jsonFileName);
            if (!File.Exists(jsonPath))
            {
                ModLogger.Debug($"[Fixer] No settings JSON found in {modFolderPath}. Skipping...");
                return;
            }

            ModLogger.Info($"[Fixer] Checking structure for mod: {Path.GetFileName(modFolderPath)}");

            // Parse JSON
            string jsonContent = File.ReadAllText(jsonPath);
            var settings = JsonConvert.DeserializeObject<CosmeticSettings>(jsonContent);
            if (settings == null)
            {
                ModLogger.Error($"[Fixer] Failed to deserialize JSON at {jsonPath}");
                return;
            }

            // Prepare Folders
            string spritesDir = Path.Combine(modFolderPath, "Sprites");
            string interactsDir = Path.Combine(modFolderPath, "Interacts");
            string palettesDir = Path.Combine(modFolderPath, "Palettes");

            Directory.CreateDirectory(spritesDir);
            Directory.CreateDirectory(interactsDir);
            Directory.CreateDirectory(palettesDir);

            // Categorize filenames from JSON
            HashSet<string> handSpriteFiles = [];
            if (settings.SwapSprites != null)
            {
                foreach (var entry in settings.SwapSprites)
                foreach (var name in entry.ReplacementSpriteNames)
                    handSpriteFiles.Add(name.ToLower());
            }

            HashSet<string> interactFiles = [];
            if (settings.InteractSwaps != null)
            {
                foreach (var entry in settings.InteractSwaps)
                    interactFiles.Add(entry.ReplacementSpriteName.ToLower());
            }

            HashSet<string> secondaryFiles = [];
            if (settings.GlobalSecondary != null)
            {
                foreach (var entry in settings.GlobalSecondary)
                foreach (var name in entry.SecondaryTextureNames)
                    secondaryFiles.Add(name.ToLower());
            }

            ModLogger.Debug(
                $"[Fixer] JSON parsed. Found {handSpriteFiles.Count} sprites, {interactFiles.Count} interacts, {secondaryFiles.Count} secondary textures.");
            
            // Move the files
            MoveMatchingFiles(modFolderPath, handSpriteFiles, "Sprites", "HND_");
            MoveMatchingFiles(modFolderPath, handSpriteFiles, "Sprites", "stamina_");
            MoveMatchingFiles(modFolderPath, interactFiles, "Interacts", "interact-");
            MoveMatchingFiles(modFolderPath, secondaryFiles, "Palettes", "stamina");

            string[] leftoverFiles = Directory.GetFiles(modFolderPath, "*.png");
            foreach (var file in leftoverFiles)
            {
                string fileNameOnly = Path.GetFileNameWithoutExtension(file).ToLower();
    
                // Skip reserved filenames (manifest, icon, etc.)
                if (ReservedNames.Contains(fileNameOnly)) continue;

                try
                {
                    string fileNameWithExt = Path.GetFileName(file);
                    string destPath = Path.Combine(palettesDir, fileNameWithExt);

                    if (File.Exists(destPath)) File.Delete(destPath);
                    File.Move(file, destPath);
        
                    ModLogger.Debug($"[Fixer] Moving leftover file {fileNameWithExt} -> Palettes/");
                }
                catch (Exception ex)
                {
                    ModLogger.Error($"[Fixer] Failed to move leftover file {file}: {ex.Message}");
                }
            }
            
            ModLogger.Info($"[Fixer] Finished organizing {Path.GetFileName(modFolderPath)}");
            ProcessedFolders.Add(modFolderPath);
        }
        catch (Exception ex)
        {
            ModLogger.Error($"[Fixer] CRITICAL ERROR fixing structure at {modFolderPath}: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// Moves files according to a match
    /// </summary>
    /// <param name="root">The directory that it looks inside for a match</param>
    /// <param name="targets">Things it should look for</param>
    /// <param name="subFolder">The directory where it should move them into</param>
    /// <param name="prefixFallback">If not matching, move this, if it matches this prefix</param>
    private static void MoveMatchingFiles(string root, HashSet<string> targets, string subFolder, string prefixFallback = null)
    {
        string targetFolder = Path.Combine(root, subFolder);
        string[] files = Directory.GetFiles(root, "*.png");

        foreach (var file in files)
        {
            string fileNameOnly = Path.GetFileNameWithoutExtension(file).ToLower();
            string fileNameWithExt = Path.GetFileName(file);

            bool isMatch = targets.Contains(fileNameOnly);
            
            // Fallback for files that follow naming conventions but might be missing from JSON lists
            if (!isMatch && !string.IsNullOrEmpty(prefixFallback))
            {
                if (fileNameOnly.StartsWith(prefixFallback.ToLower())) isMatch = true;
            }

            if (isMatch)
            {
                try 
                {
                    if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
                    
                    string destPath = Path.Combine(targetFolder, fileNameWithExt);
                    
                    ModLogger.Debug($"[Fixer] Moving {fileNameWithExt} -> {subFolder}/");

                    if (File.Exists(destPath))
                    {
                        ModLogger.Warning($"[Fixer] File {fileNameWithExt} already exists in {subFolder}. Overwriting.");
                        File.Delete(destPath);
                    }

                    File.Move(file, destPath);
                }
                catch (IOException ioEx)
                {
                    ModLogger.Error($"[Fixer] Failed to move {fileNameWithExt}. File may be in use. Error: {ioEx.Message}");
                }
            }
        }
    }
}