using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Standalone roulette wheel that spins to reveal which gate enemies will pour
// out of this wave. Pops up whenever GateRouletteManager rolls a new gate.
public class GateRouletteUI : MonoBehaviour
{
    [Header("Layout")]
    public float wheelSize = 260f;

    [Header("Spin")]
    public float spinDuration = 2.5f;
    public float extraFullSpins = 5f;
    public bool invertSpinDirection = false;
    public float resultHoldTime = 1.5f;

    [Header("Colors")]
    public Color[] wedgeColors = new Color[]
    {
        new Color(0.878f, 0.302f, 0.302f, 1f),
        new Color(0.302f, 0.6f, 0.878f, 1f),
        new Color(0.4f, 0.8f, 0.4f, 1f),
        new Color(0.878f, 0.753f, 0.376f, 1f),
    };
    public Color pointerColor = Color.white;
    public Color panelColor = new Color(0.102f, 0.102f, 0.180f, 0.92f);

    public event Action SpinCompleted;

    private RectTransform wheelRT;
    private TextMeshProUGUI resultLabel;
    private GateRouletteManager manager;
    private readonly List<float> wedgeCenterAngles = new List<float>();
    private Coroutine spinRoutine;

    private void Awake()
    {
        BuildPanel();
    }

    private void Start()
    {
        gameObject.SetActive(false);

        manager = GateRouletteManager.Instance;
        if (manager == null) manager = FindAnyObjectByType<GateRouletteManager>();
        if (manager == null) return;

        BuildWedges(manager.gates.Count);
        manager.GateSelected += OnGateSelected;

        if (manager.ActiveGateIndex >= 0)
            OnGateSelected(manager.ActiveGateIndex);
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.GateSelected -= OnGateSelected;
    }

    private void OnGateSelected(int index)
    {
        gameObject.SetActive(true);
        if (spinRoutine != null) StopCoroutine(spinRoutine);
        spinRoutine = StartCoroutine(SpinToGate(index));
    }

    private IEnumerator SpinToGate(int index)
    {
        if (index < 0 || index >= wedgeCenterAngles.Count) yield break;

        resultLabel.text = "";

        float startZ = wheelRT.localEulerAngles.z;
        float centerAngle = wedgeCenterAngles[index];
        float targetZ = centerAngle + extraFullSpins * 360f;
        if (invertSpinDirection) targetZ = -targetZ;

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            float t = elapsed / spinDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float z = Mathf.LerpUnclamped(startZ, targetZ, eased);
            wheelRT.localRotation = Quaternion.Euler(0f, 0f, z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        wheelRT.localRotation = Quaternion.Euler(0f, 0f, targetZ);

        string label = (manager != null && index < manager.gates.Count) ? manager.gates[index].label : $"Gate {index + 1}";
        resultLabel.text = $"{label} incoming!";

        SpinCompleted?.Invoke();

        yield return new WaitForSeconds(resultHoldTime);
        gameObject.SetActive(false);
    }

    private void BuildWedges(int count)
    {
        wedgeCenterAngles.Clear();
        if (count <= 0) return;

        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float startAngle = i * step;
            float centerAngle = startAngle + step * 0.5f;
            wedgeCenterAngles.Add(centerAngle);

            GameObject wedgeObj = new GameObject($"Wedge_{i}");
            wedgeObj.transform.SetParent(wheelRT, false);
            RectTransform wedgeRT = wedgeObj.AddComponent<RectTransform>();
            wedgeRT.anchorMin = Vector2.zero;
            wedgeRT.anchorMax = Vector2.one;
            wedgeRT.offsetMin = wedgeRT.offsetMax = Vector2.zero;
            wedgeObj.AddComponent<CanvasRenderer>();
            PieSlice slice = wedgeObj.AddComponent<PieSlice>();
            slice.startAngle = startAngle;
            slice.sweepAngle = step;
            slice.color = wedgeColors[i % wedgeColors.Length];

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(wheelRT, false);
            RectTransform labelRT = labelObj.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.5f, 0.5f);
            labelRT.anchorMax = new Vector2(0.5f, 0.5f);
            labelRT.pivot = new Vector2(0.5f, 0.5f);
            float labelRadius = wheelSize * 0.32f;
            float rad = Mathf.Deg2Rad * centerAngle;
            labelRT.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * labelRadius;
            labelRT.sizeDelta = new Vector2(90f, 30f);
            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = (manager != null && i < manager.gates.Count) ? manager.gates[i].label : $"Gate {i + 1}";
            label.fontSize = 16;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
        }
    }

    private void BuildPanel()
    {
        RectTransform rootRT = GetComponent<RectTransform>();
        if (rootRT == null) rootRT = gameObject.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 1f);
        rootRT.anchorMax = new Vector2(0.5f, 1f);
        rootRT.pivot = new Vector2(0.5f, 1f);
        rootRT.anchoredPosition = new Vector2(0f, -40f);
        rootRT.sizeDelta = new Vector2(wheelSize + 80f, wheelSize + 140f);

        Image panel = GetComponent<Image>();
        if (panel == null) panel = gameObject.AddComponent<Image>();
        panel.color = panelColor;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(transform, false);
        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -10f);
        titleRT.sizeDelta = new Vector2(0f, 30f);
        var title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "GATE ROULETTE";
        title.fontSize = 20;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;

        GameObject pointerObj = new GameObject("Pointer");
        pointerObj.transform.SetParent(transform, false);
        RectTransform pointerRT = pointerObj.AddComponent<RectTransform>();
        pointerRT.anchorMin = new Vector2(0.5f, 1f);
        pointerRT.anchorMax = new Vector2(0.5f, 1f);
        pointerRT.pivot = new Vector2(0.5f, 0f);
        pointerRT.anchoredPosition = new Vector2(0f, -44f);
        pointerRT.sizeDelta = new Vector2(30f, 30f);
        var pointer = pointerObj.AddComponent<TextMeshProUGUI>();
        pointer.text = "▼";
        pointer.fontSize = 26;
        pointer.alignment = TextAlignmentOptions.Center;
        pointer.color = pointerColor;

        GameObject wheelObj = new GameObject("Wheel");
        wheelObj.transform.SetParent(transform, false);
        wheelRT = wheelObj.AddComponent<RectTransform>();
        wheelRT.anchorMin = new Vector2(0.5f, 1f);
        wheelRT.anchorMax = new Vector2(0.5f, 1f);
        wheelRT.pivot = new Vector2(0.5f, 0.5f);
        wheelRT.anchoredPosition = new Vector2(0f, -(44f + wheelSize * 0.5f));
        wheelRT.sizeDelta = new Vector2(wheelSize, wheelSize);

        GameObject resultObj = new GameObject("Result");
        resultObj.transform.SetParent(transform, false);
        RectTransform resultRT = resultObj.AddComponent<RectTransform>();
        resultRT.anchorMin = new Vector2(0f, 0f);
        resultRT.anchorMax = new Vector2(1f, 0f);
        resultRT.pivot = new Vector2(0.5f, 0f);
        resultRT.anchoredPosition = new Vector2(0f, 10f);
        resultRT.sizeDelta = new Vector2(0f, 30f);
        resultLabel = resultObj.AddComponent<TextMeshProUGUI>();
        resultLabel.fontSize = 18;
        resultLabel.alignment = TextAlignmentOptions.Center;
        resultLabel.color = Color.white;
    }
}
