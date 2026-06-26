using UnityEngine;

// Shared helper for aiming/rotating single-direction top-down sprites (one
// drawn facing a fixed default direction, no 8-way sprite sheet). Rotating
// such a sprite a full 360 degrees makes it flip upside-down past +/-90
// degrees. Instead, this folds any facing angle into the right-facing
// (-90, 90) range and reports that the sprite should be horizontally
// mirrored (localScale.x negated) for the left half -- mirroring, not
// rotating past vertical, is what keeps it readable.
public static class TopDownAim
{
    // axisAngle is the angle (relative to the GameObject's own unrotated,
    // local +x) that the art's "neutral facing" direction actually sits at --
    // 0 if it's drawn facing local +x. When mirroring, the rotation has to be
    // compensated by 2*axisAngle to land back on the correct world angle;
    // get this wrong and anything that isn't due-left/due-right ends up
    // pointing the wrong way once flipped.
    public static float Fold(float angle, float axisAngle, out bool flipped)
    {
        float normalized = Mathf.DeltaAngle(0f, angle);
        flipped = normalized > 90f || normalized < -90f;
        if (!flipped) return normalized;

        return Mathf.DeltaAngle(0f, angle + 2f * axisAngle - 180f);
    }
}
