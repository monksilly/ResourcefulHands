using System;
using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using Imui.Examples;
using Imui.Rendering;
using ResourcefulHands.Assets;
using ResourcefulHands.Core;
using WKLib.API.UI;
using ResourcefulHands.UI.Imui.Utility;
using UnityEngine;
using static ResourcefulHands.UI.Imui.WindowsDeclarations;

namespace ResourcefulHands.UI.Imui;

public class PacksMenu : WKLibWindow
{
    private ImScrollFlag scrollBarFlags = ImScrollFlag.PersistentVerBar | ImScrollFlag.HideHorBar;

    private string searchBuffer = "";
    
    public override void Draw(ImGui gui, bool isRootPanelOpen)
    {
        if (!isRootPanelOpen)
            return;
        
        if (!gui.BeginWindow("RH Packs", ref isOpen, new ImSize(800, 400), ImWindowFlag.None))
            return;
        
        gui.Separator("Pack selection");

        var layoutWidth = gui.GetLayoutWidth();
        var spacing = gui.Style.Layout.Spacing;
        
        gui.AddSpacing(spacing);
        gui.TextEdit(ref searchBuffer, hint: "Search for a pack here.");
        gui.AddSpacing(spacing * 2f);

        var layoutHeight = gui.GetLayoutHeight();

        // The spacing is made this way to add a small gap between the 2 selections
        var packSelectionWidth = layoutWidth * 0.5f - spacing * 0.5f;
        
        // the GetRowHeight is in anticipation of the "open packs" folder button (and the reload packs button too)
        // and the spacing is there to make it look pretty
        var packSelectionHeight = layoutHeight - gui.GetRowHeight() - spacing * 3f;
        gui.BeginHorizontal(packSelectionWidth, packSelectionHeight);

        gui.BeginVertical();
        DrawInactivePacks(gui);
        gui.EndVertical();
        
        gui.AddSpacing(spacing);
        
        gui.BeginVertical(packSelectionWidth, packSelectionHeight);
        DrawActivePacks(gui);
        gui.EndVertical();
        
        gui.EndHorizontal();

        // Extra buttons (open packs, reload packs, etc)
        gui.AddSpacing(spacing * 2f);
        DrawActions(gui);
        
        gui.EndWindow(scrollBarFlags);
    }

    private void DrawActions(ImGui gui)
    {
        var spacing = gui.Style.Layout.Spacing;

        gui.BeginHorizontal();

        if (gui.Button("Open Packs folder", size: (gui.GetLayoutWidth() * 0.5f - spacing * 0.5f, gui.GetRowHeight() )))
        { 
            Application.OpenURL("file://" + RHConfig.PacksFolder.Replace("\\", "/"));
        }

        if (gui.Button("Reload Packs", ImSizeMode.Fill))
        {
            ResourcePacksManager.ReloadPacks();
        }
        
        gui.EndHorizontal();
    }
    
    private void DrawActivePacks(ImGui gui)
    {
        gui.PushId("ActivePacks");
        
        var isSearching = searchBuffer.Trim().Length > 0;
        var spacing = gui.Style.Layout.Spacing;
        var cellHeight = 100f;
        
        gui.Separator("Active packs");
        gui.BeginList((gui.GetLayoutWidth(), gui.GetLayoutHeight()));
        var grid = gui.BeginGrid(1, cellHeight);
        
        for (int i = 0; i < ResourcePacksManager.ActivePacks.Length; i++)
        {   
            ref var loadedPack = ref ResourcePacksManager.ActivePacks[i];
            if (loadedPack == null)
                continue;

            if (isSearching)
            {
                if (!loadedPack.name.ToLower().Contains(searchBuffer.Trim().ToLower())
                    && !loadedPack.author.ToLower().Contains(searchBuffer.Trim().ToLower())
                    && !loadedPack.desc.ToLower().Contains(searchBuffer.Trim().ToLower()))
                    continue;
            }

            var gridRect = gui.GridNextCell(ref grid);

            var gridId = gui.GetNextControlId();
            gui.RegisterGroup(gridId, gridRect);
            var isGridHovered = gui.IsGroupHovered(gridId);

            if (isGridHovered)
                gui.Canvas.RectOutline(gridRect, gui.Style.Button.Normal.BorderColor, 2f, 2f);

            var cellWidth = gridRect.W;
            
            var iconRect = DrawIcon(gui, loadedPack, gridRect, cellHeight, cellWidth);
            
            cellWidth -= (iconRect.W + spacing);
            if (cellWidth <= 0f)
                continue;

            var titleRect = DrawTitle(gui, loadedPack, gridRect, iconRect, cellHeight, cellWidth);
            var authorRect = DrawAuthor(gui, loadedPack, gridRect, iconRect, cellHeight, cellWidth);
            var descriptionRect = DrawDescription(gui, loadedPack, gridRect, iconRect, cellHeight, cellWidth);
            
            cellWidth -= (titleRect.W + spacing);
            if (cellWidth <= 0f)
                continue;
            
            if (!isGridHovered)
                continue;

            bool isOnlyElement = ResourcePacksManager.ActivePacks.Length == 1;
            var makeInactiveRect = DrawMakeInactive(gui, ref loadedPack, gridRect, titleRect, isOnlyElement, cellHeight, cellWidth);
            
            cellWidth -= (makeInactiveRect.W + spacing);
            if (cellWidth <= 0f)
                continue;
            
            bool isFirstElement = i == 0;
            bool isLastElement = i == (ResourcePacksManager.ActivePacks.Length - 1);
            
            // Dont draw priority if theres only 1 active pack
            if (isOnlyElement)
                continue;

            if (isFirstElement)
            {
                DrawMoveDown(gui, loadedPack, gridRect, makeInactiveRect, cellHeight, cellWidth);
            }
            else if (isLastElement)
            {
                DrawMoveUp(gui, loadedPack, gridRect, makeInactiveRect, cellHeight, cellWidth);
            }
            else
            {
                DrawMoveUp(gui, loadedPack, gridRect, makeInactiveRect, cellHeight, cellWidth);
                DrawMoveDown(gui, loadedPack, gridRect, makeInactiveRect, cellHeight, cellWidth);
            }
        }
        gui.EndGrid(in grid);
        gui.EndList(flags: scrollBarFlags);

        gui.PopId();
    }
    
    private void DrawInactivePacks(ImGui gui)
    {
        gui.PushId("InactivePacks");
        
        var isSearching = searchBuffer.Trim().Length > 0;
        var spacing = gui.Style.Layout.Spacing;
        var cellHeight = 100f;
        
        gui.Separator("Inactive packs");
        gui.BeginList((gui.GetLayoutWidth(), gui.GetLayoutHeight()));
        var grid = gui.BeginGrid(1, cellHeight);
        
        for (int i = 0; i < ResourcePacksManager.LoadedPacks.Count; i++)
        {   
            var loadedPack = ResourcePacksManager.LoadedPacks[i];
            if (loadedPack == null)
                continue;

            if (loadedPack.IsActive)
                continue;

            if (isSearching)
            {
                if (!loadedPack.name.ToLower().Contains(searchBuffer.Trim().ToLower())
                    && !loadedPack.author.ToLower().Contains(searchBuffer.Trim().ToLower())
                    && !loadedPack.desc.ToLower().Contains(searchBuffer.Trim().ToLower()))
                    continue;   
            }
            
            var gridRect = gui.GridNextCell(ref grid);

            var gridId = gui.GetNextControlId();
            gui.RegisterGroup(gridId, gridRect);
            var isGridHovered = gui.IsGroupHovered(gridId);

            if (isGridHovered)
                gui.Canvas.RectOutline(gridRect, gui.Style.Button.Normal.BorderColor, 2f, 2f);

            var cellWidth = gridRect.W;
            
            var iconRect = DrawIcon(gui, loadedPack, gridRect, cellHeight, cellWidth);
            
            cellWidth -= (iconRect.W + spacing);
            if (cellWidth <= 0f)
                continue;

            var titleRect = DrawTitle(gui, loadedPack, gridRect, iconRect, cellHeight, cellWidth);
            var authorRect = DrawAuthor(gui, loadedPack, gridRect, iconRect, cellHeight, cellWidth);
            var descriptionRect = DrawDescription(gui, loadedPack, gridRect, iconRect, cellHeight, cellWidth);
            
            cellWidth -= (titleRect.W + spacing);
            if (cellWidth <= 0f)
                continue;
            
            if (!isGridHovered)
                continue;

            var makeActiveRect = DrawMakeActive(gui, ref loadedPack, gridRect, titleRect, cellHeight, cellWidth);
            
            cellWidth -= (makeActiveRect.W + spacing);
            if (cellWidth <= 0f)
                continue;
        }
        gui.EndGrid(in grid);
        gui.EndList(flags: scrollBarFlags);

        gui.PopId();
    }

    #region Pack Selection UI Elements
    private ImRect DrawIcon(ImGui gui, ResourcePack loadedPack, ImRect gridRect, float cellHeight, float cellWidth)
    {
        var spacing = gui.Style.Layout.Spacing;
    
        var iconRect = new ImRect(gridRect);
        {
            iconRect.X += spacing;
                    
            iconRect.H -= spacing;
            iconRect.Y += spacing / 2f;
                    
            iconRect.W = iconRect.H; // Maintain a square icon
        }
        if (SettingsWindow.DebugUIBoxes)
            gui.Canvas.RectOutline(iconRect, new Color32(255, 255, 0, 255), 2f, 0f);
        gui.Image(loadedPack.Icon, iconRect);
        return iconRect;
    }

    private ImRect DrawTitle(ImGui gui, ResourcePack loadedPack, ImRect gridRect, ImRect previousRect, float cellHeight, float cellWidth)
    {
        var spacing = gui.Style.Layout.Spacing;

        var titleWidth = (cellWidth * 0.8f) - (spacing * 2f);
        var titleHeight = (cellHeight * 0.25f) - spacing;
                
        var titleXPos = spacing + previousRect.W + spacing;
        var titleYPos  = (cellHeight * 0.75f);
                
        var titleRect = new ImRect(gridRect);
        {
            titleRect.X += titleXPos;
            titleRect.Y += titleYPos;
                    
            titleRect.H = titleHeight;
            titleRect.W = titleWidth;
        }
        if (SettingsWindow.DebugUIBoxes)
            gui.Canvas.RectOutline(titleRect, new Color32(0, 125, 255, 255), 2f, 0f);
        gui.Text(loadedPack.name, titleRect);

        return titleRect;
    }

    private ImRect DrawAuthor(ImGui gui, ResourcePack loadedPack, ImRect gridRect, ImRect previousRect, float cellHeight, float cellWidth)
    {
        var spacing = gui.Style.Layout.Spacing;

        var authorWidth = (cellWidth * 0.8f) - (spacing * 2f);
        var authorHeight = (cellHeight * 0.25f) - spacing;
                
        var authorXPos = spacing + previousRect.W + spacing;
        var authorYPos  = (cellHeight * 0.5f) + spacing;
                
        var authorRect = new ImRect(gridRect);
        {
            authorRect.X += authorXPos;
            authorRect.Y += authorYPos;
                    
            authorRect.H = authorHeight;
            authorRect.W = authorWidth;
        }
                
        if (SettingsWindow.DebugUIBoxes)
            gui.Canvas.RectOutline(authorRect, new Color32(0, 255, 255, 255), 2f, 0f);
        gui.Text(loadedPack.author, authorRect);
        return authorRect;
    }

    private ImRect DrawDescription(ImGui gui, ResourcePack loadedPack, ImRect gridRect, ImRect previousRect, float cellHeight, float cellWidth)
    {
        var spacing = gui.Style.Layout.Spacing;
        
        var descriptionWidth = (cellWidth * 0.8f) - (spacing * 2f);
        var descriptionHeight = (cellHeight * 0.5f) - spacing;
                
        var descriptionXPos = spacing + previousRect.W + spacing;
        var descriptionYPos = spacing;
                
        var descriptionRect = new ImRect(gridRect);
        {
            descriptionRect.X += descriptionXPos;
            descriptionRect.Y += descriptionYPos;
                    
            descriptionRect.H = descriptionHeight;
            descriptionRect.W = descriptionWidth;
        }
        if (SettingsWindow.DebugUIBoxes)
            gui.Canvas.RectOutline(descriptionRect, new Color32(125, 0, 255, 255), 2f, 0f);
        gui.Text(loadedPack.desc, descriptionRect, wrap: true, overflow: ImTextOverflow.Ellipsis);
        return descriptionRect;
    }
    
    private ImRect DrawMakeActive(ImGui gui, ref ResourcePack loadedPack, ImRect gridRect, ImRect previousRect, float cellHeight, float cellWidth)
    {
        var spacing = gui.Style.Layout.Spacing;
        
        var previousRectXPos = Math.Abs(previousRect.X - gridRect.X); // Calculate relative position, since the X value is global and we need the local pos
        
        var makeActiveWidth = (cellWidth * 1f) - (spacing * 2f);
        var makeActiveHeight = (cellHeight * 1f) - (spacing * 2f);

        var makeActiveXPos = previousRectXPos + previousRect.W + spacing;
        var makeActiveYPos = spacing;
                
        var makeActiveRect = new ImRect(gridRect);
        {
            makeActiveRect.X += makeActiveXPos;
            makeActiveRect.Y += makeActiveYPos;

            makeActiveRect.H = makeActiveHeight;
            makeActiveRect.W = makeActiveWidth;
        }
        if (SettingsWindow.DebugUIBoxes)
            gui.Canvas.RectOutline(makeActiveRect, new Color32(0, 67, 245, 255), 2f, 0f);

        if (gui.Button(">", makeActiveRect))
        {
            loadedPack.IsActive = true;
        }
        return makeActiveRect;
    }
    
    private ImRect DrawMakeInactive(ImGui gui, ref ResourcePack loadedPack, ImRect gridRect, ImRect previousRect, bool isOnlyElement, float cellHeight, float cellWidth)
    {
        var spacing = gui.Style.Layout.Spacing;
        
        var previousRectXPos = Math.Abs(previousRect.X - gridRect.X); // Calculate relative position, since the X value is global and we need the local pos
        
        var makeInactiveWidth = (cellWidth * (isOnlyElement ? 1f : 0.5f)) - (spacing * 2f);
        var makeInactiveHeight = (cellHeight * 1f) - (spacing * 2f);

        var makeInactiveXPos = previousRectXPos + previousRect.W + spacing;
        var makeInactiveYPos = spacing;
                
        var makeInactiveRect = new ImRect(gridRect);
        {
            makeInactiveRect.X += makeInactiveXPos;
            makeInactiveRect.Y += makeInactiveYPos;

            makeInactiveRect.H = makeInactiveHeight;
            makeInactiveRect.W = makeInactiveWidth;
        }
        if (SettingsWindow.DebugUIBoxes)
            gui.Canvas.RectOutline(makeInactiveRect, new Color32(0, 67, 245, 255), 2f, 0f);
    
        if (gui.Button("<", makeInactiveRect))
        {
            loadedPack.IsActive = false;
        }
        
        return makeInactiveRect;
    }

    private ImRect DrawMoveUp(ImGui gui, ResourcePack loadedPack, ImRect gridRect, ImRect previousRect, float cellHeight, float cellWidth)
    {
        var spacing = gui.Style.Layout.Spacing;
        
        var previousRectXPos = Math.Abs(previousRect.X - gridRect.X); // Calculate relative position, since the X value is global and we need the local pos
        
        var moveUpWidth = (cellWidth * 1f) - (spacing * 2f);
        var moveUpHeight = (cellHeight * 0.5f) - spacing;
                
        var moveUpXPos = previousRectXPos + previousRect.W + spacing;
        var moveUpYPos = (cellHeight * 0.5f);
                
        var moveUpRect = new ImRect(gridRect);
        {
            moveUpRect.X += moveUpXPos;
            moveUpRect.Y += moveUpYPos;

            moveUpRect.H = moveUpHeight;
            moveUpRect.W = moveUpWidth;
        }
        
        if (SettingsWindow.DebugUIBoxes)
            gui.Canvas.RectOutline(moveUpRect, new Color32(0, 67, 245, 255), 2f, 0f);
    
        gui.Button("^", moveUpRect);
        return moveUpRect;
    }
    
    private ImRect DrawMoveDown(ImGui gui, ResourcePack loadedPack, ImRect gridRect, ImRect previousRect, float cellHeight, float cellWidth)
    {
        var spacing = gui.Style.Layout.Spacing;
        
        var previousRectXPos = Math.Abs(previousRect.X - gridRect.X); // Calculate relative position, since the X value is global and we need the local pos
        
        var moveDownWidth = (cellWidth * 1f) - (spacing * 2f);
        var moveDownHeight = (cellHeight * 0.5f) - (spacing * 2f);
                
        var moveDownXPos = previousRectXPos + previousRect.W + spacing;
        var moveDownYPos = spacing;
                
        var moveDownRect = new ImRect(gridRect);
        {
            moveDownRect.X += moveDownXPos;
            moveDownRect.Y += moveDownYPos;

            moveDownRect.H = moveDownHeight;
            moveDownRect.W = moveDownWidth;
        }
        if (SettingsWindow.DebugUIBoxes)
            gui.Canvas.RectOutline(moveDownRect, new Color32(0, 67, 245, 255), 2f, 0f);
    
        gui.Button("v", moveDownRect);
        return moveDownRect;
    }
    #endregion

    public override void HandleInput(ImGui gui) { }
}