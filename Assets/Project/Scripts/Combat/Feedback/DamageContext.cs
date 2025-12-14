using UnityEngine;

// DamageContext carries damage event data for feedback/logic layers.
public struct DamageContext
{
    public int DamageAmount;
    public bool IsCrit;
    public MPCharacterSoulActorBase Attacker;
    public MPCharacterSoulActorBase Victim;
    public Vector3 HitPoint;
}
