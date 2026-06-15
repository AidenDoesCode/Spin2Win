using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class ScoreUI : MonoBehaviour
{
    private TMP_Text scoreText;

    void Awake()
    {
        scoreText = GetComponent<TMP_Text>();
    }

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ScoreChanged += OnScoreChanged;
            // initialize
            scoreText.text = "Score: " + ScoreManager.Instance.Score.ToString();
        }
        else
        {
            scoreText.text = "Score: 0";
        }
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ScoreChanged -= OnScoreChanged;
    }

    void OnScoreChanged(int newScore)
    {
        scoreText.text = "Score: " + newScore.ToString();
    }
}
