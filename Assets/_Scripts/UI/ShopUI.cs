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
    [Tooltip("Flicker interval at the very start of the spin (fast). Tune this to match the start of your spin sound effect.")]
    public float itemSpinTickIntervalStart = 0.03f;
    [Tooltip("Flicker interval right before the card lands (slow, like a reel settling). Tune this to match the end of your spin sound effect.")]
    public float itemSpinTickIntervalEnd = 0.35f;
    [Tooltip("Shapes the fast-to-slow curve. Higher values stay fast longer before slowing sharply near the end; 1 = linear slowdown.")]
    public float itemSpinEaseExponent = 2.5f;

    [Header("Colors")]
    public Color backdropColor     = new Color(0.078f, 0.012f, 0.012f, 0.85f); // Near-black wine red
    public Color panelColor        = new Color(0.2f, 0.031f, 0.031f, 0.97f);   // Deep Maroon (casino felt)
    public Color cardBorderColor   = new Color(0.541f, 0.078f, 0.078f, 1f);    // Red Trim
    public Color purchasedColor    = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color affordableColor   = new Color(1f, 0.8431f, 0f, 1f);              // Jackpot Gold
    public Color unaffordableColor = new Color(0.8157f, 0f, 0f, 1f);              // Card Suit Red
    public Color rerollButtonColor = new Color(1f, 0.8431f, 0f, 1f);              // Jackpot Gold
    public Color cardTextColor     = new Color(0.0431f, 0.0745f, 0.1686f, 1f);    // Deep Abyss (dark text on white cards)

    [Header("Card Type Colors")]
    public Color towerCardColor    = new Color32(0x0A, 0x5C, 0x36, 0xFF);         // Felt Green (matches the placement grid)
    public Color upgradeCardColor  = new Color32(0x33, 0x08, 0x08, 0xFF);         // Velvet Crimson (instant-action event card)
    public Color cardLabelTextColor = new Color32(0xF5, 0xF5, 0xF0, 0xFF);        // Crisp Ivory (readable on the dark card bodies above)

    [Header("Card Background Art")]
    [Tooltip("Overrides towerCardColor with real art on Tower cards once assigned. Leave empty to keep the flat color.")]
    public Sprite towerCardBackgroundArt;
    [Tooltip("Overrides upgradeCardColor with real art on Upgrade cards once assigned. Leave empty to keep the flat color.")]
    public Sprite upgradeCardBackgroundArt;

    [Header("Rarity Colors")]
    public Color commonRarityColor    = new Color(0.6275f, 0.6275f, 0.6275f, 1f); // Simple Utility
    public Color uncommonRarityColor  = new Color(0f, 0.6588f, 0.5882f, 1f);      // Seafoam Teal
    public Color rareRarityColor      = new Color(0.6078f, 0.3647f, 0.8980f, 1f); // Coral Purple
    public Color epicRarityColor      = new Color(0.9451f, 0.3569f, 0.7098f, 1f); // Neon Jelly Pink
    public Color legendaryRarityColor = new Color(1f, 0.8431f, 0f, 1f);           // Jackpot Gold

    [System.Serializable]
    public class BorderArt
    {
        [Tooltip("Plays on loop on the card border if assigned. Can animate the border Image's sprite, color, scale, rotation -- anything keyed in the clip.")]
        public AnimationClip animation;
        [Tooltip("Shown when no animation clip is assigned (or as the very first frame before the clip kicks in). Leave empty to keep the flat rarity color border.")]
        public Sprite fallbackSprite;
    }

    [Header("Rarity Border Art")]
    public BorderArt commonBorderArt;
    public BorderArt uncommonBorderArt;
    public BorderArt rareBorderArt;
    public BorderArt epicBorderArt;
    public BorderArt legendaryBorderArt;

    [Header("Audio")]
    [Tooltip("The one sound effect played when Reroll/Spin is pressed. Should already contain the full spin -- fast ticking, slowdown, and landing ding -- as a single clip. The visual card flicker (Item Spin Reveal settings above) eases on its own timeline to match it.")]
    public AudioClip spinSound;
    [Range(0f, 3f)] public float spinVolume = 1f;
    [Tooltip("Cash register / card-flick sound played when a card is successfully bought.")]
    public AudioClip purchaseSound;
    [Range(0f, 3f)] public float purchaseVolume = 1f;

    private AudioSource audioSource;
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
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

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

    // The shop theme is driven directly off these events (rather than
    // indirectly through RoundManager) so it's tied precisely to whether the
    // shop itself is open, and reliably keeps looping in the background --
    // SFX all play on a separate AudioSource so they never interrupt it.
    private void OnShopOpened()
    {
        Show();
        MusicManager.Instance?.PlayBuyPhaseMusic();
    }

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
        if (spinSound != null) audioSource.PlayOneShot(spinSound, spinVolume * SfxSettings.Volume);

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

        bool revealed = shop.CurrentOffers.Exists(o => o != null);
        ApplyRerollButtonMode(revealed);

        for (int i = 0; i < shop.CurrentOffers.Count; i++)
            cards.Add(CreateCard(shop.CurrentOffers[i], i, spin && shop.CurrentOffers[i] != null));

        ResizePanel();
    }

    // Locking can leave some slots filled and others empty at the same time,
    // so the reroll button always sits in its small top-left spot -- it can
    // no longer take over the whole panel as a single all-or-nothing prompt.
    private void ApplyRerollButtonMode(bool revealed)
    {
        if (rerollFlashRoutine != null) StopCoroutine(rerollFlashRoutine);

        rerollRT.anchorMin = new Vector2(0f, 1f);
        rerollRT.anchorMax = new Vector2(0f, 1f);
        rerollRT.pivot = new Vector2(0f, 1f);
        rerollRT.anchoredPosition = new Vector2(padding, -(titleRowHeight + rerollRowGap));
        rerollRT.sizeDelta = new Vector2(rerollButtonWidth, rerollButtonHeight);
        rerollLabel.fontSize = 26;
        rerollLabel.text = revealed ? $"Reroll ({shop.rerollCost})" : "SPIN FOR TOWERS!";
        rerollImg.color = rerollButtonColor;

        if (!revealed)
            rerollFlashRoutine = StartCoroutine(FlashRerollButton());

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

    // An empty (not-yet-rerolled) slot just shows a placeholder -- the player
    // has to spend a reroll to fill it in.
    private GameObject CreateEmptyCard(int index)
    {
        float xPos = padding + index * (cardWidth + cardGap);

        GameObject borderObj = new GameObject($"ShopCard_{index}_Empty");
        borderObj.transform.SetParent(innerPanel, false);
        RectTransform borderRT = borderObj.AddComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0f, 0f);
        borderRT.anchorMax = new Vector2(0f, 0f);
        borderRT.pivot = new Vector2(0f, 0f);
        borderRT.anchoredPosition = new Vector2(xPos, padding);
        borderRT.sizeDelta = new Vector2(cardWidth, cardHeight);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = commonRarityColor;

        GameObject innerObj = new GameObject("Inner");
        innerObj.transform.SetParent(borderObj.transform, false);
        RectTransform innerRT = innerObj.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(2f, 2f);
        innerRT.offsetMax = new Vector2(-2f, -2f);
        Image innerImg = innerObj.AddComponent<Image>();
        innerImg.color = purchasedColor;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(innerObj.transform, false);
        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "?";
        label.fontSize = 60;
        label.alignment = TextAlignmentOptions.Center;
        label.color = cardTextColor;

        return borderObj;
    }

    private GameObject CreateCard(ShopCardSO offer, int index, bool spin)
    {
        if (offer == null) return CreateEmptyCard(index);

        bool isPurchased = shop.IsOfferPurchased(index);
        bool isLocked = shop.IsOfferLocked(index);
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
        float glowIntensity = GetRarityGlowIntensity(offer.rarity);
        BorderArt borderArt = GetBorderArt(offer.rarity);

        if (borderArt != null && borderArt.animation != null)
        {
            if (borderArt.fallbackSprite != null) borderImg.sprite = borderArt.fallbackSprite;
            borderImg.color = Color.white;
            PlayBorderAnimation(borderObj, borderArt.animation);
        }
        else if (borderArt != null && borderArt.fallbackSprite != null)
        {
            borderImg.sprite = borderArt.fallbackSprite;
            borderImg.color = Color.white;
            if (glowIntensity > 0f)
                rarityGlowRoutines.Add(StartCoroutine(RarityGlow(borderImg, Color.white, glowIntensity, GetRarityGlowSpeed(offer.rarity))));
        }
        else
        {
            borderImg.color = rarityColor;
            if (glowIntensity > 0f)
                rarityGlowRoutines.Add(StartCoroutine(RarityGlow(borderImg, rarityColor, glowIntensity, GetRarityGlowSpeed(offer.rarity))));
        }

        GameObject innerObj = new GameObject("Inner");
        innerObj.transform.SetParent(borderObj.transform, false);
        RectTransform innerRT = innerObj.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(2f, 2f);
        innerRT.offsetMax = new Vector2(-2f, -2f);
        Image innerImg = innerObj.AddComponent<Image>();
        Color baseCardColor = offer.rewardType == SpinFortRewardType.Tower ? towerCardColor : upgradeCardColor;
        Sprite baseCardArt = offer.rewardType == SpinFortRewardType.Tower ? towerCardBackgroundArt : upgradeCardBackgroundArt;
        innerImg.sprite = baseCardArt;
        innerImg.color = isPurchased ? purchasedColor : (baseCardArt != null ? Color.white : baseCardColor);

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
        // Narrowed from full width (1f) so the lock badge has its own clear
        // corner at top-right instead of sitting on top of the name text.
        labelRT.anchorMax = new Vector2(0.76f, 1f);
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.fontSize = 35;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.color = cardLabelTextColor;

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
        // the stat/description popup -- TowerSO stats for Tower cards,
        // ShopCardSO stats for everything else.
        if (offer.rewardType == SpinFortRewardType.Tower)
        {
            TowerCardClickHandler clickHandler = borderObj.AddComponent<TowerCardClickHandler>();
            clickHandler.tower = offer.towerReward;
        }
        else
        {
            ShopCardClickHandler clickHandler = borderObj.AddComponent<ShopCardClickHandler>();
            clickHandler.card = offer;
        }

        GameObject lockObj = new GameObject("LockButton");
        lockObj.transform.SetParent(innerObj.transform, false);
        RectTransform lockRT = lockObj.AddComponent<RectTransform>();
        // Sits in the corner freed up by narrowing the name label above, so
        // it never overlaps the tower name text.
        lockRT.anchorMin = new Vector2(0.78f, 0.86f);
        lockRT.anchorMax = new Vector2(0.98f, 1f);
        lockRT.offsetMin = lockRT.offsetMax = Vector2.zero;
        Image lockImg = lockObj.AddComponent<Image>();
        lockImg.color = isLocked ? rerollButtonColor : purchasedColor;
        Button lockButton = lockObj.AddComponent<Button>();
        lockButton.interactable = !isPurchased;

        GameObject lockLabelObj = new GameObject("Label");
        lockLabelObj.transform.SetParent(lockObj.transform, false);
        RectTransform lockLabelRT = lockLabelObj.AddComponent<RectTransform>();
        lockLabelRT.anchorMin = Vector2.zero;
        lockLabelRT.anchorMax = Vector2.one;
        lockLabelRT.offsetMin = lockLabelRT.offsetMax = Vector2.zero;
        var lockLabel = lockLabelObj.AddComponent<TextMeshProUGUI>();
        lockLabel.text = isLocked ? "LOCKED" : "LOCK";
        lockLabel.fontSize = 14;
        lockLabel.alignment = TextAlignmentOptions.Center;
        lockLabel.color = cardTextColor;

        lockButton.onClick.AddListener(() => OnLockClicked(capturedIndex));

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

    // Public so other card-rendering UIs (e.g. the upgrade inventory) can
    // reuse the exact same border art/animation instead of duplicating it.
    public BorderArt GetBorderArt(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Uncommon:  return uncommonBorderArt;
            case CardRarity.Rare:      return rareBorderArt;
            case CardRarity.Epic:      return epicBorderArt;
            case CardRarity.Legendary: return legendaryBorderArt;
            default:                   return commonBorderArt;
        }
    }

    // Uses the legacy Animation component (not Animator) so any AnimationClip
    // can just be dropped in and looped at runtime with zero extra setup --
    // no AnimatorController asset to author. Works for sprite-swap, color,
    // scale, rotation, whatever the clip keys on the border Image/RectTransform.
    public static void PlayBorderAnimation(GameObject borderObj, AnimationClip clip)
    {
        Animation anim = borderObj.GetComponent<Animation>();
        if (anim == null) anim = borderObj.AddComponent<Animation>();

        clip.legacy = true;
        anim.AddClip(clip, clip.name);
        anim.clip = clip;
        anim.wrapMode = WrapMode.Loop;
        anim.Play(clip.name);
    }

    private IEnumerator SpinThenReveal(ShopCardSO offer, int index, bool isPurchased, bool canAfford,
        TextMeshProUGUI label, TextMeshProUGUI costLabel, Image iconImg, Button button, TextMeshProUGUI buttonLabel)
    {
        float duration = itemSpinBaseDuration + index * itemSpinStaggerPerCard;
        float elapsed = 0f;
        float tickTimer = 0f;
        List<ShopCardSO> pool = shop != null ? shop.possibleOffers : null;

        while (elapsed < duration)
        {
            if (label == null || costLabel == null || iconImg == null) yield break;

            tickTimer += Time.deltaTime;

            // Flicker rate decelerates over the course of the spin -- fast at
            // first, slowing down right before landing, like a real reel.
            float progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float eased = Mathf.Pow(progress, itemSpinEaseExponent);
            float currentTickInterval = Mathf.Lerp(itemSpinTickIntervalStart, itemSpinTickIntervalEnd, eased);

            if (tickTimer >= currentTickInterval && pool != null && pool.Count > 0)
            {
                tickTimer = 0f;
                var randomOffer = pool[Random.Range(0, pool.Count)];
                label.text = randomOffer.label;
                costLabel.text = $"{randomOffer.cost} pts";
                costLabel.color = affordableColor;

                Sprite randomIcon = randomOffer.icon != null ? randomOffer.icon
                    : (randomOffer.towerReward != null ? randomOffer.towerReward.icon : null);
                iconImg.sprite = randomIcon;
                iconImg.enabled = randomIcon != null;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (label == null || costLabel == null || iconImg == null || button == null || buttonLabel == null) yield break;
        ApplyFinalCardContent(offer, isPurchased, canAfford, label, costLabel, iconImg, button, buttonLabel);
    }

    private void ApplyFinalCardContent(ShopCardSO offer, bool isPurchased, bool canAfford,
        TextMeshProUGUI label, TextMeshProUGUI costLabel, Image iconImg, Button button, TextMeshProUGUI buttonLabel)
    {
        label.text = offer.label;

        costLabel.text = $"{offer.cost} pts";
        costLabel.color = canAfford ? affordableColor : unaffordableColor;

        Sprite icon = offer.icon != null ? offer.icon
            : (offer.towerReward != null ? offer.towerReward.icon : null);
        iconImg.sprite = icon;
        iconImg.enabled = icon != null;

        buttonLabel.text = isPurchased ? "SOLD" : "BUY";
        button.interactable = !isPurchased && canAfford;
    }

    private void OnLockClicked(int index)
    {
        shop?.ToggleOfferLock(index);
    }

    private void OnBuyClicked(int index, GameObject card)
    {
        if (shop == null) return;

        bool success = shop.TryBuyOffer(index);
        if (success)
        {
            if (purchaseSound != null) audioSource.PlayOneShot(purchaseSound, purchaseVolume * SfxSettings.Volume);
        }
        else
        {
            StartCoroutine(ShakeCard(card));
        }
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
        rerollImg.color = rerollButtonColor;
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
        rerollLabel.color = cardTextColor;
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
