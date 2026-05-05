using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ResourcefulHands.Utility;

public static class FileUtils
{
    /// <summary>
    /// Gets the SHA256 Checksum of a file
    /// </summary>
    /// <param name="filePath">The file's path</param>
    /// <returns></returns>
    public static string GetSHA256Checksum(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        using SHA256 sha = SHA256.Create();

        byte[] hashBytes = sha.ComputeHash(stream);
        StringBuilder sb = new();

        foreach (byte b in hashBytes)
            sb.Append(b.ToString("x2")); // hex format

        return sb.ToString();
    }
}