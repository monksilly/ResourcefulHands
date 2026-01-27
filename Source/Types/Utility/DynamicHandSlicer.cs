using System.Collections.Generic;
using UnityEngine;

namespace ResourcefulHands;

public static class DynamicHandSlicer
{
    public enum SheetType
    {
        Background,
        Foreground,
    }
    
    /// <summary>
    /// Slices a sheet and returns a dictionary where the key is the coordinate name.
    /// </summary>
    public static Dictionary<Vector2, Texture2D> SliceSheet(Texture2D sheet, int cols, int rows)
    {
        var namedSlices = new Dictionary<Vector2, Texture2D>();
        
        var cellW = sheet.width / cols;
        var cellH = sheet.height / rows;

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                // Unity reads from bottom-left. 
                // To name from Top-Left (standard for sheets), we invert the row index for pixel math.
                var invertedRow = (rows - 1) - r;
                
                var slice = new Texture2D(cellW, cellH)
                {
                    filterMode = FilterMode.Point // Keep pixel art crisp
                };

                var pixels = sheet.GetPixels(c * cellW, invertedRow * cellH, cellW, cellH);
                slice.SetPixels(pixels);
                slice.Apply();

                // Naming convention: SheetName_R0_C2
                var posName = new Vector2(r, c);
                namedSlices.Add(posName, slice);
            }
        }
        return namedSlices;
    }

    public static Dictionary<string, Texture2D> GetNamedSlicesFromSlicedSheet(SheetType sheetType,
        Dictionary<Vector2, Texture2D> slices, int layer = 1)
    {
        var namedSlices = new Dictionary<string, Texture2D>();
        
        var prefix = sheetType == SheetType.Background ? "B" : "F";

        foreach (var slice in slices)
        {
            var slicePos = slice.Key;
            var sliceTexture = slice.Value;

            var posR = (int)Mathf.Abs(slicePos.x);
            
            string gridLetter = "";
            switch (posR)
            {
                case 0:
                    gridLetter = "A";
                    break;
                case 1:
                    gridLetter = "B";
                    break;
                case 2:
                    gridLetter = "C";
                    break;
                case 3:
                    gridLetter = "D";
                    break;
                default:
                    gridLetter = "A";
                    break;
            }

            var name = $"HND_{prefix}{layer}{gridLetter}{slicePos.y+1}";
            namedSlices.Add(name, sliceTexture);
        }
        return namedSlices;
    }
}