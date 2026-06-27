using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shared "How To Play" overlay builder, used by both MainMenuUI and
// PauseMenuUI so the two don't duplicate the same programmatic UI
// construction (mirrors SettingsSliderUI's role for the volume panel).
public static class InstructionsPanelUI
{
    private const string HowToPlayText =
        "SPIN THE SHOP\n" +
        "Between rounds, spend gold to reroll the shop for new towers and upgrade cards. Lock a card to keep it through your next reroll.\n\n" +
        "PLACE YOUR TOWERS\n" +
        "Drag a tower card from your inventory onto the grid to place it. Click a placed tower to see its stats and range.\n\n" +
        "USE UPGRADE CARDS\n" +
        "Global cards (heal, gold/round, etc) apply with the Use button. Tower-targeted cards (fire rate/range/damage) get dragged directly onto a placed tower.\n\n" +
        "SURVIVE THE ROUND\n" +
        "When the buy-phase timer runs out (or you hit Continue), enemies spawn and attack your base. Defend it as long as you can -- the round counter and your gold both carry your run forward.\n\n" +
        "PAUSE ANYTIME\n" +
        "Press Esc during a run to pause, adjust settings, or check this screen again.";

    public static GameObject Build(Transform parent, Color accentColor)
    {
        GameObject backdropObj = new GameObject("InstructionsPanel");
        backdropObj.transform.SetParent(parent, false);
        RectTransform backdropRT = backdropObj.AddComponent<RectTransform>();
        backdropRT.anchorMin = Vector2.zero;
        backdropRT.anchorMax = Vector2.one;
        backdropRT.offsetMin = backdropRT.offsetMax = Vector2.zero;
        Image backdrop = backdropObj.AddComponent<Image>();
        backdrop.color = new Color(0.078f, 0.012f, 0.012f, 0.85f);

        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(backdropObj.transform, false);
        RectTransform panelRT = panelObj.AddComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(820f, 640f);
        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0.2f, 0.031f, 0.031f, 0.97f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -24f);
        titleRT.sizeDelta = new Vector2(0f, 50f);
        var title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "HOW TO PLAY";
        title.fontSize = 36;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = accentColor;

        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(panelObj.transform, false);
        RectTransform bodyRT = bodyObj.AddComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.offsetMin = new Vector2(36f, 30f);
        bodyRT.offsetMax = new Vector2(-36f, -90f);
        var body = bodyObj.AddComponent<TextMeshProUGUI>();
        body.text = HowToPlayText;
        body.fontSize = 19;
        body.lineSpacing = 4f;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.color = Color.white;

        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(panelObj.transform, false);
        RectTransform closeRT = closeObj.AddComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1f, 1f);
        closeRT.anchorMax = new Vector2(1f, 1f);
        closeRT.pivot = new Vector2(1f, 1f);
        closeRT.anchoredPosition = new Vector2(-16f, -16f);
        closeRT.sizeDelta = new Vector2(48f, 48f);
        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = new Color(0.8157f, 0f, 0f, 1f);
        Button closeButton = closeObj.AddComponent<Button>();
        closeButton.onClick.AddListener(() => backdropObj.SetActive(false));

        GameObject closeLabelObj = new GameObject("Label");
        closeLabelObj.transform.SetParent(closeObj.transform, false);
        RectTransform closeLabelRT = closeLabelObj.AddComponent<RectTransform>();
        closeLabelRT.anchorMin = Vector2.zero;
        closeLabelRT.anchorMax = Vector2.one;
        closeLabelRT.offsetMin = closeLabelRT.offsetMax = Vector2.zero;
        var closeLabel = closeLabelObj.AddComponent<TextMeshProUGUI>();
        closeLabel.text = "X";
        closeLabel.fontSize = 22;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.color = Color.white;

        return backdropObj;
    }
}
