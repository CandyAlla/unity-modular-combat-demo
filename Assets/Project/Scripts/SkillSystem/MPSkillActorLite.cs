using UnityEngine;
using System.Collections.Generic;

// MPSkillActorLite is a lightweight skill actor that owns primary/active skills
// and drives their runtime controllers. Attach to the player GameObject.
public class MPSkillActorLite : MonoBehaviour
{
    [System.Serializable]
    public class SkillSlot
    {
        public SkillConfig Config;
        [HideInInspector] public SkillRuntimeController Controller;
        [HideInInspector] public SkillRuntimeController.SkillPhase LastPhase = SkillRuntimeController.SkillPhase.Idle;
        [HideInInspector] public Vector3 LastDirection = Vector3.forward;
        [HideInInspector] public Vector3 LastTarget = Vector3.zero;
        [HideInInspector] public string PoolKey;
    }

    #region Inspector
    [Header("Skill Slots")]
    [SerializeField] private SkillSlot _primarySkill;
    [SerializeField] private SkillSlot _activeSkill;
    [SerializeField] private SkillSlot _secondaryActiveSkill;
    [Header("Default Skill Paths (Resources)")]
    [Header("Default Skill Paths (Resources)")]
    [SerializeField] private string _primarySkillResourcePath = GameConsts.PATH_CONFIG_SKILL_PRIMARY;
    [SerializeField] private string _activeSkillResourcePath = GameConsts.PATH_CONFIG_SKILL_ACTIVE;
    [SerializeField] private string _secondarySkillResourcePath = GameConsts.PATH_CONFIG_SKILL_SECONDARY;
    #endregion

    #region Fields
    private GameObject _owner;
    private static readonly HashSet<string> _initializedPoolKeys = new HashSet<string>();
    #endregion

    #region Events
    public System.Action<SkillSlot> OnSkillActivated;
    #endregion

    #region Properties
    public SkillSlot PrimarySlot => _primarySkill;
    public SkillSlot ActiveSlot => _activeSkill;
    public SkillSlot SecondaryActiveSlot => _secondaryActiveSkill;
    #endregion

    #region Public Methods
    public void Initialize(GameObject owner)
    {
        _owner = owner;
        EnsureSlot(ref _primarySkill);
        EnsureSlot(ref _activeSkill);
        EnsureSlot(ref _secondaryActiveSkill);
        AssignDefaultConfigsIfEmpty();
        InitSlot(_primarySkill);
        InitSlot(_activeSkill);
        InitSlot(_secondaryActiveSkill);
    }

    public void Tick(float deltaTime)
    {
        UpdateSlot(_primarySkill, deltaTime);
        UpdateSlot(_activeSkill, deltaTime);
        UpdateSlot(_secondaryActiveSkill, deltaTime);
    }

    public bool TryGetActiveSkillCooldown(out float remaining, out float total, out bool isReady)
    {
        remaining = 0f;
        total = 0f;
        isReady = false;

        if (_activeSkill == null || _activeSkill.Controller == null || _activeSkill.Config == null)
        {
            return false;
        }

        remaining = _activeSkill.Controller.CooldownRemaining;
        total = _activeSkill.Config.Cooldown;
        isReady = _activeSkill.Controller.IsReady;
        return true;
    }

    public bool TryGetSecondarySkillCooldown(out float remaining, out float total, out bool isReady)
    {
        remaining = 0f;
        total = 0f;
        isReady = false;

        if (_secondaryActiveSkill == null || _secondaryActiveSkill.Controller == null || _secondaryActiveSkill.Config == null)
        {
            return false;
        }

        remaining = _secondaryActiveSkill.Controller.CooldownRemaining;
        total = _secondaryActiveSkill.Config.Cooldown;
        isReady = _secondaryActiveSkill.Controller.IsReady;
        return true;
    }

    public bool IsPrimaryReady()
    {
        return _primarySkill != null && _primarySkill.Controller != null && _primarySkill.Controller.IsReady;
    }

    // Input entry for primary attack
    public void OnPrimaryAttackInput(Vector3 targetPosition, Vector3 direction)
    {
        if (TryCastSlot(_primarySkill, targetPosition, direction))
        {
            NotifyStateCasting();
        }
    }

    // Input entry for active skill
    public bool OnActiveSkillInput(Vector3 targetPosition, Vector3 direction)
    {
        return TryCastSlot(_activeSkill, targetPosition, direction);
    }

    public bool OnSecondaryActiveSkillInput(Vector3 targetPosition, Vector3 direction)
    {
        return TryCastSlot(_secondaryActiveSkill, targetPosition, direction);
    }

    public bool IsActiveSkillReady()
    {
        return _activeSkill != null && _activeSkill.Controller != null && _activeSkill.Controller.IsReady;
    }

    public void AssignDefaultConfigsIfEmpty()
    {
        if (_primarySkill != null && _primarySkill.Config == null && !string.IsNullOrEmpty(_primarySkillResourcePath))
        {
            _primarySkill.Config = Resources.Load<SkillConfig>(_primarySkillResourcePath);
        }

        if (_activeSkill != null && _activeSkill.Config == null && !string.IsNullOrEmpty(_activeSkillResourcePath))
        {
            _activeSkill.Config = Resources.Load<SkillConfig>(_activeSkillResourcePath);
        }

        if (_secondaryActiveSkill != null && _secondaryActiveSkill.Config == null && !string.IsNullOrEmpty(_secondarySkillResourcePath))
        {
            _secondaryActiveSkill.Config = Resources.Load<SkillConfig>(_secondarySkillResourcePath);
        }
    }
    #endregion

    #region Private Methods
    private void InitSlot(SkillSlot slot)
    {
        if (slot == null || slot.Config == null)
        {
            return;
        }

        slot.Controller = new SkillRuntimeController();
        slot.Controller.Initialize(slot.Config, _owner);
    }

    public bool TryCastSlot(SkillSlot slot, Vector3 targetPos, Vector3 dir)
    {
        if (slot == null || slot.Controller == null)
        {
            return false;
        }

        if (!slot.Controller.IsReady)
        {
            return false;
        }

        slot.LastDirection = dir;
        slot.LastTarget = targetPos;

        var casted = slot.Controller.TryStartCast(targetPos, dir);
        if (casted)
        {
            NotifyStateCasting();
        }
        return casted;
    }

    // TODO: integrate with a simple FSM; for now it is a stub for callers to hook up.
    private void NotifyStateCasting()
    {
        // This method can signal an FSM or animator about casting state.
    }

    private void UpdateSlot(SkillSlot slot, float deltaTime)
    {
        if (slot == null || slot.Controller == null)
        {
            return;
        }

        var previous = slot.Controller.Phase;
        slot.Controller.Tick(deltaTime);
        var current = slot.Controller.Phase;

        if (previous != current)
        {
            slot.LastPhase = previous;
        }

        // When entering Active, spawn projectile if configured
        if (previous != SkillRuntimeController.SkillPhase.Active &&
            current == SkillRuntimeController.SkillPhase.Active)
        {
            OnSkillActivated?.Invoke(slot);
            SpawnProjectile(slot);
        }
    }

    private void SpawnProjectile(SkillSlot slot)
    {
        if (slot.Config == null || slot.Config.ProjectilePrefab == null)
        {
            return;
        }

        var spawnPos = _owner != null ? _owner.transform.position : slot.LastTarget;
        var direction = slot.LastDirection.sqrMagnitude > 0.001f ? slot.LastDirection.normalized : Vector3.forward;

        var poolKey = string.IsNullOrEmpty(slot.PoolKey) ? $"Proj_{slot.Config.ProjectilePrefab.name}" : slot.PoolKey;
        slot.PoolKey = poolKey;

        if (PoolManager.Inst != null && !_initializedPoolKeys.Contains(poolKey))
        {
            PoolManager.InitPoolItem<BulletActorLite>(poolKey, slot.Config.ProjectilePrefab, 0);
            _initializedPoolKeys.Add(poolKey);
        }

        BulletActorLite bullet = null;
        if (PoolManager.Inst != null)
        {
            bullet = PoolManager.SpawnItemFromPool<BulletActorLite>(poolKey, spawnPos, Quaternion.LookRotation(direction, Vector3.up));
        }
        else
        {
            var projGo = Instantiate(slot.Config.ProjectilePrefab, spawnPos, Quaternion.LookRotation(direction, Vector3.up));
            bullet = projGo.GetComponent<BulletActorLite>();
        }

        if (bullet != null)
        {
            bullet.Init(new BulletSpawnContext
            {
                Owner = _owner,
                Direction = direction,
                Speed = slot.Config.ProjectileSpeed,
                MaxLifeTime = slot.Config.ProjectileLifeTime,
                Damage = slot.Config.ProjectileDamage > 0 ? slot.Config.ProjectileDamage : slot.Config.BaseDamage,
                PoolKey = poolKey
            });
        }
    }

    private void EnsureSlot(ref SkillSlot slot)
    {
        if (slot == null)
        {
            slot = new SkillSlot();
        }
    }
    #endregion
}
