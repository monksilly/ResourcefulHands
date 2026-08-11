using System.Text;

namespace ResourcefulHands.Utility;

public class TextUtils
{
    public static string InsertLineBreaks(string text, int maxLineLength)
    {
        if (string.IsNullOrWhiteSpace(text) || maxLineLength <= 0)
            return string.Empty;

        var words = text.Trim().Split(' ');
        var sb = new StringBuilder();

        int currentLineLength = 0;

        foreach (var word in words)
        {
            if (currentLineLength + word.Length + 1 > maxLineLength)
            {
                sb.AppendLine();
                currentLineLength = 0;
            }
            else if (currentLineLength > 0)
            {
                sb.Append(' ');
                currentLineLength++;
            }

            sb.Append(word);
            currentLineLength += word.Length;
        }

        return sb.ToString();
    }
}