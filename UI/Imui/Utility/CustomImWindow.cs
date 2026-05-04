using Imui.Controls;
using Imui.Core;

namespace ResourcefulHands.UI.Imui.Utility;

public static class CustomImWindow
{
    public static void EndWindow(this ImGui gui, ImScrollFlag scrollFlags)
    {
        gui.EndScrollable(scrollFlags);
        gui.Layout.Pop();

        gui.WindowManager.EndWindow();

        gui.Canvas.PopClipRect();
        gui.Canvas.PopRectMask();
        gui.Canvas.PopOrder();

        gui.PopId();
    }   
}