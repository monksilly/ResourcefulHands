using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ResourcefulHands;

// TODO: this needs alotta work to be effective
public class RHDebugTools : MonoBehaviour
{
    public static RHDebugTools? Instance;
    public static bool isOn;

    private static readonly List<AudioClip> PlayingClips = [];
    private GUIStyle _style = GUIStyle.none;
    private bool _enableNextFrame;

    internal static void Create()
    {
        Instance = new GameObject("DebugTools").AddComponent<RHDebugTools>();
        DontDestroyOnLoad(Instance);
    }
    
    public static void QueueSound(AudioClip clip, bool force = false)
    {
        CoroutineDispatcher.Dispatch(_queueSound(clip, force));
    }
    private static IEnumerator _queueSound(AudioClip clip, bool force)
    {
        if(!force && PlayingClips.Contains(clip))
            yield break;
        
        PlayingClips.Add(clip);
        yield return new WaitForSeconds(1.0f);
        if(clip)
            PlayingClips.Remove(clip);
    }
    
    public void Awake()
    {
        // Keep only one instance
        if (!Instance)
            Instance = this;
        else
            Destroy(this);
        
        if (RHConfig.AlwaysDebug)
            isOn = true;
        
        _style = new GUIStyle
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState { textColor = Color.white }
        };

        SceneManager.sceneUnloaded += _ =>
        {
            _enableNextFrame |= isOn;
            isOn = false;
        };
        SceneManager.sceneLoaded += (_, mode) =>
        {
            if (mode != LoadSceneMode.Single) return;
            PlayingClips.Clear();
            _enableNextFrame |= isOn;
            isOn = false;
        };
    }

    public void OnGUI()
    {
        if(!isOn) return;

        var prevColor = GUI.contentColor;
        
        GUI.contentColor = Color.white;
        GUILayout.Label("Recent sounds:", _style);
        for (int i = PlayingClips.Count - 1; i >= 0; i--)
        {
            if (PlayingClips[i] == null)
                PlayingClips.RemoveAt(i);
        }
        foreach (var clip in PlayingClips)
            GUILayout.Label(clip.name, _style);
        
        GUI.contentColor = prevColor;
    }

    public void LateUpdate()
    {
        if (!_enableNextFrame) return;
        
        isOn = true;
        _enableNextFrame = false;
    }
}


[HarmonyPatch(typeof(UnityEngine.Object))]
public static class DEBUG_InstantiatePatches
{
    private static void OnInstantiated(UnityEngine.Object result, UnityEngine.Object original)
    {
        if (!result) return;
        
        void PatchObject(UnityEngine.Object obj)
        {
            switch (obj)
            {
                case AudioSource audio:
                    if(audio is { isActiveAndEnabled: true, playOnAwake: true })
                        RHDebugTools.QueueSound(audio.clip);
                    break;
            }
        }
        
        switch (result)
        {
            case GameObject go:
            {
                var comps = go.GetComponentsInChildren<Component>();
                foreach (var comp in comps)
                    PatchObject(comp);
                break;
            }
            case Component component:
            {
                var comps = component.GetComponentsInChildren<Component>();
                foreach (var comp in comps)
                    PatchObject(comp);
                break;
            }
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object) })]
    private static void Postfix_1(UnityEngine.Object __result, UnityEngine.Object original) 
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(UnityEngine.SceneManagement.Scene) })]
    private static void Postfix_2(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(Transform) })]
    private static void Postfix_3(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(Transform), typeof(bool) })]
    private static void Postfix_4(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(Vector3), typeof(Quaternion) })]
    private static void Postfix_5(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(UnityEngine.Object.Instantiate), new[] { typeof(UnityEngine.Object), typeof(Vector3), typeof(Quaternion), typeof(Transform) })]
    private static void Postfix_6(UnityEngine.Object __result, UnityEngine.Object original)
        => OnInstantiated(__result, original);
}

[HarmonyPatch(typeof(AudioSource))]
[HarmonyPriority(Priority.Low)]
public static class DEBUG_AudioSourcePatches
{
    // CODE FROM: Patches.cs
    
    // Patch Play()
    [HarmonyPatch(nameof(AudioSource.Play), [])]
    [HarmonyPrefix]
    private static void Play_NoArgs_Postfix(AudioSource __instance)
        => LogClip(src:__instance);

    // Patch Play(double delay)
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(double) })]
    [HarmonyPrefix]
    private static void Play_DelayDouble_Postfix(AudioSource __instance)
        => LogClip(src:__instance);

    // Patch Play(ulong delaySamples)
    [HarmonyPatch(nameof(AudioSource.Play), new[] { typeof(ulong) })]
    [HarmonyPrefix]
    private static void Play_DelayUlong_Postfix(AudioSource __instance)
        => LogClip(src:__instance);
    
    // Patch PlayOneShot(AudioClip)
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip))]
    [HarmonyPrefix]
    private static void PlayOneShot_ClipOnly_Postfix(AudioSource __instance, ref AudioClip __0)
    {
        LogClip(clip:__0);
    }

    // Patch PlayOneShot(AudioClip, float volumeScale)
    [HarmonyPatch(nameof(AudioSource.PlayOneShot), typeof(AudioClip), typeof(float))]
    [HarmonyPrefix]
    private static void PlayOneShot_ClipAndVolume_Postfix(AudioSource __instance, ref AudioClip __0)
    {
        LogClip(clip:__0);
    }
    
    // Shared logic
    private static void LogClip(AudioSource src = null!, AudioClip clip = null!)
    {
        if(!RHDebugTools.isOn) return;
        
        if(!clip) return;
        if(src) clip = src.clip;
        
        RHDebugTools.QueueSound(clip);
    }
}