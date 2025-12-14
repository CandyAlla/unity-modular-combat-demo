using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// MPCharacterSoulActorBase provides shared HP and update hooks for player/NPC actors.
// Movement and death handling are delegated to overrides; base enforces simple damage flow.
public class MPCharacterSoulActorBase : MonoBehaviour
{
    #region Inspector
    [SerializeField] protected int MaxHp = 100;
    [SerializeField] protected int CurrentHp = 100;
    #endregion

    #region Properties
    public bool IsDead { get; protected set; }
    public BuffLayerMgr BuffLayerMgr => _buffLayerMgr;
    public MPAttributeComponent AttributeComponent => _attributeComponent;
    public event System.Action<DamageContext> OnDamaged;
    #endregion

    #region Fields
    protected bool _isPlayer;
    private bool _initialized;
    protected BuffLayerMgr _buffLayerMgr;
    protected MPAttributeComponent _attributeComponent;
    [Header("Hurt Feedback")]
    [SerializeField] private Renderer _hurtRenderer;
    [SerializeField] private Color _hurtFlashColor = Color.white;
    [SerializeField] private float _hurtFlashDuration = 0.2f;
    [SerializeField] private float _hurtStunDuration = 0.3f;
    private Color _hurtOriginalColor = Color.white;
    private Coroutine _hurtFlashCo;
    private NavMeshAgent _hurtAgent;
    private bool _hurtPaused;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        InitActorIfNeeded();

        if (_hurtRenderer == null)
        {
            _hurtRenderer = GetComponentInChildren<Renderer>();
        }

        _hurtAgent = GetComponent<NavMeshAgent>();

        if (_hurtRenderer != null && _hurtRenderer.sharedMaterial != null)
        {
            var mat = _hurtRenderer.sharedMaterial;
            if (mat.HasProperty("_BaseColor"))
                _hurtOriginalColor = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
                _hurtOriginalColor = mat.GetColor("_Color");
        }
    }

    protected virtual void OnEnable()
    {
        ResetHealth();
    }

    protected virtual void OnDisable()
    {
        ResetHurtFeedback();
    }

    protected virtual void OnDestroy() { }

    // Per-frame logic is driven externally via TickActor to centralize updates in the room manager.
    #endregion

    #region Public Methods
    public virtual void TakeDamage(int amount)
    {
        if (IsDead)
        {
            return;
        }

        var dmg = Mathf.Max(0, amount);
        CurrentHp = Mathf.Clamp(CurrentHp - dmg, 0, MaxHp);
        Debug.Log($"[{name}] Took {dmg} damage. Current HP: {CurrentHp}");
        ShowFloatTextPublic(dmg, FloatTextType.Damage);
        OnAfterTakeDamage(dmg);

        var dmgCtx = new DamageContext
        {
            DamageAmount = dmg,
            IsCrit = false,
            Attacker = null,
            Victim = this,
            HitPoint = transform.position
        };

        OnDamaged?.Invoke(dmgCtx);
        PlayHurtFeedback();

        if (CurrentHp <= 0)
        {
            IsDead = true;
            OnBeforeDeath();
            OnAfterDeath();
        }
    }

    public void TickActor(float deltaTime)
    {
        if (IsDead)
        {
            return;
        }

        if (_isPlayer)
        {
            OnUpdatePlayerMovement(deltaTime);
        }
        else
        {
            OnUpdateNpcMovement(deltaTime);
        }
    }
    #endregion

    #region Protected Methods
    protected virtual void OnInitActor() { }
    protected virtual void OnUpdatePlayerMovement(float deltaTime) { }
    protected virtual void OnUpdateNpcMovement(float deltaTime) { }
    protected virtual void OnAfterTakeDamage(int damage) { }
    protected virtual void OnBeforeDeath()
    {
        if (_isPlayer)
        {
            EventBus.OnValueChange(new PlayerDeadEvent { Actor = this as MPSoulActor });
        }
        else
        {
            EventBus.OnValueChange(new EnemyDeadEvent { Actor = this as MPNpcSoulActor });
        }

        _buffLayerMgr?.ClearAll();
    }
    protected virtual void OnAfterDeath() { }
    #endregion

    #region Private Methods
    private void InitActorIfNeeded()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ResetHealth();
        OnInitActor();

        if (_attributeComponent == null)
        {
            _attributeComponent = new MPAttributeComponent();
            // Default initialization - can be overridden or loaded from config later
            _attributeComponent.Initialize(3.5f, 10, MaxHp);
        }

        if (_buffLayerMgr == null)
        {
            var lookup = DataCtrl.Instance.GetBuffConfigLookup();
            _buffLayerMgr = new BuffLayerMgr(lookup, _attributeComponent);
            _buffLayerMgr.OnBuffAdded += (cfg) =>
            {
                ShowFloatTextPublic(0, FloatTextType.Buff, cfg.BuffName);
            };
        }
    }

    protected void ResetHealth()
    {
        MaxHp = Mathf.Max(1, MaxHp);
        CurrentHp = MaxHp;
        IsDead = false;
    }

    public virtual void ResetActorState()
    {
        ResetHealth();
        _buffLayerMgr?.ClearAll();
        ResetHurtFeedback();
    }

    public void TryAddBuffStack(BuffType type)
    {
        _buffLayerMgr?.TryAddStack(type);
    }

    public void ClearAllBuffs()
    {
        _buffLayerMgr?.ClearAll();
    }

    public void TickBuffs(float deltaTime)
    {
        _buffLayerMgr?.Tick(deltaTime);
    }
    public void ShowFloatTextPublic(int value, FloatTextType type, string customText = null)
    {
        if (PoolManager.Inst == null) return;

        // Simple random offset for variety
        var offset = new Vector3(Random.Range(-0.5f, 0.5f), 2.0f, Random.Range(-0.5f, 0.5f));
        var pos = transform.position + offset;

        var textObj = PoolManager.SpawnItemFromPool<SoulFloatingText>("UI_FloatText", pos, Quaternion.identity);
        if (textObj != null)
        {
            var defaults = FloatTextConfigProvider.GetDefaults(type);
            var info = new FloatTextInfo
            {
                Type = type,
                Value = value,
                CustomText = customText,
                Position = pos,
                Duration = defaults.Duration,
                MoveSpeed = defaults.MoveSpeed,
                Color = defaults.Color
            };
            textObj.Init(info);
        }
    }

    public void SetHurtPaused(bool paused)
    {
        _hurtPaused = paused;
        if (paused)
        {
            ResetHurtFeedback();
        }
    }

    protected void ResetHurtFeedback()
    {
        if (_hurtFlashCo != null)
        {
            StopCoroutine(_hurtFlashCo);
            _hurtFlashCo = null;
        }

        if (_hurtRenderer != null && _hurtRenderer.material != null)
        {
            var mat = _hurtRenderer.material;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", _hurtOriginalColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", _hurtOriginalColor);
        }
    }

    private void PlayHurtFeedback()
    {
        if (_hurtPaused || (MPRoomManager.Inst != null && MPRoomManager.Inst.IsPaused))
        {
            return;
        }

        PlayHurtFlash();
        PlayHurtStun();
    }

    private void PlayHurtFlash()
    {
        if (_hurtRenderer == null)
        {
            return;
        }

        if (_hurtFlashCo != null)
        {
            StopCoroutine(_hurtFlashCo);
        }

        _hurtFlashCo = StartCoroutine(HurtFlashRoutine());
    }

    private IEnumerator HurtFlashRoutine()
    {
        var mat = _hurtRenderer.material;
        if (mat != null)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", _hurtFlashColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", _hurtFlashColor);
        }

        yield return new WaitForSeconds(_hurtFlashDuration);

        if (mat != null)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", _hurtOriginalColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", _hurtOriginalColor);
        }

        _hurtFlashCo = null;
    }

    private void PlayHurtStun()
    {
        if (_isPlayer && this is MPSoulActor player)
        {
            player.SetCanControl(false, _hurtStunDuration);
            return;
        }

        if (!_isPlayer && _hurtAgent != null)
        {
            StartCoroutine(HurtStunRoutine());
        }
    }

    private IEnumerator HurtStunRoutine()
    {
        var prevStopped = _hurtAgent.isStopped;
        _hurtAgent.isStopped = true;
        yield return new WaitForSeconds(_hurtStunDuration);
        if (!_hurtPaused)
        {
            _hurtAgent.isStopped = prevStopped;
        }
    }
    #endregion
}
