using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class BaseHealthUI : MonoBehaviour
{
    private TMP_Text text;

    public BaseHealth baseHealth;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (baseHealth == null)
            baseHealth = BaseHealth.Instance;

        if (baseHealth != null)
        {
            baseHealth.HealthChanged += OnHealthChanged;
            OnHealthChanged(baseHealth.CurrentHealth, baseHealth.maxHealth);
        }
        else
        {
            text.text = "Base: -";
        }
    }

    private void OnDestroy()
    {
        if (baseHealth != null)
            baseHealth.HealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int current, int max)
    {
        text.text = $"Base {current}/{max}";
    }
}