using UnityEngine;
using UnityEngine.AI;


// MPNpcSoulActor is a basic NPC that chases the player and reports death to the room manager.
public class MPNpcSoulActor : MPCharacterSoulActorBase
{
    #region Inspector
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private PoolKey _poolKey = PoolKey.Enemy_Dummy;
    public int AttackDamage = 10;
    public float AttackInterval = 1.0f;
    public float AttackRange = 1.2f;
    public float SearchRange = 10.0f; // New field for Idle -> Chasing detection
    #endregion

    #region Fields
    private string _uniqueId;
    private MPSoulActor _playerTarget;
    private MPRoomManager _roomManager;
    private float _attackTimer;
    private bool _isPaused;
    private NpcStateManager _stateMgr;
    private bool _movementEnabled = true;
    #endregion

    #region Public Methods
    public void Init(MPRoomManager room, MPSoulActor player)
    {
        _roomManager = room;
        _playerTarget = player;
        ResetState();
        if (_stateMgr == null)
        {
            _stateMgr = new NpcStateManager();
        }
        _stateMgr.ChangeState(NpcStateManager.NpcState.Birth);
        _stateMgr.ChangeState(NpcStateManager.NpcState.Idle);
    }

    public void ApplyAttributes(NpcAttributesConfig.NpcAttributesEntry attrs)
    {
        if (attrs == null)
        {
            return;
        }

        MaxHp = attrs.MaxHp;
        CurrentHp = MaxHp;
        _moveSpeed = attrs.MoveSpeed;
        AttackDamage = attrs.AttackDamage;
        AttackInterval = attrs.AttackInterval;
        AttackRange = attrs.AttackRange;
        SearchRange = attrs.SearchRange;

        if (_attributeComponent != null)
        {
            _attributeComponent.SetBaseValue(AttributeType.MoveSpeed, _moveSpeed);
            _attributeComponent.SetBaseValue(AttributeType.AttackPower, AttackDamage);
            _attributeComponent.SetBaseValue(AttributeType.MaxHp, MaxHp);
        }

// Speed update is handled by MPMotionComponent during SetDestination or via attribute changes
    }

    public void SetPoolKey(PoolKey poolKey)
    {
        if (poolKey != PoolKey.None)
        {
            _poolKey = poolKey;
        }
    }

    public PoolKey GetPoolKey() => _poolKey;

    public void SetMovementEnabled(bool enabled)
    {
        _movementEnabled = enabled;
        if (_motionComponent != null)
        {
            if (!enabled)
            {
                _motionComponent.Stop();
            }
            else
            {
                _motionComponent.Resume();
            }
        }
        
    }
    #endregion

    #region Protected Methods
    protected override void OnInitActor()
    {
        _isPlayer = false;
        MaxHp = MaxHp <= 0 ? 30 : MaxHp;
        if (_motionComponent != null)
        {
            _motionComponent.SetNavMeshEnabled(true);
        }

        _stateMgr = new NpcStateManager();
        _stateMgr.ChangeState(NpcStateManager.NpcState.Birth);
        _stateMgr.ChangeState(NpcStateManager.NpcState.Idle);
    }

    public void SetUniqueId(string id)
    {
        _uniqueId = id;
    }

    public string GetUniqueId() => _uniqueId;

    protected override void OnUpdateNpcMovement(float deltaTime)
    {
        if (_isPaused || IsDead || !_movementEnabled) return;
        // Relaxed check: Allow logic to run even if briefly off-navmesh (e.g. pushed by physics)
        // Strict checks are only needed when calling SetDestination
        if (_motionComponent == null) return;
        if (_stateMgr == null) return;
        
        // Priority: If stunned by hit, stop logic
        if (IsHurtStunned) return;

        // General target validation
        if (_playerTarget == null || _playerTarget.IsDead)
        {
             // If target is lost, maybe go back to Idle? For now just return.
             return;
        }

        float sqrDist = (_playerTarget.transform.position - transform.position).sqrMagnitude;
        float attackRangeSqr = AttackRange * AttackRange;
        float searchRangeSqr = SearchRange * SearchRange;

        switch (_stateMgr.CurrentState)
        {
            case NpcStateManager.NpcState.Idle:
                // Logic: Found player? -> Chase
                if (sqrDist <= searchRangeSqr)
                {
                    _stateMgr.ChangeState(NpcStateManager.NpcState.Chasing);
                }
                break;

            case NpcStateManager.NpcState.Chasing:
                // Logic: In attack range? -> Attack
                // Logic: Move to player
                if (sqrDist <= attackRangeSqr)
                {
                    _stateMgr.ChangeState(NpcStateManager.NpcState.Attack);
                    // Stop moving immediately
                    _motionComponent.Stop();
                }
                else
                {
                     // Update speed and destination
                    UpdateMovementLogic(sqrDist);
                }
                break;

            case NpcStateManager.NpcState.Attack:
                // Logic: Player ran away? -> Chase
                // Logic: Execute attack
                if (sqrDist > attackRangeSqr)
                {
                    _stateMgr.ChangeState(NpcStateManager.NpcState.Chasing);
                    // Resume moving
                    _motionComponent.Resume();
                }
                else
                {
                    // Face target
                    var dir = _playerTarget.transform.position - transform.position;
                    dir.y = 0;
                    if (dir.magnitude > 0.1f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), deltaTime * 10f);
                    }
                    
                    HandleAttack(deltaTime, sqrDist);
                }
                break;
        }
    }

    private void UpdateMovementLogic(float sqrDist)
    {
        if (_motionComponent == null) return;
        _motionComponent.SetDestination(_playerTarget.transform.position);
    }

    protected override void OnBeforeDeath()
    {
        if (_motionComponent != null)
        {
            _motionComponent.SetNavMeshEnabled(false);
        }

        // Base class raises EventBus event
        base.OnBeforeDeath();

        if (_stateMgr != null)
        {
            _stateMgr.ChangeState(NpcStateManager.NpcState.Dead);
        }
    }

    protected override void OnAfterDeath()
    {
        if (PoolManager.Inst != null)
        {
            PoolManager.DespawnItemToPool(_poolKey, this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Private Methods
    private void ResetState()
    {
        base.ResetActorState(); // Ensure base flags like IsHurtStunned are reset
        
        IsDead = false;
        CurrentHp = MaxHp;
        _attackTimer = 0f;
        _isPaused = false;
        _movementEnabled = true;

        if (_motionComponent != null)
        {
            _motionComponent.SetNavMeshEnabled(true);
            _motionComponent.Resume();
        }
    }

    private void HandleAttack(float deltaTime, float sqrDistanceToPlayer)
    {
        if (_isPaused)
        {
            return;
        }

        _attackTimer += deltaTime;

        if (_attackTimer < AttackInterval)
        {
            return;
        }

        if (_playerTarget == null || _playerTarget.IsDead)
        {
            return;
        }

        if (sqrDistanceToPlayer <= AttackRange * AttackRange)
        {
            _attackTimer = 0f;
            _roomManager?.RegisterPlayerDamageTaken(AttackDamage);
            _playerTarget?.TakeDamage(AttackDamage);
        }
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;

        if (_motionComponent != null && _motionComponent.IsOnNavMesh)
        {
            if (paused)
            {
                _motionComponent.Stop();
            }
            else
            {
                _motionComponent.Resume();
            }
        }
    }
    #endregion
}
