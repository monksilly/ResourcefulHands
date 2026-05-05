using System.IO;
using System.Text;
using UnityEngine;

namespace ResourcefulHands.Utility;

public static class RHExtensions
{
    // Stream Helpers
    public static void WriteString(this Stream stream, string value)
    {
        foreach (var character in value)
            stream.WriteByte((byte)character);
    }

    public static void WriteInteger(this Stream stream, uint value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    public static void WriteShort(this Stream stream, ushort value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
    }

    // Texture Helpers
    public static RenderTexture ConvertToARGB32(this RenderTexture self)
    {
        if (self.format == RenderTextureFormat.ARGB32) return self;
        RenderTexture result = RenderTexture.GetTemporary(self.width, self.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(self, result);
        return result;
    }
    
    // String Helpers
    public static string CleanString(string str)
    {
        // remove all directly invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
            str = str.Replace(invalidChar.ToString(), "");

        // limit chars to the ascii range so no weird characters like ∞
        var strBuild = new StringBuilder();
        foreach (char c in str)
        {
            if(c > 127) continue;
            strBuild.Append(c);
        }
        
        return strBuild.ToString();
    }
}