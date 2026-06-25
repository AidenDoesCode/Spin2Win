using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Plays an AnimationClip via a minimal PlayableGraph bound to an Animator --
// no AnimatorController/override asset needs to be authored. The Animator is
// added automatically if the target doesn't already have one and is never
// given a controller; Playables drive it directly. This is the supported
// runtime path for sprite-swap clips (object-reference curves), which plain
// AnimationClip.SampleAnimation did not reliably apply without an Animator
// present.
public class AnimationClipPlayer
{
    private readonly PlayableGraph graph;
    private readonly PlayableOutput output;
    private AnimationClipPlayable clipPlayable;
    private AnimationClip currentClip;
    private double time;
    private bool looping;

    public AnimationClipPlayer(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator == null) animator = target.AddComponent<Animator>();

        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        graph = PlayableGraph.Create(target.name + "_AnimationClipPlayer");
        output = AnimationPlayableOutput.Create(graph, "Output", animator);
    }

    public void Play(AnimationClip clip, bool loop)
    {
        if (clip == null || !graph.IsValid()) return;

        if (clipPlayable.IsValid())
            clipPlayable.Destroy();

        currentClip = clip;
        looping = loop;
        time = 0d;

        clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetTime(time);
        output.SetSourcePlayable(clipPlayable);

        if (!graph.IsPlaying()) graph.Play();
        graph.Evaluate();
    }

    // Advances playback by deltaTime. Returns true on the frame a non-looping
    // clip reaches its end, so the caller can switch back to another clip.
    public bool Tick(float deltaTime)
    {
        if (currentClip == null || !clipPlayable.IsValid()) return false;

        time += deltaTime;
        double length = Mathf.Max(0.0001f, currentClip.length);

        if (time < length)
        {
            clipPlayable.SetTime(time);
            graph.Evaluate();
            return false;
        }

        if (looping)
        {
            time %= length;
            clipPlayable.SetTime(time);
            graph.Evaluate();
            return false;
        }

        time = length;
        clipPlayable.SetTime(time);
        graph.Evaluate();
        return true;
    }

    // Native graph handles aren't garbage collected -- callers must invoke
    // this from OnDestroy (or equivalent) or the graph leaks.
    public void Dispose()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
