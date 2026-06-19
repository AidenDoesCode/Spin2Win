using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class ScoreUI : MonoBehaviour
{
    private TMP_Text scoreText;

    [Tooltip("Number of digits to pad the score to (e.g. 5 -> 00010)")]
    public int paddingDigits = 5;

    void Awake()
    {
        scoreText = GetComponent<TMP_Text>();
    }

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ScoreChanged += OnScoreChanged;
            scoreText.text = "Score: " + FormatScore(ScoreManager.Instance.Score);
        }
        else
        {
            scoreText.text = "Score: " + FormatScore(0);
        }
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ScoreChanged -= OnScoreChanged;
    }

    void OnScoreChanged(int newScore)
    {
        scoreText.text = "Score: " + FormatScore(newScore);
    }

    private string FormatScore(int score)
    {
        int pd = Mathf.Max(1, paddingDigits);
        return score.ToString().PadLeft(pd, ' ');
    }
}
