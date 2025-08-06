using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SummitRoster;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    private void Start()
    {
        SettingsHandler.Instance.AddSetting(new ProgressBarDisplayModeSetting());
    }

    [HarmonyPatch(typeof(RunManager), "StartRun")]
    [HarmonyPostfix]
    private static void Post_LoadIsland()
    {
        Logger.LogInfo("Patch running ProgressMap");
        GameObject progressMap = new GameObject("ProgressMap", typeof(ProgressMap));
    }
}

public class ProgressMap : MonoBehaviourPunCallbacks
{
    private GameObject _overlay;
    private GameObject _peakGo;
    private TMP_FontAsset _mainFont;
    private readonly Dictionary<Character, GameObject> _characterLabels = new();
    private const float TotalMountainHeight = 1920f; // in meters
    private const float BarHeightPixels = 800f;
    private const float LeftOffset = 60f;
    private const float BottomOffset = 50f;

    private ProgressBarDisplayMode _displayMode = ProgressBarDisplayMode.Full;
    private const float DisplayRange = 100f;

    private void Awake()
    {
        if (_overlay != null)
        {
            Object.DestroyImmediate(_overlay);
        }

        _overlay = new GameObject("ProgressMap");
        var canvas = _overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = _overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Load font
        if (_mainFont == null)
        {
            var fontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            _mainFont = fontAssets.FirstOrDefault(a => a.faceInfo.familyName == "Daruma Drop One");
        }

        // PEAK header
        _peakGo = new GameObject("PeakText", typeof(RectTransform), typeof(TextMeshProUGUI));
        _peakGo.transform.SetParent(_overlay.transform, false);

        var peakText = _peakGo.GetComponent<TextMeshProUGUI>();
        peakText.font = _mainFont;
        peakText.text = "PEAK";
        peakText.color = new Color(1f, 1f, 1f, 0.3f);

        var peakRect = _peakGo.GetComponent<RectTransform>();
        peakRect.sizeDelta = peakText.GetPreferredValues();
        peakRect.anchorMin = peakRect.anchorMax = new Vector2(0, 0.5f);
        peakRect.pivot = new Vector2(0.5f, 0f);
        peakRect.anchoredPosition = new Vector2(LeftOffset, BottomOffset + (BarHeightPixels / 2));

        // Add vertical bar
        var barGO = new GameObject("AltitudeBar");
        barGO.transform.SetParent(_overlay.transform, false);

        var barRect = barGO.AddComponent<RectTransform>();
        barRect.anchorMin = barRect.anchorMax = new Vector2(0, 0.5f);
        barRect.sizeDelta = new Vector2(10, BarHeightPixels);
        barRect.anchoredPosition = new Vector2(LeftOffset, BottomOffset);

        var barImage = barGO.AddComponent<Image>();
        barImage.color = new Color(0.75f, 0.75f, 0.69f, 0.3f);
    }

    private void Start()
    {
        _characterLabels.Clear();
        foreach (var character in Character.AllCharacters)
        {
            AddCharacter(character);
        }

        _displayMode = SettingsHandler.Instance.GetSetting<ProgressBarDisplayModeSetting>().Value;
    }

    private void LateUpdate()
    {
        _displayMode = SettingsHandler.Instance.GetSetting<ProgressBarDisplayModeSetting>().Value;

        _peakGo.SetActive(_displayMode == ProgressBarDisplayMode.Full);

        foreach (var character in Character.AllCharacters)
        {
            if (!_characterLabels.ContainsKey(character))
            {
                AddCharacter(character);
            }

            var labelGO = _characterLabels[character];

            var height = character.refs.stats.heightInMeters;
            var nickname = character.refs.view.Owner.NickName;

            var label = labelGO.GetComponentInChildren<TextMeshProUGUI>();
            label.text = $"{nickname} {height}m";
            label.gameObject.GetComponent<RectTransform>().sizeDelta = label.GetPreferredValues() * 1.1f;

            label.color = character.refs.customization.PlayerColor;
            labelGO.GetComponentInChildren<Image>().color = character.refs.customization.PlayerColor;

            float pixelY = 0;
            if (_displayMode == ProgressBarDisplayMode.Full)
            {
                var normalized = Mathf.InverseLerp(0f, TotalMountainHeight, height);
                pixelY = Mathf.Lerp(-BarHeightPixels / 2f, BarHeightPixels / 2f, normalized);
            }
            else if (_displayMode == ProgressBarDisplayMode.Centered)
            {
                var localH = Character.localCharacter.refs.stats.heightInMeters;
                var logH = Mathf.Log(localH);
                var logMin = logH - Mathf.Log(DisplayRange);
                var logMax = logH + Mathf.Log(DisplayRange);
                var logValue = Mathf.Log(height);

                // normalized now runs 0→1 over [localH/zoom … localH*zoom], with log scaling
                var normalized = Mathf.InverseLerp(logMin, logMax, logValue);
                normalized = Mathf.Clamp01(normalized);

                pixelY = Mathf.Lerp(-BarHeightPixels / 2f, BarHeightPixels / 2f, normalized);

            }

            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(LeftOffset + 50f, BottomOffset + pixelY);
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log("Adding player to map");
        StartCoroutine(WaitAndAddPlayer(newPlayer));
    }

    private IEnumerator WaitAndAddPlayer(Photon.Realtime.Player newPlayer)
    {
        yield return new WaitUntil(() => PlayerHandler.GetPlayerCharacter(newPlayer) != null);

        var map = GameObject.Find("ProgressMap").GetComponent<ProgressMap>();
        map.AddCharacter(PlayerHandler.GetPlayerCharacter(newPlayer));
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player leavingPlayer)
    {
        Debug.Log("Removing player from map");
        var map = GameObject.Find("ProgressMap").GetComponent<ProgressMap>();
        map.RemoveCharacter(PlayerHandler.GetPlayerCharacter(leavingPlayer));
    }

    public void AddCharacter(Character character)
    {
        Debug.Log($"Adding character {character}");

        if (!_characterLabels.ContainsKey(character))
        {
            // save the player's name
            var nickname = character.refs.view.Owner.NickName;

            // Parent label object
            var labelGO = new GameObject($"Label_{nickname}");
            labelGO.transform.SetParent(_overlay.transform, false);

            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0, 0.5f);

            // Dot marker
            var markerGO = new GameObject($"Marker");
            markerGO.transform.SetParent(labelGO.transform, false);

            var marker = markerGO.AddComponent<Image>();
            marker.color = character.refs.customization.PlayerColor;

            var markerRect = markerGO.GetComponent<RectTransform>();
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 1);
            markerRect.sizeDelta = new Vector2(10, 5);

            // Text label
            var textGO = new GameObject($"Text");
            textGO.transform.SetParent(labelGO.transform, false);

            var labelText = textGO.AddComponent<TextMeshProUGUI>();
            labelText.color = character.refs.customization.PlayerColor;
            labelText.font = _mainFont;
            labelText.fontSize = 18;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = new Vector2(0, 0.5f);
            textRect.pivot = new Vector2(0, 0.5f);
            textRect.anchoredPosition = new Vector2(20, 0);

            _characterLabels[character] = labelGO;
        }
    }

    public void RemoveCharacter(Character character)
    {
        var labelGO = _characterLabels[character];
        DestroyImmediate(labelGO);
        _characterLabels.Remove(character);
    }
}