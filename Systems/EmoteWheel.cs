using System.Collections;
using DG.Tweening;
using ResourcefulHands.Core;
using ResourcefulHands.EmbedResources;
using UnityEngine;
using UnityEngine.UI;
using WKLib.API.Input;

namespace ResourcefulHands.Systems;

internal class EmoteWheel : MonoBehaviour
{
    private Canvas _canvas;
    private RawImage _centerButton;
    private RawImage[] _wheelButtons;
    private Image[] _wheelEmotes;
    private RawImage _cursor;
    private Image _emoteLeft;
    private Image _emoteRight;

    private Vector2 _cursorPos;
    private static readonly Color ActiveColor = new Color(0.6981f, 0.1153f, 0.1153f, 1);
    private const string KeyInteractLeft = "Hand-Left";
    private const string KeyInteractRight = "Hand-Right";

    private static bool _active;
    public static bool IsActive => _active;
    
    private void Start()
    {
        if (!HandExtensions.TryGet(0, out var handExtensions))
            return;
        
        // Build Wheel
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = -1;
        _canvas.enabled = false;
        
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(400, 400);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        _centerButton = new GameObject("CenterButton", typeof(RectTransform)).AddComponent<RawImage>();
        _centerButton.transform.SetParent(_canvas.transform, false);
        _centerButton.texture = RHResources.TryGetTexture("EmoteWheel/Wheel_Center");
        
        var leftQuickEmoteBg = new GameObject("CenterButton", typeof(RectTransform)).AddComponent<RawImage>();
        leftQuickEmoteBg.transform.SetParent(_canvas.transform, false);
        leftQuickEmoteBg.transform.localPosition = new Vector3(-100, 0, 0);
        leftQuickEmoteBg.texture = RHResources.TryGetTexture("EmoteWheel/Wheel_HandBind");
        _emoteLeft = new GameObject($"Emote_Left", typeof(RectTransform)).AddComponent<Image>();
        _emoteLeft.transform.SetParent(leftQuickEmoteBg.transform, false);
        _emoteLeft.enabled = false;
        var emoteLeftRect = _emoteLeft.transform as RectTransform;
        emoteLeftRect?.sizeDelta = new Vector2(30, 30);
        UpdateEmoteSprite(0,RHConfig.LeftEmote.Value,_emoteLeft);

        var rightQuickEmoteBg = new GameObject("CenterButton", typeof(RectTransform)).AddComponent<RawImage>();
        rightQuickEmoteBg.transform.SetParent(_canvas.transform, false);
        rightQuickEmoteBg.transform.localPosition = new Vector3(100, 0, 0);
        rightQuickEmoteBg.texture = RHResources.TryGetTexture("EmoteWheel/Wheel_HandBind");
        _emoteRight = new GameObject($"Emote_Right", typeof(RectTransform)).AddComponent<Image>();
        _emoteRight.transform.SetParent(rightQuickEmoteBg.transform, false);
        _emoteRight.enabled = false;
        var emoteRightRect = _emoteRight.transform as RectTransform;
        emoteRightRect?.sizeDelta = new Vector2(30, 30);
        emoteRightRect?.localScale = new Vector3(-1, 1, 1);
        UpdateEmoteSprite(1,RHConfig.RightEmote.Value,_emoteRight);

        _wheelButtons = new RawImage[RHConfig.MaxEmotes];
        _wheelEmotes = new Image[RHConfig.MaxEmotes];
        float angleStep = -(360.0f / RHConfig.MaxEmotes);
        Vector3 emoteDir = Quaternion.Euler(0f, 0f, angleStep/2.0f) * Vector3.up;
        for (int i = 0; i < _wheelButtons.Length; i++)
        {
            var buttonName = $"Wheel_{i}";
            var button = new GameObject(buttonName, typeof(RectTransform)).AddComponent<RawImage>();
            button.transform.SetParent(_canvas.transform, false);
            button.texture = RHResources.TryGetTexture($"EmoteWheel/{buttonName}");
            _wheelButtons[i] = button;

            var emote = handExtensions?.GetEmote(i);
            if (emote != null)
            {
                var emoteSprite = new GameObject($"Emote_{i}", typeof(RectTransform)).AddComponent<Image>();
                emoteSprite.transform.SetParent(button.transform, false);
                emoteSprite.sprite = emote.Sprites[0];
                _wheelEmotes[i] = emoteSprite;
                
                var rectTransform = emoteSprite.transform as RectTransform;
                rectTransform?.sizeDelta = new Vector2(20, 20);
                rectTransform?.localPosition = emoteDir*35;
            }
                
            emoteDir = Quaternion.Euler(0f, 0f, angleStep) * emoteDir;
        }
        
        _cursor = new GameObject("Cursor", typeof(RectTransform)).AddComponent<RawImage>();
        _cursor.transform.SetParent(_canvas.transform, false);
        _cursor.transform.localScale = Vector3.one * 0.0625f;
        _cursor.texture = RHResources.TryGetTexture("EmoteWheel/Wheel_Cursor");
    }

    private void Activate()
    {
        ENT_Player.playerObject.camLocked = true;
        _canvas.enabled = true;
        _active = true;
        _cursorPos = Vector2.zero;
    }

    private void Deactivate()
    {
        ENT_Player.playerObject.camLocked = false;
        _canvas.enabled = false;
        _active = false;
        _cursorPos = Vector2.zero;
    }

    private void OnDestroy()
    {
        Deactivate();
    }

    private void Update()
    {
        // Wheel Toggle
        if (!_active)
        {
            if(InputUtility.GetKeyDown(RHConfig.EmoteWheelKey.Value) || InputUtility.GetKeyDown(RHConfig.EmoteWheelKeyAlt.Value))
                Activate();
            return;
        }
        
        if (InputManager.IsGamepad())
            _cursorPos = InputManager.GetLookVector() * 1.5f;
        else
            _cursorPos += InputManager.GetLookVector() * ((float)SettingsManager.settings.mouseSensitivity * 4f * Time.deltaTime);
        
        _cursorPos = Vector2.ClampMagnitude(_cursorPos, 1f);
        _cursor.transform.localPosition = new Vector3(_cursorPos.x, _cursorPos.y, 0f)*32.0f;

        bool isCenter = _cursorPos.magnitude < 0.6f;
        _centerButton.color = Color.Lerp(_centerButton.color, isCenter ? ActiveColor : Color.white, Time.deltaTime * 6);
        
        int emoteIndex = isCenter ? -1 : Mathf.FloorToInt((360-(Vector2.SignedAngle(Vector2.down, _cursorPos)+180))/36.0f) % _wheelButtons.Length;
        for (int i = 0; i < _wheelButtons.Length; i++)
        {
            bool isHovered = !isCenter && emoteIndex == i;
            _wheelButtons[i].color = Color.Lerp(_wheelButtons[i].color, isHovered ? ActiveColor : Color.white, Time.deltaTime * 6);
            _wheelButtons[i].transform.localScale = Vector3.Lerp(_wheelButtons[i].transform.localScale, isHovered ? new Vector3(1.1f,1.1f,1.1f) : Vector3.one, Time.deltaTime * 6);
        }

        if (InputManager.GetButton(KeyInteractLeft).Down || InputManager.GetButton(KeyInteractRight).Down)
        {
            StartCoroutine(CheckPressedEmotes(emoteIndex));
            Deactivate();
        }

        if ((RHConfig.ToggleWheel.Value && (InputUtility.GetKeyDown(RHConfig.EmoteWheelKey.Value) || InputUtility.GetKeyDown(RHConfig.EmoteWheelKeyAlt.Value))) ||
            (!RHConfig.ToggleWheel.Value && (InputUtility.GetKeyUp(RHConfig.EmoteWheelKey.Value) || InputUtility.GetKeyUp(RHConfig.EmoteWheelKeyAlt.Value))))
        {
            Deactivate();
        }

        if (!isCenter && (InputUtility.GetKeyUp(RHConfig.EmoteLeftKey.Value) || InputUtility.GetKeyUp(RHConfig.EmoteLeftKeyAlt.Value)))
        {
            RHConfig.LeftEmote.Value = emoteIndex;
            UpdateEmoteSprite(0,RHConfig.LeftEmote.Value,_emoteLeft);
        }
        if (!isCenter && (InputUtility.GetKeyUp(RHConfig.EmoteRightKey.Value) || InputUtility.GetKeyUp(RHConfig.EmoteRightKeyAlt.Value)))
        {
            RHConfig.RightEmote.Value = emoteIndex;
            UpdateEmoteSprite(1,RHConfig.RightEmote.Value,_emoteRight);
        }
    }

    IEnumerator CheckPressedEmotes(int emoteIndex)
    {
        yield return new WaitForSecondsRealtime(0.05f);
        
        if (InputManager.GetButton(KeyInteractLeft).Pressed && HandExtensions.TryGet(0, out var handExtensionsLeft))
        {
            if (emoteIndex == -1)
                handExtensionsLeft?.StopEmote();
            else
                handExtensionsLeft?.SetEmote(emoteIndex);
        }
        
        if (InputManager.GetButton(KeyInteractRight).Pressed && HandExtensions.TryGet(1, out var handExtensionsRight))
        {
            if (emoteIndex == -1)
                handExtensionsRight?.StopEmote();
            else
                handExtensionsRight?.SetEmote(emoteIndex);
        }
    }
    
    private void UpdateEmoteSprite(int handId, int emoteId, Image sprite)
    {
        if (!HandExtensions.TryGet(handId, out var handExtensions))
            return;
        
        var emote = handExtensions?.GetEmote(emoteId);
        if (emote != null)
        {
            sprite.sprite = emote.Sprites[0];
            sprite.enabled = true;
            sprite.transform.parent.DOPunchScale(Vector3.one * 0.1f, 0.25f);
        }
    }
}