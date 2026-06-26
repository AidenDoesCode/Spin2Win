using UnityEngine;
using TMPro;

// New: tiny floating/fading world-space text popup (e.g. "+15g" over a dying
// enemy). No prefab needed -- Spawn() builds the TextMeshPro object on the
// fly, the same way the rest of the UI in this project builds itself in code.
public class FloatingText : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float duration = 0.5f;

    private TextMeshPro label;
    private float elapsed;
    private Color startColor;

    public static FloatingText Spawn(Vector3 worldPosition, string text, Color color, float floatSpeed = 1f, float duration = 0.5f)
    {
        GameObject obj = new GameObject("FloatingText");
        obj.transform.position = worldPosition;

        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = 4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 100;

        FloatingText popup = obj.AddComponent<FloatingText>();
        popup.label = tmp;
        popup.floatSpeed = floatSpeed;
        popup.duration = duration;
        popup.startColor = color;

        return popup;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        if (label != null)
            label.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));

        if (elapsed >= duration)
            Destroy(gameObject);
    }
}
