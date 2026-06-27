using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// Shared builder for the Music/SFX volume sliders, used by both PauseMenuUI
// and MainMenuUI so the two settings panels don't duplicate the same ~50
// lines of programmatic UI construction.
public static class SettingsSliderUI
{
    public static void BuildVolumeSlider(Transform parent, string label, Vector2 anchoredPosition,
        float initialValue, Color fillColor, UnityAction<float> onValueChanged)
    {
        GameObject labelObj = new GameObject($"{label}Label");
        labelObj.transform.SetParent(parent, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 1f);
        labelRT.anchorMax = new Vector2(0f, 1f);
        labelRT.pivot = new Vector2(0f, 1f);
        labelRT.anchoredPosition = anchoredPosition + new Vector2(10f, 12f);
        labelRT.sizeDelta = new Vector2(100f, 24f);
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 18;
        labelText.color = Color.white;
        labelObj.AddComponent<PixelTextShadow>().enableShadow = true;

        GameObject sliderObj = new GameObject($"{label}Slider");
        sliderObj.transform.SetParent(parent, false);
        RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0f, 1f);
        sliderRT.anchorMax = new Vector2(0f, 1f);
        sliderRT.pivot = new Vector2(0f, 1f);
        sliderRT.anchoredPosition = anchoredPosition + new Vector2(110f, 0f);
        sliderRT.sizeDelta = new Vector2(220f, 20f);

        Image background = sliderObj.AddComponent<Image>();
        background.color = new Color(0.122f, 0.020f, 0.020f, 1f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1.5f;
        slider.value = initialValue;

        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(2f, 2f);
        fillAreaRT.offsetMax = new Vector2(-2f, -2f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRT = fillObj.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = fillColor;

        slider.fillRect = fillRT;
        slider.targetGraphic = fillImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.onValueChanged.AddListener(onValueChanged);
    }

    // Builds the whole "Music + SFX" panel (background + both sliders),
    // bound to MusicManager/SfxSettings, sized to fit two stacked sliders.
    public static GameObject BuildVolumePanel(Transform parent, Vector2 anchoredPosition, Color fillColor)
    {
        GameObject panelObj = new GameObject("SettingsPanel");
        panelObj.transform.SetParent(parent, false);
        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(360f, 110f);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);

        BuildVolumeSlider(panelObj.transform, "Music", new Vector2(0f, -25f),
            MusicManager.Instance != null ? MusicManager.Instance.volume : 0.6f,
            fillColor,
            v => MusicManager.Instance?.SetVolume(v));

        BuildVolumeSlider(panelObj.transform, "SFX", new Vector2(0f, -75f),
            SfxSettings.Volume,
            fillColor,
            v => { if (SfxSettings.Instance != null) SfxSettings.Instance.volume = v; });

        return panelObj;
    }
}
