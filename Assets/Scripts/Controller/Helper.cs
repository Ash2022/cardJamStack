using UnityEngine;

public static class Helper
{
    /// <summary>
    /// Returns a distinct color for the given index.
    /// </summary>
    /// <param name="index">Color index (0-based).</param>
    public static Color GetColor(int index)
    {
        // spread hues evenly around the color wheel
        const float saturation = 0.8f;
        const float value = 0.8f;
        int maxSlots = 8;  // maximum expected distinct colors
        float hue = (index % maxSlots) / (float)maxSlots;
        return Color.HSVToRGB(hue, saturation, value);
    }

    /// <summary>
    /// The color used to indicate a hidden box.
    /// </summary>
    public static Color GetHiddenColor()
    {
        return Color.gray;
    }
}
