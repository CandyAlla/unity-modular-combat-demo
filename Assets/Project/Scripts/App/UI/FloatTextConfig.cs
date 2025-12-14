using System;
using System.Collections.Generic;
using UnityEngine;

// FloatTextConfig defines default visual settings per float text type.
[CreateAssetMenu(fileName = "FloatTextConfig", menuName = "Configs/Float Text Config", order = 10)]
public class FloatTextConfig : ScriptableObject
{
    [Serializable]
    public class FloatTextEntry
    {
        public FloatTextType Type;
        public Color Color = Color.white;
        public float Duration = 1.0f;
        public float MoveSpeed = 2.0f;
    }

    public List<FloatTextEntry> Entries = new List<FloatTextEntry>();
}

// FloatTextConfigProvider loads and caches the FloatTextConfig from Resources/Configs.
public static class FloatTextConfigProvider
{
    private static FloatTextConfig _cached;

    public static FloatTextConfig.FloatTextEntry GetDefaults(FloatTextType type)
    {
        EnsureLoaded();
        if (_cached != null && _cached.Entries != null)
        {
            foreach (var entry in _cached.Entries)
            {
                if (entry != null && entry.Type == type)
                {
                    return entry;
                }
            }
        }

        // Fallback defaults if not configured.
        return new FloatTextConfig.FloatTextEntry
        {
            Type = type,
            Color = ResolveFallbackColor(type),
            Duration = 1.0f,
            MoveSpeed = 2.0f
        };
    }

    private static void EnsureLoaded()
    {
        if (_cached != null)
        {
            return;
        }

        _cached = Resources.Load<FloatTextConfig>("Configs/FloatTextConfig");
    }

    private static Color ResolveFallbackColor(FloatTextType type)
    {
        switch (type)
        {
            case FloatTextType.Critical: return Color.yellow;
            case FloatTextType.Heal: return Color.green;
            case FloatTextType.Miss: return Color.gray;
            case FloatTextType.Buff: return Color.cyan;
            default: return Color.white;
        }
    }
}
