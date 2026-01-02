using UnityEngine;

// SkillConfig defines minimal skill metadata and numbers for the demo skill system.
[CreateAssetMenu(fileName = "SkillConfig", menuName = "Configs/Skill Config", order = 40)]
public class SkillConfig : ScriptableObject
{
    #region Types
    public enum SkillType
    {
        Basic = 0,
        Active = 1,
        Passive = 2
    }
    #endregion

    #region Fields
    [Header("Identity")]
    public int SkillId;
    public string DisplayName;
    [TextArea] public string Description;

    [Header("Numbers")]
    public float Cooldown = 1f;
    public float CastTime = 0f;
    public float BaseDamage = 10f;
    public float DamageMultiplier = 1f;

    [Header("Presentation")]
    public string AnimState;
    public GameObject HitEffectPrefab;
    public GameObject ProjectilePrefab;
    public float ProjectileSpeed = 10f;
    public float ProjectileLifeTime = 3f;
    public float ProjectileDamage = 10f;
    public PoolKey PoolKey; // PoolKey for projectiles

    [Header("Type")]
    public SkillType Category = SkillType.Basic;
    #endregion
}
