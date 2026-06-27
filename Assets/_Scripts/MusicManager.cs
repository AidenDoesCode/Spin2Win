using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent across scene loads (like ScoreManager). Drag your music clips on
// here and call the Play*Music() methods from wherever the corresponding
// event happens -- RoundContinueUI, GameOverUI, MainMenuUI already do this.
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Tracks")]
    public AudioClip mainMenuMusic;
    [Tooltip("Plays while the player is buying/placing towers (the setup phase).")]
    public AudioClip buyPhaseMusic;
    [Tooltip("Plays once the Continue/Start Round button is pressed and the wave kicks off.")]
    public AudioClip combatMusic;

    [Header("Game Over Effect")]
    [Tooltip("On game over, mainMenuMusic itself replays -- muffled and pitched down -- instead of a separate track.")]
    [Range(0.1f, 1f)] public float gameOverPitch = 0.75f;
    [Tooltip("Low-pass filter cutoff in Hz applied on game over. Lower = more muffled.")]
    public float gameOverLowPassCutoff = 1200f;

    [Header("Low Health Tension")]
    [Tooltip("Pitch applied to whichever normal track is playing once the base is at 0 health (right before Game Over takes over). 1 = unaffected at full health.")]
    [Range(0.1f, 1f)] public float lowHealthPitch = 0.85f;
    [Tooltip("Low-pass cutoff in Hz at 0 health. Higher = less muffled. 22000 = effectively no filtering at full health.")]
    public float lowHealthLowPassCutoff = 3000f;
    [Tooltip("Shapes the ramp as health drops. >1 keeps the effect subtle until health gets low, then it deepens quickly.")]
    public float lowHealthRampExponent = 2f;

    [Header("Pause Effect")]
    [Tooltip("Pitch applied to whichever normal track is playing while the pause menu is open.")]
    [Range(0.1f, 1f)] public float pauseMufflePitch = 0.85f;
    [Tooltip("Low-pass filter cutoff in Hz while paused. Lower = more muffled.")]
    public float pauseMuffleCutoff = 1200f;

    [Header("Settings")]
    [Range(0f, 3f)] public float volume = 0.6f;
    public float crossfadeDuration = 1.5f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    // sourceA and sourceB share this GameObject, so a single AudioLowPassFilter
    // here processes whichever one is actually audible -- only one is ever at
    // non-zero volume outside a brief crossfade overlap, so one shared filter
    // is correct (two filters on the same object would double-apply muffling).
    private AudioLowPassFilter normalFilter;
    private AudioSource gameOverSource;
    private AudioLowPassFilter gameOverFilter;
    private AudioSource activeSource;
    private Coroutine fadeRoutine;
    private AudioClip currentClip;
    private bool isGameOver;
    private bool isPauseMuffled;
    private float lastHealthPercent = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        sourceA.loop = true;
        sourceB.loop = true;
        sourceA.playOnAwake = false;
        sourceB.playOnAwake = false;
        sourceA.volume = 0f;
        sourceB.volume = 0f;
        // Music must never be silenced by Time.timeScale=0 (e.g. the Game Over
        // pause) or by anything calling the global AudioListener.pause.
        sourceA.ignoreListenerPause = true;
        sourceB.ignoreListenerPause = true;
        normalFilter = gameObject.AddComponent<AudioLowPassFilter>();
        normalFilter.cutoffFrequency = 22000f;
        activeSource = sourceA;

        GameObject goObj = new GameObject("GameOverSource");
        goObj.transform.SetParent(transform, false);
        gameOverSource = goObj.AddComponent<AudioSource>();
        gameOverSource.loop = true;
        gameOverSource.playOnAwake = false;
        gameOverSource.volume = 0f;
        gameOverSource.ignoreListenerPause = true;
        gameOverFilter = goObj.AddComponent<AudioLowPassFilter>();
        gameOverFilter.cutoffFrequency = 22000f;
    }

    // Restarting (or returning to the Main Menu and playing again) reloads
    // the scene, but this object survives via DontDestroyOnLoad -- without an
    // explicit reset it would carry over whatever pitch/muffle/fade state the
    // previous run ended on (e.g. still-muffled Game Over audio bleeding into
    // a freshly restarted round). This puts it back to exactly the state it
    // was in right after Awake, so the next Play*Music() call behaves like a
    // true fresh load.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        sourceA.Stop();
        sourceB.Stop();
        gameOverSource.Stop();
        sourceA.volume = 0f;
        sourceB.volume = 0f;
        gameOverSource.volume = 0f;
        sourceA.pitch = 1f;
        sourceB.pitch = 1f;
        normalFilter.cutoffFrequency = 22000f;
        gameOverFilter.cutoffFrequency = 22000f;

        activeSource = sourceA;
        currentClip = null;
        isGameOver = false;
        isPauseMuffled = false;
        lastHealthPercent = 1f;
    }

    // Called by PauseMenuUI whenever the pause menu opens/closes, so the
    // currently-playing track sounds muffled behind the menu -- mirrors the
    // Game Over muffle, but reversible (unpausing restores whatever
    // pitch/cutoff the low-health tension effect currently calls for).
    public void SetPauseMuffled(bool muffled)
    {
        isPauseMuffled = muffled;
        if (isGameOver) return; // already muffled by the Game Over effect

        if (muffled)
        {
            sourceA.pitch = pauseMufflePitch;
            sourceB.pitch = pauseMufflePitch;
            normalFilter.cutoffFrequency = pauseMuffleCutoff;
        }
        else
        {
            SetHealthFactor(lastHealthPercent);
        }
    }

    // Lets a settings slider change volume immediately -- changing the
    // `volume` field alone wouldn't apply until the next crossfade, since
    // FadeRoutine is what actually writes to the AudioSources.
    public void SetVolume(float newVolume)
    {
        volume = newVolume;
        AudioSource current = isGameOver ? gameOverSource : activeSource;
        if (current != null) current.volume = volume;
    }

    public void PlayMainMenuMusic() => PlayNormalTrack(mainMenuMusic);
    public void PlayBuyPhaseMusic() => PlayNormalTrack(buyPhaseMusic);
    public void PlayCombatMusic() => PlayNormalTrack(combatMusic);

    // Call with (currentHealth / maxHealth) every time the base takes damage
    // or heals. Whichever normal track is currently playing gradually slows
    // and muffles as health drops, building tension into the full Game Over
    // muffle rather than the two effects feeling disconnected. Has no effect
    // once isGameOver has taken over (gameOverSource is a separate source).
    public void SetHealthFactor(float healthPercent01)
    {
        lastHealthPercent = Mathf.Clamp01(healthPercent01);
        if (isGameOver) return;

        float lost = 1f - lastHealthPercent;
        float eased = Mathf.Pow(lost, lowHealthRampExponent);

        float pitch = Mathf.Lerp(1f, lowHealthPitch, eased);
        float cutoff = Mathf.Lerp(22000f, lowHealthLowPassCutoff, eased);

        sourceA.pitch = pitch;
        sourceB.pitch = pitch;
        normalFilter.cutoffFrequency = cutoff;
    }

    private void PlayNormalTrack(AudioClip clip)
    {
        if (clip == null) return;
        if (clip == currentClip && !isGameOver) return;

        currentClip = clip;
        isGameOver = false;

        AudioSource incoming = activeSource == sourceA ? sourceB : sourceA;
        incoming.clip = clip;
        incoming.Play();
        activeSource = incoming;

        StartFade(incoming);

        // Re-apply whatever health tension was already in effect so a track
        // switch (e.g. combat -> buy phase) doesn't reset pitch to normal
        // while the base is still at low health.
        SetHealthFactor(lastHealthPercent);
    }

    // On game over, the main theme itself replays muffled and pitched down
    // rather than swapping to a separate clip.
    public void PlayGameOverMusic()
    {
        if (mainMenuMusic == null) return;

        currentClip = mainMenuMusic;
        isGameOver = true;

        gameOverSource.clip = mainMenuMusic;
        gameOverSource.pitch = gameOverPitch;
        gameOverFilter.cutoffFrequency = gameOverLowPassCutoff;
        gameOverSource.Play();

        StartFade(gameOverSource);
    }

    private void StartFade(AudioSource targetSource)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetSource));
    }

    private IEnumerator FadeRoutine(AudioSource targetSource)
    {
        AudioSource[] all = { sourceA, sourceB, gameOverSource };
        float[] startVolumes = new float[all.Length];
        for (int i = 0; i < all.Length; i++) startVolumes[i] = all[i].volume;

        float t = 0f;
        while (t < crossfadeDuration)
        {
            // Unscaled so the fade always completes even while Time.timeScale
            // is 0 (e.g. mid-transition into the paused Game Over screen).
            t += Time.unscaledDeltaTime;
            float p = crossfadeDuration > 0f ? t / crossfadeDuration : 1f;
            for (int i = 0; i < all.Length; i++)
            {
                float targetVol = (all[i] == targetSource) ? volume : 0f;
                all[i].volume = Mathf.Lerp(startVolumes[i], targetVol, p);
            }
            yield return null;
        }

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == targetSource)
            {
                all[i].volume = volume;
            }
            else
            {
                all[i].volume = 0f;
                all[i].Stop();
            }
        }
    }
}
