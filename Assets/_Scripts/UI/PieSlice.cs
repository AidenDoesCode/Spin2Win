using UnityEngine;
using UnityEngine.UI;

// Draws a colored pie-slice wedge as a procedural mesh, centered in its RectTransform.
// Angle 0 = up (12 o'clock), increasing clockwise.
public class PieSlice : Graphic
{
    [Range(0f, 360f)] public float startAngle = 0f;
    [Range(0f, 360f)] public float sweepAngle = 90f;
    public int segments = 24;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = GetPixelAdjustedRect();
        float radius = Mathf.Min(r.width, r.height) * 0.5f;
        Vector2 center = r.center;

        UIVertex centerVert = UIVertex.simpleVert;
        centerVert.color = color;
        centerVert.position = center;
        vh.AddVert(centerVert);

        int steps = Mathf.Max(1, segments);
        float angleStep = sweepAngle / steps;

        for (int i = 0; i <= steps; i++)
        {
            float angle = Mathf.Deg2Rad * (startAngle + angleStep * i);
            Vector2 pos = center + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;

            UIVertex v = UIVertex.simpleVert;
            v.color = color;
            v.position = pos;
            vh.AddVert(v);

            if (i > 0)
                vh.AddTriangle(0, i, i + 1);
        }
    }
}
