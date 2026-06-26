using UnityEngine;

// Central volume control for every one-shot sound effect in the project
// (separate from MusicManager.volume, which only affects music). Persistent
// across scene loads, like MusicManager/ScoreManager. Drop one of these
// anywhere and drag the slider -- every PlayOneShot/PlayClipAtPoint call
// across the project multiplies against SfxSettings.Volume.
public class SfxSettings : MonoBehaviour
{
    public static SfxSettings Instance { get; private set; }

    [Range(0f, 3f)] public float volume = 1f;

    public static float Volume => Instance != null ? Instance.volume : 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
