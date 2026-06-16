using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Keeps a group of TMP text elements the same width so their right-aligned content stays vertically in-line.
/// Attach this to a parent GameObject that contains the HUD text elements (e.g. top-right container) and
/// assign the TMP_Text references in the inspector.
/// The script will set a LayoutElement.preferredWidth on each target to the maximum preferred width among them.
/// </summary>
public class HUDAligner : MonoBehaviour
{
    [Tooltip("Text elements to align (right-aligned TMP text recommended)")]
    public TMP_Text[] targets;

    [Tooltip("Extra pixels to add to the computed width for padding")]
    public float extraPadding = 8f;

    [Tooltip("Update every frame. Disable for small performance improvement if texts change rarely.")]
    public bool updateEveryFrame = false;

    void Start()
    {
        AlignOnce();
    }

    void LateUpdate()
    {
        if (updateEveryFrame) AlignOnce();
    }

    public void AlignOnce()
    {
        if (targets == null || targets.Length == 0) return;

        float maxWidth = 0f;
        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            if (t == null) continue;

            // ensure right alignment so text lines up on the right edge
            t.alignment = TextAlignmentOptions.TopRight;

            // Ask TMP for preferred width for the current text
            Vector2 pref = t.GetPreferredValues(t.text, 10000f, t.rectTransform.rect.height);
            if (pref.x > maxWidth) maxWidth = pref.x;
        }

        maxWidth += extraPadding;

        for (int i = 0; i < targets.Length; i++)
        {
            var t = targets[i];
            if (t == null) continue;

            var le = t.GetComponent<LayoutElement>();
            if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = maxWidth;
            // let height size naturally
            le.preferredHeight = -1f;
        }
    }
}
