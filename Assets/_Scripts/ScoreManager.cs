using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int score;

    public int Score => score;

    public event Action<int> ScoreChanged;

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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        ScoreChanged?.Invoke(Score);
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
