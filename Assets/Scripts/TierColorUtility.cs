using UnityEngine;

public static class TierColorUtility
{
    public static readonly Color Common = new Color32(170, 170, 170, 255); // #AAAAAA
    public static readonly Color Rare = new Color32(80, 160, 255, 255); // #50A0FF
    public static readonly Color Epic = new Color32(180, 90, 255, 255); // #B45AFF
    public static readonly Color Legendary = new Color32(255, 160, 60, 255); // #FFA03C

    public static Color GetColor(string tierName)
    {
        if (string.IsNullOrEmpty(tierName)) return Common;
        switch (tierName.Trim().ToLowerInvariant())
        {
            case "rare": return Rare;
            case "epic": return Epic;
            case "legendary": return Legendary;
            default: return Common;
        }
    }

    public static string ToHex(Color c) => ColorUtility.ToHtmlStringRGB(c);
}