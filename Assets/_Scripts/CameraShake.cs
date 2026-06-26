using System.Collections;
using UnityEngine;

// New: lightweight camera shake. Attach to the Main Camera (or any camera
// you want to shake) -- nothing else needs to reference it directly, other
// scripts just call CameraShake.Instance?.Shake(duration, magnitude).
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Defaults")]
    [Tooltip("Used when Shake() is called with no arguments.")]
    public float defaultDuration = 0.1f;
    [Tooltip("Max random offset (world units) applied on X/Y while shaking.")]
    public float defaultMagnitude = 0.05f;

    private Vector3 basePosition;
    private Coroutine activeShake;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);

        basePosition = transform.localPosition;
    }

    public void Shake() => Shake(defaultDuration, defaultMagnitude);

    public void Shake(float duration, float magnitude)
    {
        if (activeShake != null) StopCoroutine(activeShake);
        activeShake = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float offsetX = Random.Range(-magnitude, magnitude);
            float offsetY = Random.Range(-magnitude, magnitude);
            transform.localPosition = basePosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = basePosition;
        activeShake = null;
    }
}
