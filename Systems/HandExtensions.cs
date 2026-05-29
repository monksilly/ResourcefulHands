using System;
using System.Collections.Generic;
using System.Linq;
using ResourcefulHands.Assets;
using ResourcefulHands.Core;
using ResourcefulHands.Utility;
using UnityEngine;
using UnityEngine.Serialization;
using WKLib.API.Audio;
using WKLib.API.Input;
using WKLib.Core.Classes;
using Random = UnityEngine.Random;

namespace ResourcefulHands.Systems;

public class HandExtensions : MonoBehaviour
{
    private static readonly Dictionary<ENT_Player.Hand, HandExtensions> _handExtensionsMap = new();

    private ENT_Player.Hand _hand;
    private EmoteEntry? _currentEmote;

    private int _emoteSoundIndex;
    private AudioSource? _emoteAudioSource;
    private float _emoteTime;

    public Vector3 originalOffset;
    public Vector3 baseScaleFactor;
    public Vector3 originalScale;
    public Quaternion originalRotation;
    
    private const string KeyInteractLeft = "Hand-Left";
    private const string KeyInteractRight = "Hand-Right";
    
    private bool IsLeft => _hand.id == 0;
    
    public static bool TryGet(ENT_Player.Hand? hand, out HandExtensions? ext)
    {
        ext = null;
        return hand != null && _handExtensionsMap.TryGetValue(hand, out ext);
    }
    
    public static bool TryGet(int handId, out HandExtensions? ext)
    {
        ext = _handExtensionsMap.Values.FirstOrDefault(h => h._hand.id == handId);
        return ext != null;
    }

    private void Awake()
    {
        _hand = GetComponent<Hand_Base>().hand;
        _handExtensionsMap.Add(_hand, this);
    }

    private void OnDestroy()
    {
        _handExtensionsMap.Remove(_hand);
    }

    private void Update()
    {
        if (_hand.currentCosmetics == null || _hand.currentCosmetics.Count == 0)
            return;

        UpdateEmotes();
    }

    private void UpdateEmotes()
    {
        if (!_hand.IsFree())
        {
            StopEmote();
            return;
        }

        if (EmoteWheel.IsActive)
            return;

        var keyBind = IsLeft ? RHConfig.EmoteLeftKey.Value : RHConfig.EmoteRightKey.Value;
        var keyBindAlt = IsLeft ? RHConfig.EmoteLeftKeyAlt.Value : RHConfig.EmoteRightKeyAlt.Value;
        var handKeyBind = IsLeft ? KeyInteractLeft : KeyInteractRight;
        var emote = IsLeft ? RHConfig.LeftEmote.Value : RHConfig.RightEmote.Value;

        if (InputUtility.GetKeyDown(keyBind) || InputUtility.GetKeyDown(keyBindAlt))
        {
            SetEmote(emote);
            return;
        }

        if((InputUtility.GetKeyUp(keyBind) || InputUtility.GetKeyUp(keyBindAlt)) && !RHConfig.ToggleEmotes.Value && _currentEmote != null)
        {
            StopEmote();
        }

        if (_currentEmote != null && InputManager.GetButton(handKeyBind).Up && !RHConfig.ToggleEmotes.Value)
        {
            StopEmote();
        }
    }

    public void ApplySprite()
    {
        ApplyEmote();
    }
    
    private void ApplyEmote()
    {
        if (_currentEmote == null) return;
        var spriteIndex = 0;

        switch (_currentEmote.PlayMode)
        {
            case EmotePlayMode.Loop:
                spriteIndex = Mathf.FloorToInt(Mathf.Repeat((Time.time-_emoteTime) * _currentEmote.Framerate, _currentEmote.Sprites.Count));
                break;
            case EmotePlayMode.LoopGlobal:
                spriteIndex = Mathf.FloorToInt(Mathf.Repeat(Time.time * _currentEmote.Framerate, _currentEmote.Sprites.Count));
                break;
            case EmotePlayMode.Once:
                spriteIndex = Mathf.FloorToInt(Mathf.Min((Time.time-_emoteTime) * _currentEmote.Framerate, _currentEmote.Sprites.Count-1));
                break;
        }
            
        _hand.SetSprite(_currentEmote.Sprites[spriteIndex]);
    }

    public void SetEmote(EmoteEntry? emote, bool force = false)
    {
        if (_currentEmote != null && !force && !RHConfig.ToggleEmotes.Value || emote == null) return;

        if (_currentEmote == emote && RHConfig.ToggleEmotes.Value)
        {
            StopEmote();
            return;
        }
        
        bool changedEmote = _currentEmote != emote;

        _currentEmote = emote;
        if (changedEmote)
            _emoteAudioSource = AudioUtility.PlaySound(
                GetEmoteSound(), _hand.handModel.position, _hand.handModel, RHConfig.EmoteVolume.Value,
                loop: _currentEmote.SoundLoop, bypassEffects: true, mixerType: AudioMixerType.Sfx);

        if (_currentEmote.PlayMode is EmotePlayMode.Loop or EmotePlayMode.Once)
            _emoteTime = Time.time;
    }

    public void SetEmote(int emoteIndex, bool force = false)
    {
        SetEmote(GetEmote(emoteIndex, out _), force);
    }
    
    public void SetEmote(string emoteId, bool force = false)
    {
        SetEmote(GetEmote(emoteId, out _), force);
    }

    public EmoteEntry? GetEmote(int emoteIndex, out CosmeticHandPack? cosmetic)
    {
        cosmetic = null;

        int index = 0;
        foreach (var handCurrentCosmetic in _hand.currentCosmetics)
        {
            if (!PackManager.HandCosmeticPacksDict.TryGetValue(handCurrentCosmetic.cosmeticInfo.id, out var pack))
                return null;
            
            if (pack.ExtendedCosmeticData.emotes == null || pack.ExtendedCosmeticData.emotes.Count <= emoteIndex)
                continue;
            
            if(pack.ExtendedCosmeticData.emotes[emoteIndex] == null)
                continue;

            foreach (var emoteEntry in pack.ExtendedCosmeticData.emotes)
            {
                if (index == emoteIndex)
                {
                    if (emoteEntry.Sprites.Count == 0) return null;

                    cosmetic = pack;
                    return emoteEntry;
                }

                index++;
            }
        }

        cosmetic = null;
        return null;
    }
    
    public EmoteEntry? GetEmote(string id, out CosmeticHandPack? cosmetic)
    {
        cosmetic = null;
        if (string.IsNullOrEmpty(id))
            return null;
        
        bool isFullId = id.Contains("/");

        if (isFullId)
        {
            var cosmeticId = id.Substring(0, id.IndexOf("/", StringComparison.Ordinal));
            var emoteId = id.Substring(id.IndexOf("/", StringComparison.Ordinal) + 1);
            
            var handCurrentCosmetic = _hand.currentCosmetics.FirstOrDefault(x => x.cosmeticInfo.id == cosmeticId);
            
            if (!handCurrentCosmetic || !PackManager.HandCosmeticPacksDict.TryGetValue(handCurrentCosmetic.cosmeticInfo.id, out var pack))
                return null;

            var emote = pack?.ExtendedCosmeticData?.emotes?.FirstOrDefault(e => e.id == emoteId && e.Sprites.Count > 0);
            
            if(emote == null) return null;

            cosmetic = pack;
            return emote;
        }
        else
        {
            foreach (var handCurrentCosmetic in _hand.currentCosmetics)
            {
                if (!handCurrentCosmetic || !PackManager.HandCosmeticPacksDict.TryGetValue(handCurrentCosmetic.cosmeticInfo.id, out var pack))
                    return null;
                
                var emote = pack?.ExtendedCosmeticData?.emotes?.FirstOrDefault(e => e.id == id && e.Sprites.Count > 0);

                if(emote == null) return null;
                
                cosmetic = pack;
                return emote;
            }
        }

        return null;
    }

    public void ApplyOffset()
    {
        if (_currentEmote == null) return;
        
        float side = IsLeft ? -1f : 1f;
        _hand.handSway.targetOffset = new Vector3(_currentEmote.position.x * -side, _currentEmote.position.y, _currentEmote.position.z);
    }

    public void ApplyScale()
    {
        if (_currentEmote == null)
        {
            _hand.handModel.localScale = originalScale;
            return;
        }
        
        _hand.handModel.localScale = Vector3.Lerp(_hand.handModel.localScale, Vector3.Scale(_currentEmote.Scale, baseScaleFactor), Time.deltaTime * 6f);
    }

    public void ApplyRotation()
    {
        if (_currentEmote == null) return;
        
        float side = IsLeft ? -1f : 1f;
        var rotation = Quaternion.Euler(0, 0, _currentEmote.Rotation * side);
        _hand.handSway.transform.localRotation = Quaternion.Lerp(_hand.handSway.transform.localRotation,
            rotation *
            Quaternion.Euler(Vector3.ClampMagnitude(Random.insideUnitSphere * _hand.handSway.shakeAmount * 30f,
                20.5f)) * Quaternion.Euler(0.0f, 0.0f,
                Mathf.Sin(_hand.handSway.rockAmount + Time.time) + _hand.handSway.parameters.bobBaseRotation * side),
            Time.deltaTime * 6f);
    }

    public AudioClip? GetEmoteSound()
    {
        if (_emoteAudioSource && _emoteAudioSource.isPlaying) _emoteAudioSource.Stop();
        if (_currentEmote?.SoundClips == null || _currentEmote.SoundClips.Count == 0) return null;

        switch (_currentEmote.SoundPlayMode)
        {
            case SoundPlayMode.Random:
                return _currentEmote.SoundClips[Random.Range(0, _currentEmote.SoundClips.Count)];
            case SoundPlayMode.Sequential:
                var clip = _currentEmote.SoundClips[_emoteSoundIndex];
                _emoteSoundIndex = (_emoteSoundIndex + 1) % _currentEmote.SoundClips.Count;
                return clip;
        }

        return null;
    }

    public void StopEmote()
    {
        if (_currentEmote == null) return;

        ModLogger.Debug("Stopped Emote " + _currentEmote.name);
        if (_currentEmote.SoundLoop)
            _emoteAudioSource?.Stop();
        _emoteAudioSource = null;
        _currentEmote = null;
    }
}