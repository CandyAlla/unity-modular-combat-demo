using UnityEngine;

// GlobalHelper hosts small shared helpers to avoid duplication across UI/logic layers.
public static class GlobalHelper
{
    #region Public Helpers
    // Formats a time value in seconds to mm:ss.
    public static string FormatTime(float timeSeconds)
    {
        var totalSeconds = Mathf.Max(0, Mathf.FloorToInt(timeSeconds));
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
    #endregion
}
