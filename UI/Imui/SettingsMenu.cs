using Imui.Controls;
using Imui.Core;
using ResourcefulHands.Systems;
using WKLib.API;
using WKLib.API.UI;
using static ResourcefulHands.Core.RHConfig;

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

        gui.Separator("General");
        UIUtility.DrawConfigEntry(gui, LazyManip);
        UIUtility.DrawConfigEntry(gui, UseOldSprReplace);
        UIUtility.DrawConfigEntry(gui, UseOutdatedPacks);
        UIUtility.DrawConfigEntry(gui, ExpandVanillaAudioOverrides);
        
        gui.AddSpacing();
        gui.Separator("Emotes");
        UIUtility.DrawConfigEntry(gui, EmoteWheelKey);
        UIUtility.DrawConfigEntry(gui, EmoteWheelKeyAlt);
        UIUtility.DrawConfigEntry(gui, ToggleWheel);
        UIUtility.DrawConfigEntry(gui, EmoteLeftKey);
        UIUtility.DrawConfigEntry(gui, EmoteLeftKeyAlt);
        UIUtility.DrawConfigEntry(gui, EmoteRightKey);
        UIUtility.DrawConfigEntry(gui, EmoteRightKeyAlt);
        UIUtility.DrawConfigEntry(gui, ToggleEmotes);
        
        gui.AddSpacing();
        gui.Separator("Debugging");
        UIUtility.DrawConfigEntry(gui, ColorConsole);
        UIUtility.DrawConfigEntry(gui, AlwaysDebug);
        
#if DEBUG
        gui.Checkbox(ref DebugUIBoxes, "Debug UI Boxes");
#endif
        
        gui.EndWindow();
    }

    public override void HandleInput(ImGui gui) { }
}