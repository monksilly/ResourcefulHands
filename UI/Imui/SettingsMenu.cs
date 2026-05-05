using Imui.Controls;
using Imui.Core;
using ResourcefulHands.Systems;
using WKLib.API;
using WKLib.API.UI;

namespace ResourcefulHands.UI.Imui;

public class SettingsMenu : WKLibWindow
{
    public bool DebugUIBoxes = false;
    
    public override void Draw(ImGui gui, bool isRootPanelOpen)
    {
        if (!isRootPanelOpen)
            return;
        
        if (!gui.BeginWindow("RH Settings", ref isOpen, new ImSize(400, 400), ImWindowFlag.None))
            return;
        
        gui.Separator("Settings");
        
        if (gui.Checkbox(ref ConfigManager.ExpandVanillaAudioOverrides.RefValue, "Expand vanilla audio overrides"))
            Plugin.WKLibAPI.DefaultConfigFile.SaveSync();
        
        gui.TooltipAtLastControl("Enabling this allows vanilla voice cosmetics to modify\nmore sounds outside of just player and movement sounds.");
        
#if DEBUG
        gui.Checkbox(ref DebugUIBoxes, "Debug UI Boxes");
#endif
        
        gui.EndWindow();
    }

    public override void HandleInput(ImGui gui) { }
}