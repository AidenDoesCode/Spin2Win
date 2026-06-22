using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SpinFort/Wheel Definition", fileName = "SpinFortWheelDefinition")]
public class SpinFortWheelDefinitionSO : ScriptableObject
{
    [Serializable]
    public class WheelSegment
    {
        public string label = "Prize";
        [Min(0f)] public float weight = 1f;
        public SpinFortRewardType rewardType = SpinFortRewardType.Points;
        public int intValue = 100;
        public float floatValue = 1.25f;
        [Min(0f)] public float duration = 1f;
        public TowerSO towerReward; // only used when rewardType == Tower
    }

    public List<WheelSegment> segments = new List<WheelSegment>();

    public WheelSegment Roll()
    {
        if (segments == null || segments.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var s in segments)
            if (s != null && s.weight > 0f) totalWeight += s.weight;

        if (totalWeight <= 0f)
            return segments[UnityEngine.Random.Range(0, segments.Count)];

        float roll = UnityEngine.Random.value * totalWeight;
        float cursor = 0f;

        foreach (var s in segments)
        {
            if (s == null || s.weight <= 0f) continue;
            cursor += s.weight;
            if (roll <= cursor) return s;
        }

        return segments[segments.Count - 1];
    }
}