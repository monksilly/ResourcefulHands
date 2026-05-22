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
        
        gui.AddSpacing();
        gui.Separator("Emotes");
        for (int i = 0; i < MaxEmotes; i++)
        {
            UIUtility.DrawConfigEntry(gui, EmoteKeysLeft[i]);
            UIUtility.DrawConfigEntry(gui, EmoteKeysRight[i]);
        }
        
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