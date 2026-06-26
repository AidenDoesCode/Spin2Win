using System.Collections;
using UnityEngine;

// New: reusable squash-and-stretch pop. Add (or AddComponent at runtime) to
// any transform and call Play() -- it snaps to a squashed scale then eases
// back to whatever scale it was already at, so it works regardless of any
// existing scale multiplier/flip already applied to that transform.
public class SpriteSquash : MonoBehaviour
{
    [Header("Squash & Stretch")]
    public float squashScaleX = 1.2f;
    public float squashScaleY = 0.8f;
    public float duration = 0.15f;

    private Coroutine activeRoutine;

    public void Play() => Play(squashScaleX, squashScaleY, duration);

    public void Play(float scaleX, float scaleY, float overrideDuration)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(SquashRoutine(scaleX, scaleY, overrideDuration));
    }

    private IEnumerator SquashRoutine(float scaleX, float scaleY, float dur)
    {
        Vector3 resting = transform.localScale;
        Vector3 squashed = new Vector3(resting.x * scaleX, resting.y * scaleY, resting.z);

        transform.localScale = squashed;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squashed, resting, elapsed / dur);
            yield return null;
        }

        transform.localScale = resting;
        activeRoutine = null;
    }
}
