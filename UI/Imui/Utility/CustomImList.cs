using Imui.Controls;
using Imui.Core;

namespace ResourcefulHands.UI.Imui.Utility;

public static class CustomImList
{
    public static void EndList(this ImGui gui, ImScrollFlag flags = ImScrollFlag.None)
    {
        gui.EndScrollable(flags);
        gui.Canvas.PopClipRect();
        gui.Canvas.PopRectMask();
        gui.Layout.Pop();
    }
}