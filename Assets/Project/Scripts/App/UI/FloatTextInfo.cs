using System;
using UnityEngine;

[Serializable]
public class FloatTextInfo
{
    public FloatTextType Type;
    public int Value;
    public Vector3 Position;
    public Color Color = Color.white;
    public float Duration = 1.0f;
    public float MoveSpeed = 2.0f;
    
    // Optional curves for more juice, keeping simple for now
    // public AnimationCurve ScaleCurve; 
}

public enum FloatTextType
{
    Damage,
    Critical,
    Miss,
    Heal
}
