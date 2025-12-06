using UnityEngine;

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
    #endregion

    #region Fields
    protected bool _isPlayer;
    private bool _initialized;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        InitActorIfNeeded();
    }

    protected virtual void OnEnable()
    {
        ResetHealth();
    }

    protected virtual void OnDestroy() { }

    protected virtual void Update()
    {
        if (IsDead)
        {
            return;
        }

        var deltaTime = Time.deltaTime;
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

    #region Public Methods
    public virtual void TakeDamage(int amount)
    {
        if (IsDead)
        {
            return;
        }

        var dmg = Mathf.Max(0, amount);
        CurrentHp = Mathf.Clamp(CurrentHp - dmg, 0, MaxHp);
        OnAfterTakeDamage(dmg);

        if (CurrentHp <= 0)
        {
            IsDead = true;
            OnBeforeDeath();
            OnAfterDeath();
        }
    }
    #endregion

    #region Protected Methods
    protected virtual void OnInitActor() { }
    protected virtual void OnUpdatePlayerMovement(float deltaTime) { }
    protected virtual void OnUpdateNpcMovement(float deltaTime) { }
    protected virtual void OnAfterTakeDamage(int damage) { }
    protected virtual void OnBeforeDeath() { }
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
    }

    private void ResetHealth()
    {
        MaxHp = Mathf.Max(1, MaxHp);
        CurrentHp = MaxHp;
        IsDead = false;
    }
    #endregion
}
