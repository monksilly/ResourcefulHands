using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ResourcefulHands.Systems;

public static class VanillaHandChecker
{
    public static bool CheckStructure(string filePath, bool isAbsolute = false)
    {
        string correctPath = filePath;
        
        if (!isAbsolute)
            correctPath = Path.GetFullPath(filePath);
        
        // Interacts check
        bool interactsExist = InteractsCheck(correctPath);
        bool spritesExist = SpritesCheck(correctPath);
        bool palettesExist = PalettesExist(correctPath);
        
        return interactsExist && spritesExist && palettesExist;

    }

    public static void FixStructure(string filePath, bool isAbsolute = false)
    {
        string correctPath = filePath;
        if (!isAbsolute)
            correctPath = Path.GetFullPath(filePath);
        
        bool interactsExist = InteractsCheck(correctPath);
        bool spritesExist = SpritesCheck(correctPath);
        bool palettesExist = PalettesExist(correctPath);
        
        if (!interactsExist)
        {
            var newDir = Directory.CreateDirectory(Path.Join(correctPath, "Interacts"));
            string[] paths = Directory.GetFiles(correctPath, "interact-*", SearchOption.AllDirectories);
            foreach (string path in paths)
            {
                File.Move(path, Path.Join(newDir.FullName, Path.GetFileName(path)));
            }
        }
        
    }

    private static Dictionary<string, string> LoadJsonToDictionary(string filePath, bool isAbsolute = false)
    {
        string correctPath = filePath;
        if (!isAbsolute)
            correctPath = Path.GetFullPath(filePath);
        string text = File.ReadAllText(correctPath);
        var jsonRaw = JsonConvert.DeserializeObject(text);
        return null;
    }

    private static bool InteractsCheck(string absoluteFilePath)
    {
        bool exists = false;
        bool folderExists = Directory.Exists(Path.Join(absoluteFilePath, "Interacts"));

        string[] filePaths = Directory.GetFiles(absoluteFilePath,  "*", SearchOption.TopDirectoryOnly);

        foreach (var filePath in filePaths)
        {
            if (exists) break;
            
            if (filePath.Contains("interact-"))
                exists = true;
        }

        return !exists && folderExists;
    }

    private static bool SpritesCheck(string absoluteFilePath)
    {
        return Directory.Exists(Path.Join(absoluteFilePath, "Sprites"));
    }
    
    private static bool PalettesExist(string absoluteFilePath)
    {
        return Directory.Exists(Path.Join(absoluteFilePath, "Palettes"));
    }
}