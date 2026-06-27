using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int score = 200;

    public int Score => score;

    public event Action<int> ScoreChanged;

    private const string HighScoreKey = "Spin2Win_HighScore";
    public static int HighScore { get; private set; }
    public static event Action<int> HighScoreChanged;

    // Captured once on first Awake. Since this object survives scene loads
    // (DontDestroyOnLoad below), Awake never runs again -- without this, gold
    // earned across a run would carry into the next "Restart"/Play Again,
    // making it feel like the round was resumed instead of started fresh.
    private int startingScore;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            startingScore = score;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetScore(startingScore);
    }

    public void AddScore(int amount)
    {
        score += amount;
        ScoreChanged?.Invoke(Score);
        CheckHighScore();
    }

    private void CheckHighScore()
    {
        if (score <= HighScore) return;

        HighScore = score;
        PlayerPrefs.SetInt(HighScoreKey, HighScore);
        PlayerPrefs.Save();
        HighScoreChanged?.Invoke(HighScore);
    }

    public bool TrySpendScore(int amount)
    {
        if (amount <= 0)
            return true;

        if (score < amount)
            return false;

        score -= amount;
        ScoreChanged?.Invoke(Score);
        return true;
    }

    public void ResetScore()
    {
        score = 0;
        ScoreChanged?.Invoke(Score);
    }

    public void SetScore(int amount)
    {
        score = Mathf.Max(0, amount);
        ScoreChanged?.Invoke(Score);
        CheckHighScore();
    }

    public void AddToScore(int amount)
    {
        if (amount == 0)
            return;

        SetScore(score + amount);
    }

    private void OnValidate()
    {
        score = Mathf.Max(0, score);

        if (Application.isPlaying)
            ScoreChanged?.Invoke(score);
    }
}
