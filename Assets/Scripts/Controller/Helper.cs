using UnityEngine;

public static class Helper
{
    /// <summary>
    /// Returns a distinct color for the given index.
    /// </summary>
    /// <param name="index">Color index (0-based).</param>
    public static Color GetColorOLD(int index)
    {
        // spread hues evenly around the color wheel
        const float saturation = 0.8f;
        const float value = 0.8f;
        int maxSlots = 8;  // maximum expected distinct colors
        float hue = (index % maxSlots) / (float)maxSlots;
        return Color.HSVToRGB(hue, saturation, value);
    }


    public static Color GetColor(int colorIndex)
    {
        LevelData level = GameManager.Instance.CurrentLevelData;

        if (level == null || colorIndex < 0 || colorIndex >= level.colorNames.Count)
            return Color.gray;

        string colorName = level.colorNames[colorIndex];
        return GetEditorColor(colorName);
    }

    /// <summary>
    /// The color used to indicate a hidden box.
    /// </summary>
    public static Color GetHiddenColor()
    {
        return Color.gray;
    }

    public static Color GetEditorColor(string colorName)
    {
        return colorName switch
        {
            "Red" => Color.red,
            "Green" => Color.green,
            "Blue" => Color.blue,
            "Orange" => new Color(1f, 0.5f, 0f),
            "Yellow" => Color.yellow,
            "Pink" => new Color(1f, 0.4f, 0.7f),
            "Purple" => new Color(0.6f, 0.2f, 0.7f),
            "White" => Color.white,
            "LightBlue" => new Color(0.5f, 0.8f, 1f),
            "Turquoise" => new Color(0.3f, 1f, 0.9f),
            _ => Color.gray,
        };
    }
}
