using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ResourcefulHands;

public class OF_CosmeticPage : MonoBehaviour
{
    public static OF_CosmeticPage instance;
    private UI_CosmeticsMenu.CosmeticPage? cosmeticPage;

    private GameObject? cosmeticsMenuObject;
    private UI_CosmeticsMenu? cosmeticsMenu;
    private UI_Page? pageTemplate;
    private UI_PageHolder? pageHolder;

    private GameObject? RHPage;
    private UI_TabGroup.Tab? RHTab;

    public List<Cosmetic_Base> RHHands = [];
    
    private void Awake()
    {
        if (!instance)
            instance = this;
        else
        {
            RHLog.Warning("Destroying Duplicate Cosmetics Page Manager");
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoadedDelegate;
        SceneManager.sceneLoaded += OnSceneLoadedDelegate;
    }

    private void OnSceneLoadedDelegate(Scene scene, LoadSceneMode mode) => OnSceneLoaded(scene.name);
    
    private void OnSceneLoaded(string sceneName)
    {
        if (instance != this) return;
        
        if (sceneName != "Main-Menu")
        {
            cosmeticPage = null;
            cosmeticsMenuObject = null;
            cosmeticsMenu = null;
            pageTemplate = null;
            RHPage = null;
            RHTab = null;
            instance.StopAllCoroutines();
            return;
        }

        if (RHPage) return;

        instance.StartCoroutine(InitializeCosmeticsRoutine());
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedDelegate;
    }

    private IEnumerator InitializeCosmeticsRoutine()
    {
        RHLog.Info("Waiting for Resource Packs to finish loading...");
        
        var packTask = ResourcePacksManager.InitialLoadTask;
        while (!packTask.IsCompleted)
        {
            yield return null;
        }
        
        RHLog.Info("Packs loaded. Now waiting for Unity Cosmetics Menu to initialize...");
        
        GameObject? candidate = null;
        while (candidate == null)
        {
            candidate = GameObject.Find("Canvas - Screens/Screens/Canvas - Screen - Other/Cosmetics");
            yield return new WaitForSeconds(0.1f); // Poll every 10th of a second to save CPU
        }
        
        // Just in-case wait for the script to initialize
        var menu = candidate.GetComponent<UI_CosmeticsMenu>();
        while (menu == null || menu.cosmeticPages == null)
        {
            yield return null;
        }
        
        RHLog.Info("Game UI ready. Injecting RH Cosmetic Page...");
        
        FindCosmeticsTemplate();
        PrepareCosmeticPage();
        PrepareCosmetics();
        ApplyCosmetics();
    }
    
    private void FindCosmeticsTemplate()
    {
        if (cosmeticsMenuObject && cosmeticsMenu) return;
        
        RHLog.Debug("Finding Cosmetics Template");
        var candidate = GameObject.Find("Canvas - Screens/Screens/Canvas - Screen - Other/Cosmetics");
        if (!candidate)
        {
            RHLog.Error("Cosmetics template game object not found");
            return;
        };
        var uiCosmeticMenu = candidate.GetComponent<UI_CosmeticsMenu>();
        if (!uiCosmeticMenu)
        {
            RHLog.Error("Cosmetics menu template not found");
            return;
        }
        RHLog.Debug("Cosmetics template found");
        
        cosmeticsMenuObject = candidate;
        cosmeticsMenu = uiCosmeticMenu;
        
        // var templatePage = cosmeticsMenuObject.transform.Find("Cosmetics_Root/Page-Hands");
        // if (!templatePage)
        // {
        //     RHLog.Error("Cosmetics page template not found");
        //     return;
        // }
        //
        // if (!RHPage)
        // {
        //     var existing = GameObject.Find("Canvas - Screens/Screens/Canvas - Screen - Other/Cosmetics/Cosmetics_Root/Page-RH");
        //     if (existing)
        //     {
        //         RHPage = existing;
        //         RHLog.Warning("RH Cosmetic Page already exists, skipping creation...");
        //         return;
        //     }
        // }
        // else
        // {
        //     return;
        // }
        //
        // var newPage = Instantiate(templatePage, cosmeticsMenuObject.transform.Find("Cosmetics_Root"));
        // newPage.name = "Page-RH";
        // newPage.gameObject.SetActive(false);
        // var pageH = newPage.Find("Unlock Page/Pageholder - Hands");
        // if (!pageH)
        // {
        //     RHLog.Error("RH Cosmetics page holder not found");
        //     return;
        // }
        // pageH.name = "Pageholder - RH";
        // pageHolder = pageH.GetComponent<UI_PageHolder>();
        //
        // RHPage = newPage.gameObject;
        //
        // RHLog.Info("Successfully created RH Cosmetic page");
        //
        //
        // var templateTab = cosmeticsMenuObject.transform.Find("Cosmetics_Root/Tab Selection Hor/Hands");
        // if (!templateTab)
        // {
        //     RHLog.Error("Cosmetics tab template not found");
        //     return;
        // }
        //
        // var tabRHButton = Instantiate(templateTab, templateTab.parent);
        // tabRHButton.name = "RH Cosmetics";
        // tabRHButton.gameObject.SetActive(true);
        // tabRHButton.GetChild(0).GetComponent<TextMeshProUGUI>().text = "RH";
        //
        // // Rebuild layout so it is nice and flush with other buttons
        // templateTab.parent.GetComponent<RectTransform>()?.ForceUpdateRectTransforms();
        //
        // var tabGroup = templateTab.parent.GetComponent<UI_TabGroup>();
        // if (!tabGroup)
        // {
        //     RHLog.Error("Tab Group not found");
        //     return;
        // }
        //
        //
        // var handTab = tabGroup.tabs[0];
        // RHTab = new UI_TabGroup.Tab
        // {
        //     button = tabRHButton.GetComponent<Button>(),
        //     firstSelect = handTab.firstSelect,
        //     name = "rh",
        //     onlyDev = false,
        //     tabObject = newPage.gameObject,
        // };
        //
        // tabGroup.tabs.Add(RHTab);
    }
    
    private void PrepareCosmeticPage()
    {
        if (cosmeticPage is not null) return;
        
        // Actually attach it to The default Hands one!
        var handPage = cosmeticsMenu?.cosmeticPages.Find(p => p.cosmeticType == "hand");
        RHLog.Error($"Cosmetic page: {handPage} | ");

        cosmeticPage = new UI_CosmeticsMenu.CosmeticPage
        {
            name = "Hands",
            cosmeticType = "hand",
            pageHolder = handPage?.pageHolder
        };
        
        // cosmeticsMenu?.cosmeticPages.Add(cosmeticPage);
    }
    
    private void PrepareCosmetics()
    {
        if (RHHands.Count > 0) return;

        foreach (var resourcePack in ResourcePacksManager.LoadedPacks)
        {
            var isHandsPack = false;
            foreach (var resSources in resourcePack.Textures)
            {
                if (!resSources.Key.Contains("Sprite_Library")) continue;
                
                isHandsPack = true;
                break;
            }
            
            if (!isHandsPack) continue;
            
            RHLog.Debug($"Loading {resourcePack.name} in Experimental Menu");
            var newCosmetic = ScriptableObject.CreateInstance<Cosmetic_HandItem>();
            newCosmetic.name = resourcePack.name;
            newCosmetic.currentEmoteIds = [0, 0];
            newCosmetic.currentPaletteId = 0;

            var cardTemplate = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "card-blank-foreground");
            RHLog.Debug($"Card template: {cardTemplate}");
            var cosmeticCard = TextureCompositor.CreatePackCard(resourcePack.Icon, TextureCompositor.SpriteToTexture(cardTemplate));
            
            newCosmetic.cosmeticInfo = new Cosmetic_Info
            {
                id = resourcePack.guid,
                cosmeticName = resourcePack.name,
                tag = "hand",
                author = resourcePack.author,
                description = resourcePack.desc,
                unlock = "",
                cardForeground = TextureCompositor.TextureToSprite(cosmeticCard),
            };
            

            newCosmetic.cosmeticData = new Cosmetic_HandItem.Cosmetic_HandItem_Data()
            {
                swapSprites = [],
                cosmeticName = resourcePack.name,
                author = resourcePack.author,
                description = resourcePack.desc,
                unlock = "",
                id = resourcePack.guid,
                
            };
            
            
            Dictionary<string, Cosmetic_HandItem.SwapSprite> swapsDict = [];
            RHLog.Debug("Loading swaps");
            foreach (var resSources in resourcePack.Textures)
            {
                if (resSources.Value is null) continue; // Skip if no texture to save CPU

                if (resSources.Key.Contains("Sprite_Library"))
                {
                    var layer = int.Parse(resSources.Key.Split('_')[^1]);
                    var type = resSources.Key.Contains("Background") ? DynamicHandSlicer.SheetType.Background : DynamicHandSlicer.SheetType.Foreground;
                    var splices = DynamicHandSlicer.SliceSheet(resSources.Value, 4, 4);
                    var namedSplices = DynamicHandSlicer.GetNamedSlicesFromSlicedSheet(type, splices, layer);

                    foreach (var namedSplice in namedSplices)
                    {
                        var tName = namedSplice.Key;
                        var tTexture = namedSplice.Value;
                        RHLog.Debug($"{tName}");
                        
                        var newSwap = new Cosmetic_HandItem.SwapSprite
                        {
                            framerate = 1,
                            hand = -1,
                            loopTimeOffset = 0,
                            offset = Vector2.zero
                        };
                        
                        var newSprite = TextureCompositor.TextureToSprite(tTexture)!;
                        newSprite.name = tName;
                        newSwap.materialBase = "";
                
                        newSwap.replacementSpriteNames = [tName];
                        newSwap.replacementSprites = [newSprite];

                        newSwap.rotation = 0;
                        newSwap.scale = 1;
                        newSwap.secondaryTextures = [];
                        newSwap.spriteName = tName;
                        newSwap.usePalette = true;
                        swapsDict.TryAdd(newSwap.spriteName, newSwap);
                    }
                }
            }

            foreach (var swap in swapsDict.Keys)
            {
                RHLog.Debug(swap);
            }
            newCosmetic.cosmeticData.swapSprites.AddRange(swapsDict.Values);
            
            
            
            RHHands.Add(newCosmetic);
            
            SafeInitialize(newCosmetic);
        }
    }

    private void SafeInitialize(Cosmetic_HandItem item)
    {
        // Use reflection to check the private field 'swapDict'
        var field = typeof(Cosmetic_HandItem).GetField("swapDict", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    
        var swapDict = field?.GetValue(item) as IDictionary;

        // ONLY initialize if the dictionary is null OR empty
        if (swapDict == null || swapDict.Count == 0)
        {
            RHLog.Debug($"Initializing {item.name}...");
            InvokePrivateMethod(item, "Initialize");
        }
        else
        {
            RHLog.Warning($"{item.name} was already initialized! Skipping to avoid Dictionary crash.");
        }
    }
    
    private void ApplyCosmetics()
    {
        if (!cosmeticsMenu || RHHands.Count == 0) return;
        
        InjectIntoCosmeticManager();

        var cosmeticsField = typeof(UI_CosmeticsMenu.CosmeticPage).GetField("cosmetics", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (cosmeticPage != null && cosmeticsField != null)
        {
            // Initialize the internal list if it's null, then add your hands
            List<Cosmetic_Base> internalList = new List<Cosmetic_Base>();
            internalList.AddRange(RHHands);
            cosmeticsField.SetValue(cosmeticPage, internalList);
        }
        
        

        try
        {
            cosmeticsMenu.FillCosmeticPage(RHHands, "Only RH", cosmeticPage);
        }
        catch (Exception e)
        {
            RHLog.Error($"FillCosmeticPage failed: {e.Message}");
        }
    }
    
    private void InjectIntoCosmeticManager()
    {
        try
        {
            var type = typeof(CL_CosmeticManager);
        
            // Get the private static fields
            var handsListField = type.GetField("cosmeticHands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var handsDictField = type.GetField("cosmeticHandDict", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var loadedListField = type.GetField("loadedCosmetics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var cosmeticHands = (List<Cosmetic_HandItem>)handsListField.GetValue(null);
            var cosmeticHandDict = (Dictionary<string, Cosmetic_HandItem>)handsDictField.GetValue(null);
            var loadedCosmetics = (List<Cosmetic_Base>)loadedListField.GetValue(null);

            foreach (var hand in RHHands)
            {
                var handItem = hand as Cosmetic_HandItem;
                if (handItem == null) continue;

                if (!cosmeticHandDict.ContainsKey(handItem.cosmeticInfo.id))
                {
                    cosmeticHands.Add(handItem);
                    cosmeticHandDict.Add(handItem.cosmeticInfo.id, handItem);
                    loadedCosmetics.Add(handItem);
                
                    // Crucial: Initialize the item so its internal logic is ready
                    //handItem.Initialize();
                
                    // Register it with the save system so it can be "owned" or "equipped"
                    SettingsManager.settings.cosmeticSaveData.FillNewCosmeticInfo(handItem);
                
                    RHLog.Info($"Successfully injected {handItem.name} into CL_CosmeticManager");
                }
            }
        }
        catch (Exception e)
        {
            RHLog.Error($"Failed to inject into CL_CosmeticManager: {e.Message}");
        }
    }
    
    public static void InvokePrivateMethod(object obj, string methodName, params object[] args)
    {
        // Search for the method in the object's type and its base types
        var method = obj.GetType().GetMethod(methodName, 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public);

        if (method != null)
        {
            method.Invoke(obj, args);
        }
        else
        {
            RHLog.Error($"Method '{methodName}' not found on {obj.GetType().Name}");
        }
    }
}