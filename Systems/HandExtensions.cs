using System;
using System.Collections.Generic;
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

    private bool IsLeft => _hand.id == 0;

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

        var keyBinds = IsLeft ? RHConfig.EmoteKeysLeft : RHConfig.EmoteKeysRight;

        foreach (var cosmetic in _hand.currentCosmetics)
        {
            if (!PackManager.HandCosmeticPacksDict.TryGetValue(cosmetic.cosmeticData.id, out var pack))
                continue;

            if (pack.ExtendedCosmeticData.emotes == null || pack.ExtendedCosmeticData.emotes.Count == 0)
                continue;

            for (int i = 0; i < Mathf.Min(pack.ExtendedCosmeticData.emotes.Count, RHConfig.MaxEmotes); i++)
            {
                if (keyBinds[i].Value == KeyCode.None) continue;
                var emote = pack.ExtendedCosmeticData.emotes[i];
                if (emote.Sprites.Count == 0) continue;

                if (InputUtility.GetKeyDown(keyBinds[i].Value))
                {
                    if (RHConfig.EmoteToggles[i].Value && _currentEmote == emote)
                    {
                        StopEmote();
                    }
                    else
                    {
                        SetEmote(emote);
                    }
                    
                    break;
                }

                if(InputUtility.GetKeyUp(keyBinds[i].Value) && !RHConfig.EmoteToggles[i].Value && _currentEmote == emote)
                {
                    StopEmote();
                    break;
                }
            }
        }
    }

    public static bool TryGet(ENT_Player.Hand? hand, out HandExtensions? ext)
    {
        ext = null;
        return hand != null && _handExtensionsMap.TryGetValue(hand, out ext);
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
        if (_currentEmote != null && !force || emote == null) return;
        bool changedEmote = _currentEmote != emote;

        _currentEmote = emote;
        if (changedEmote)
            _emoteAudioSource = AudioUtility.PlaySound(
                GetEmoteSound(), _hand.handModel.position, _hand.handModel,
                loop: _currentEmote.SoundLoop, bypassEffects: true, mixerType: AudioMixerType.Sfx);

        if (_currentEmote.PlayMode is EmotePlayMode.Loop or EmotePlayMode.Once)
            _emoteTime = Time.time;
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