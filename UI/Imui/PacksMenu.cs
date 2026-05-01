using Imui.Controls;
using Imui.Core;
using WKLib.API.UI;

namespace ResourcefulHands.UI.Imui;

public class PacksMenu : WKLibWindow
{
    public override void Draw(ImGui gui, bool isRootPanelOpen)
    {
        if (!isRootPanelOpen)
            return;
        
        if (!gui.BeginWindow("Packs", ref isOpen, new ImSize(400, 400), ImWindowFlag.None))
            return;
        
        
        gui.EndWindow();
    }

    public override void HandleInput(ImGui gui) { }
}