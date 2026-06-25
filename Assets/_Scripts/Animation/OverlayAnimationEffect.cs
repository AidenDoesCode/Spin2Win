using UnityEngine;

// Plays a one-shot AnimationClip at a world position (typically on an enemy
// when hit) without interrupting the target's own walk/impact animations.
public class OverlayAnimationEffect : MonoBehaviour
{
    private AnimationClipPlayer animPlayer;
    private AnimationClip clip;

    public static void PlayAt(Transform anchor, AnimationClip animationClip, int sortingOrderOffset = 5)
    {
        if (anchor == null || animationClip == null) return;

        var go = new GameObject(animationClip.name + "_Effect", typeof(SpriteRenderer));
        go.transform.position = anchor.position;

        var sr = go.GetComponent<SpriteRenderer>();
        var anchorSr = anchor.GetComponent<SpriteRenderer>();
        if (anchorSr == null) anchorSr = anchor.GetComponentInChildren<SpriteRenderer>();
        if (anchorSr != null)
            sr.sortingOrder = anchorSr.sortingOrder + sortingOrderOffset;

        var effect = go.AddComponent<OverlayAnimationEffect>();
        effect.Begin(animationClip);
    }

    private void Begin(AnimationClip animationClip)
    {
        clip = animationClip;
        animPlayer = new AnimationClipPlayer(gameObject);
        animPlayer.Play(clip, loop: false);
    }

    private void Update()
    {
        if (animPlayer != null && animPlayer.Tick(Time.deltaTime))
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        animPlayer?.Dispose();
    }
}
