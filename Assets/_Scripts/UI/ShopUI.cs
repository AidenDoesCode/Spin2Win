using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Full-screen shop modal. Auto pops up when the buy phase starts; can be
// closed and reopened (via ShopToggleButtonUI) without affecting purchase logic.
public class ShopUI : MonoBehaviour
{
    [Header("Layout")]
    public float cardWidth = 350f;
    public float cardHeight = 475f;
    public float cardGap = 45f;
    public float padding = 60f;
    public float titleRowHeight = 90f;
    public float rerollRowGap = 14f;
    public float topGap = 30f;
    public float rerollButtonWidth = 200f;
    public float rerollButtonHeight = 60f;
    public float closeButtonSize = 80f;

    [Header("Failed Purchase Feedback")]
    public float shakeDuration = 0.3f;
    public float shakeMagnitude = 15f;

    [Header("Appear Animation")]
    public float appearDuration = 0.5f;
    public float appearSpinDegrees = 360f;

    [Header("Item Spin Reveal")]
    [Tooltip("How long the first (leftmost) card spins before revealing its real offer.")]
    public float itemSpinBaseDuration = 1.2f;
    [Tooltip("Extra spin time added per card index, so they settle one by one like slot reels.")]
    public float itemSpinStaggerPerCard = 0.4f;
    [Tooltip("How often the spinning card's displayed value flickers to a random offer.")]
    public float itemSpinTickInterval = 0.06f;

    [Header("Colors")]
    public Color backdropColor     = new Color(0f, 0f, 0f, 0.65f);
    public Color panelColor        = new Color(0.102f, 0.102f, 0.180f, 0.97f);
    public Color cardColor         = new Color(0.059f, 0.059f, 0.137f, 1f);
    public Color cardBorderColor   = new Color(0.353f, 0.353f, 0.604f, 1f);
    public Color purchasedColor    = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color affordableColor   = new Color(0.878f, 0.753f, 0.376f, 1f);
    public Color unaffordableColor = new Color(0.5f, 0.2f, 0.2f, 1f);

    [Header("Rarity Colors")]
    public Color commonRarityColor    = new Color(0.65f, 0.65f, 0.65f, 1f);
    public Color uncommonRarityColor  = new Color(0.35f, 0.85f, 0.4f, 1f);
    public Color rareRarityColor      = new Color(0.3f, 0.55f, 0.95f, 1f);
    public Color epicRarityColor      = new Color(0.65f, 0.3f, 0.9f, 1f);
    public Color legendaryRarityColor = new Color(0.95f, 0.75f, 0.2f, 1f);

    private RectTransform innerPanel;
    private readonly List<GameObject> cards = new List<GameObject>();
    private readonly List<Coroutine> itemSpinRoutines = new List<Coroutine>();
    private readonly List<Coroutine> rarityGlowRoutines = new List<Coroutine>();
    private SpinFortShopManager shop;
    private TextMeshProUGUI rerollLabel;
    private Button rerollButton;
    private RectTransform rerollRT;
    private Image rerollImg;
    private Coroutine appearRoutine;
    private Coroutine rerollFlashRoutine;

    private void Awake()
    {
        BuildBackdrop();
        BuildInnerPanel();
        BuildTitle();
        BuildCloseButton();
        BuildRerollButton();
    }

    private void Start()
    {
        shop = SpinFortShopManager.Instance;
        if (shop == null) shop = FindAnyObjectByType<SpinFortShopManager>();

        if (shop != null)
        {
            shop.ShopOpened += OnShopOpened;
            shop.ShopClosed += OnShopClosed;
            shop.ShopRefreshed += RefreshCards;
            rerollLabel.text = $"Reroll ({shop.rerollCost})";
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ScoreChanged += OnScoreChanged;

        if (shop != null && shop.IsOpen)
            Show();
        else
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (shop != null)
        {
            shop.ShopOpened -= OnShopOpened;
            shop.ShopClosed -= OnShopClosed;
            shop.ShopRefreshed -= RefreshCards;
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ScoreChanged -= OnScoreChanged;
    }

    private void OnShopOpened() => Show();
    private void OnShopClosed() => Hide();
    private void OnScoreChanged(int score)
    {
        if (gameObject.activeSelf) RefreshCards();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        BuildCardsWithSpinReveal();

        if (appearRoutine != null) StopCoroutine(appearRoutine);
        appearRoutine = StartCoroutine(PlayAppearAnimation());
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        if (appearRoutine != null) StopCoroutine(appearRoutine);
        appearRoutine = StartCoroutine(PlayDisappearAnimation());
    }

    private IEnumerator PlayAppearAnimation()
    {
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            float t = elapsed / appearDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            innerPanel.localScale = Vector3.one * eased;
            innerPanel.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(appearSpinDegrees, 0f, eased));
            elapsed += Time.deltaTime;
            yield return null;
        }

        innerPanel.localScale = Vector3.one;
        innerPanel.localRotation = Quaternion.identity;
        appearRoutine = null;
    }

    // The shop "slides away" once a round kicks off, instead of just vanishing.
    private IEnumerator PlayDisappearAnimation()
    {
        Vector3 startScale = innerPanel.localScale;
        float elapsed = 0f;
        while (elapsed < appearDuration)
        {
            float t = elapsed / appearDuration;
            float eased = t * t;
            innerPanel.localScale = Vector3.Lerp(startScale, Vector3.zero, eased);
            innerPanel.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -appearSpinDegrees, eased));
            elapsed += Time.deltaTime;
            yield return null;
        }

        innerPanel.localScale = Vector3.zero;
        gameObject.SetActive(false);
        appearRoutine = null;
    }

    private void OnRerollClicked()
    {
        if (shop == null) return;
        if (shop.TryReroll())
            BuildCardsWithSpinReveal();
    }

    private void RefreshCards() => BuildCards(spin: false);
    private void BuildCardsWithSpinReveal() => BuildCards(spin: true);

    private void BuildCards(bool spin)
    {
        foreach (Coroutine routine in itemSpinRoutines)
            if (routine != null) StopCoroutine(routine);
        itemSpinRoutines.Clear();

        foreach (Coroutine routine in rarityGlowRoutines)
            if (routine != null) StopCoroutine(routine);
        rarityGlowRoutines.Clear();

        foreach (GameObject card in cards)
            if (card != null) Destroy(card);
        cards.Clear();

        if (shop == null) return;

        bool revealed = shop.CurrentOffers.Count > 0;
        ApplyRerollButtonMode(revealed);

        if (revealed)
        {
            for (int i = 0; i < shop.CurrentOffers.Count; i++)
                cards.Add(CreateCard(shop.CurrentOffers[i], i, spin));
        }

        ResizePanel();
    }

    // Before the player has spent gold to reveal offers, the reroll button
    // takes over as a big flashing "SPIN FOR TOWERS!" prompt front-and-center.
    private void ApplyRerollButtonMode(bool revealed)
    {
        if (rerollFlashRoutine != null) StopCoroutine(rerollFlashRoutine);

        if (revealed)
        {
            rerollRT.anchorMin = new Vector2(0f, 1f);
            rerollRT.anchorMax = new Vector2(0f, 1f);
            rerollRT.pivot = new Vector2(0f, 1f);
            rerollRT.anchoredPosition = new Vector2(padding, -(titleRowHeight + rerollRowGap));
            rerollRT.sizeDelta = new Vector2(rerollButtonWidth, rerollButtonHeight);
            rerollLabel.fontSize = 26;
            rerollLabel.text = $"Reroll ({shop.rerollCost})";
            rerollImg.color = cardBorderColor;
        }
        else
        {
            rerollRT.anchorMin = new Vector2(0.5f, 0.5f);
            rerollRT.anchorMax = new Vector2(0.5f, 0.5f);
            rerollRT.pivot = new Vector2(0.5f, 0.5f);
            rerollRT.anchoredPosition = Vector2.zero;
            rerollRT.sizeDelta = new Vector2(rerollButtonWidth * 1.8f, rerollButtonHeight * 1.6f);
            rerollLabel.fontSize = 34;
            rerollLabel.text = "SPIN FOR TOWERS!";
            rerollFlashRoutine = StartCoroutine(FlashRerollButton());
        }

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
        rerollButton.interactable = shop.IsOpen && score >= shop.rerollCost;
    }

    private IEnumerator FlashRerollButton()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 4f;
            float pulse = (Mathf.Sin(t) + 1f) * 0.5f;
            rerollImg.color = Color.Lerp(affordableColor, Color.white, pulse * 0.5f);
            yield return null;
        }
    }

    private GameObject CreateCard(SpinFortShopManager.ShopOffer offer, int index, bool spin)
    {
        bool isPurchased = shop.IsOfferPurchased(index);
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
        bool canAfford = score >= offer.cost;

        float xPos = padding + index * (cardWidth + cardGap);

        GameObject borderObj = new GameObject($"ShopCard_{index}");
        borderObj.transform.SetParent(innerPanel, false);
        RectTransform borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0f, 0f);
        borderRT.anchorMax = new Vector2(0f, 0f);
        borderRT.pivot = new Vector2(0f, 0f);
        borderRT.anchoredPosition = new Vector2(xPos, padding);
        borderRT.sizeDelta = new Vector2(cardWidth, cardHeight);
        Image borderImg = borderObj.AddComponent<Image>();
        Color rarityColor = GetRarityColor(offer.rarity);
        borderImg.color = rarityColor;

        float glowIntensity = GetRarityGlowIntensity(offer.rarity);
        if (glowIntensity > 0f)
            rarityGlowRoutines.Add(StartCoroutine(RarityGlow(borderImg, rarityColor, glowIntensity, GetRarityGlowSpeed(offer.rarity))));

        GameObject innerObj = new GameObject("Inner");
        innerObj.transform.SetParent(borderObj.transform, false);
        RectTransform innerRT = innerObj.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(2f, 2f);
        innerRT.offsetMax = new Vector2(-2f, -2f);
        Image innerImg = innerObj.AddComponent<Image>();
        innerImg.color = isPurchased ? purchasedColor : cardColor;

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(innerObj.transform, false);
        RectTransform iconRT = iconObj.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.2f, 0.42f);
        iconRT.anchorMax = new Vector2(0.8f, 0.92f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.enabled = false;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(innerObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0f, 0.7f);
        labelRT.anchorMax = new Vector2(1f, 1f);
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.fontSize = 35;
        label.alignment = TextAlignmentOptions.Top;
        label.color = Color.white;

        GameObject costObj = new GameObject("Cost");
        costObj.transform.SetParent(innerObj.transform, false);
        RectTransform costRT = costObj.AddComponent<RectTransform>();
        costRT.anchorMin = new Vector2(0f, 0.28f);
        costRT.anchorMax = new Vector2(1f, 0.42f);
        costRT.offsetMin = costRT.offsetMax = Vector2.zero;
        var costLabel = costObj.AddComponent<TextMeshProUGUI>();
        costLabel.fontSize = 30;
        costLabel.alignment = TextAlignmentOptions.Center;

        GameObject buttonObj = new GameObject("BuyButton");
        buttonObj.transform.SetParent(innerObj.transform, false);
        RectTransform buttonRT = buttonObj.AddComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.08f, 0.06f);
        buttonRT.anchorMax = new Vector2(0.92f, 0.26f);
        buttonRT.offsetMin = buttonRT.offsetMax = Vector2.zero;
        Image buttonImg = buttonObj.AddComponent<Image>();
        buttonImg.color = affordableColor;
        Button button = buttonObj.AddComponent<Button>();

        GameObject buttonLabelObj = new GameObject("Label");
        buttonLabelObj.transform.SetParent(buttonObj.transform, false);
        RectTransform buttonLabelRT = buttonLabelObj.AddComponent<RectTransform>();
        buttonLabelRT.anchorMin = Vector2.zero;
        buttonLabelRT.anchorMax = Vector2.one;
        buttonLabelRT.offsetMin = buttonLabelRT.offsetMax = Vector2.zero;
        var buttonLabel = buttonLabelObj.AddComponent<TextMeshProUGUI>();
        buttonLabel.fontSize = 32;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.color = Color.black;

        int capturedIndex = index;
        button.onClick.AddListener(() => OnBuyClicked(capturedIndex, borderObj));

        // Clicking anywhere on the card besides the Buy button itself opens
        // the stat/description popup. No-ops for non-tower offers (buffs etc).
        TowerCardClickHandler clickHandler = borderObj.AddComponent<TowerCardClickHandler>();
        clickHandler.tower = offer.towerReward;

        if (spin)
        {
            button.interactable = false;
            buttonLabel.text = "...";
            Coroutine routine = StartCoroutine(SpinThenReveal(offer, index, isPurchased, canAfford, label, costLabel, iconImg, button, buttonLabel));
            itemSpinRoutines.Add(routine);
        }
        else
        {
            ApplyFinalCardContent(offer, isPurchased, canAfford, label, costLabel, iconImg, button, buttonLabel);
        }

        return borderObj;
    }

    private Color GetRarityColor(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Uncommon:  return uncommonRarityColor;
            case CardRarity.Rare:      return rareRarityColor;
            case CardRarity.Epic:      return epicRarityColor;
            case CardRarity.Legendary: return legendaryRarityColor;
            default:                   return commonRarityColor;
        }
    }

    // Higher rarities pulse brighter (more "sparkly"); Common stays static.
    private float GetRarityGlowIntensity(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Uncommon:  return 0.15f;
            case CardRarity.Rare:      return 0.25f;
            case CardRarity.Epic:      return 0.4f;
            case CardRarity.Legendary: return 0.6f;
            default:                   return 0f;
        }
    }

    private float GetRarityGlowSpeed(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Uncommon:  return 1.5f;
            case CardRarity.Rare:      return 2.2f;
            case CardRarity.Epic:      return 3f;
            case CardRarity.Legendary: return 4f;
            default:                   return 0f;
        }
    }

    private IEnumerator RarityGlow(Image borderImg, Color baseColor, float intensity, float speed)
    {
        float t = 0f;
        while (borderImg != null)
        {
            t += Time.deltaTime * speed;
            float pulse = (Mathf.Sin(t) + 1f) * 0.5f;
            borderImg.color = Color.Lerp(baseColor, Color.white, pulse * intensity);
            yield return null;
        }
    }

    private IEnumerator SpinThenReveal(SpinFortShopManager.ShopOffer offer, int index, bool isPurchased, bool canAfford,
        TextMeshProUGUI label, TextMeshProUGUI costLabel, Image iconImg, Button button, TextMeshProUGUI buttonLabel)
    {
        float duration = itemSpinBaseDuration + index * itemSpinStaggerPerCard;
        float elapsed = 0f;
        float tickTimer = 0f;
        List<SpinFortShopManager.ShopOffer> pool = shop != null ? shop.possibleOffers : null;

        while (elapsed < duration)
        {
            if (label == null || costLabel == null || iconImg == null) yield break;

            tickTimer += Time.deltaTime;
            if (tickTimer >= itemSpinTickInterval && pool != null && pool.Count > 0)
            {
                tickTimer = 0f;
                var randomOffer = pool[Random.Range(0, pool.Count)];
                label.text = randomOffer.label;
                costLabel.text = $"{randomOffer.cost} pts";
                costLabel.color = affordableColor;

                Sprite randomIcon = randomOffer.towerReward != null ? randomOffer.towerReward.icon : null;
                iconImg.sprite = randomIcon;
                iconImg.enabled = randomIcon != null;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (label == null || costLabel == null || iconImg == null || button == null || buttonLabel == null) yield break;
        ApplyFinalCardContent(offer, isPurchased, canAfford, label, costLabel, iconImg, button, buttonLabel);
    }

    private void ApplyFinalCardContent(SpinFortShopManager.ShopOffer offer, bool isPurchased, bool canAfford,
        TextMeshProUGUI label, TextMeshProUGUI costLabel, Image iconImg, Button button, TextMeshProUGUI buttonLabel)
    {
        label.text = offer.label;

        costLabel.text = $"{offer.cost} pts";
        costLabel.color = canAfford ? affordableColor : unaffordableColor;

        Sprite icon = offer.towerReward != null ? offer.towerReward.icon : null;
        iconImg.sprite = icon;
        iconImg.enabled = icon != null;

        buttonLabel.text = isPurchased ? "SOLD" : "BUY";
        button.interactable = !isPurchased && canAfford;
    }

    private void OnBuyClicked(int index, GameObject card)
    {
        if (shop == null) return;

        bool success = shop.TryBuyOffer(index);
        if (!success)
            StartCoroutine(ShakeCard(card));
    }

    private IEnumerator ShakeCard(GameObject card)
    {
        if (card == null) yield break;
        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 originalPos = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude;
            rt.anchoredPosition = originalPos + new Vector2(offsetX, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = originalPos;
    }

    private void BuildBackdrop()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image backdrop = GetComponent<Image>();
        if (backdrop == null) backdrop = gameObject.AddComponent<Image>();
        backdrop.color = backdropColor;
    }

    private void BuildInnerPanel()
    {
        GameObject panelObj = new GameObject("InnerPanel");
        panelObj.transform.SetParent(transform, false);
        innerPanel = panelObj.AddComponent<RectTransform>();
        innerPanel.anchorMin = new Vector2(0.5f, 0.5f);
        innerPanel.anchorMax = new Vector2(0.5f, 0.5f);
        innerPanel.pivot = new Vector2(0.5f, 0.5f);

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = panelColor;
    }

    private void BuildTitle()
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(innerPanel, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -padding * 0.5f);
        titleRT.sizeDelta = new Vector2(0f, 75f);
        var title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "SPIN SHOP";
        title.fontSize = 55;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
    }

    private void BuildCloseButton()
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(innerPanel, false);
        RectTransform closeRT = closeObj.AddComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1f, 1f);
        closeRT.anchorMax = new Vector2(1f, 1f);
        closeRT.pivot = new Vector2(1f, 1f);
        closeRT.anchoredPosition = new Vector2(-padding * 0.5f, -padding * 0.5f);
        closeRT.sizeDelta = new Vector2(closeButtonSize, closeButtonSize);
        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = unaffordableColor;
        Button closeButton = closeObj.AddComponent<Button>();
        closeButton.onClick.AddListener(Hide);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(closeObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "X";
        label.fontSize = 40;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
    }

    private void BuildRerollButton()
    {
        GameObject rerollObj = new GameObject("RerollButton");
        rerollObj.transform.SetParent(innerPanel, false);
        rerollRT = rerollObj.AddComponent<RectTransform>();
        rerollRT.anchorMin = new Vector2(0f, 1f);
        rerollRT.anchorMax = new Vector2(0f, 1f);
        rerollRT.pivot = new Vector2(0f, 1f);
        rerollRT.anchoredPosition = new Vector2(padding, -(titleRowHeight + rerollRowGap));
        rerollRT.sizeDelta = new Vector2(rerollButtonWidth, rerollButtonHeight);

        rerollImg = rerollObj.AddComponent<Image>();
        rerollImg.color = cardBorderColor;
        rerollButton = rerollObj.AddComponent<Button>();
        rerollButton.onClick.AddListener(OnRerollClicked);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(rerollObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        rerollLabel = labelObj.AddComponent<TextMeshProUGUI>();
        rerollLabel.fontSize = 26;
        rerollLabel.alignment = TextAlignmentOptions.Center;
        rerollLabel.color = Color.white;
    }

    private void ResizePanel()
    {
        int count = cards.Count;
        float cardsWidth = count > 0
            ? count * cardWidth + (count - 1) * cardGap
            : cardWidth;

        float minHeaderWidth = padding * 2f + rerollButtonWidth + closeButtonSize;
        float width = Mathf.Max(padding * 2f + cardsWidth, minHeaderWidth);
        float height = padding * 2f + cardHeight + topGap + titleRowHeight + rerollRowGap + rerollButtonHeight;

        innerPanel.sizeDelta = new Vector2(width, height);
    }
}
