using System.Linq;
using Imui.Controls;
using Imui.Core;
using WKLib.API;
using static ResourcefulHands.UI.Imui.WindowsDeclarations;

namespace ResourcefulHands.UI.Imui;

public class ModListTab : WKLib.API.UI.ModTab
{
    public override string DisplayName => "Resourceful Hands";
    
    public override void DrawSubMenu(ImGui gui)
    {
        if (gui.Button("Pack menu"))
        {
            PacksWindow.isOpen = !PacksWindow.isOpen;
        }
        
        if (gui.Button("Settings menu"))
        {
            SettingsWindow.isOpen = !SettingsWindow.isOpen;
        }
    }
}