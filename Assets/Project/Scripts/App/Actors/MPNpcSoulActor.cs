using UnityEngine;
using UnityEngine.AI;

// MPNpcSoulActor is a basic NPC that chases the player and reports death to the room manager.
public class MPNpcSoulActor : MPCharacterSoulActorBase
{
    #region Inspector
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private string _poolKey = "Enemy_Dummy";
    public int AttackDamage = 10;
    public float AttackInterval = 1.0f;
    public float AttackRange = 1.2f;
    #endregion

    #region Fields
    private MPSoulActor _playerTarget;
    private MPRoomManager _roomManager;
    private float _attackTimer;
    private NavMeshAgent _agent;
    private bool _isPaused;
    #endregion

    #region Public Methods
    public void Init(MPRoomManager room, MPSoulActor player)
    {
        _roomManager = room;
        _playerTarget = player;
        ResetState();
    }

    public void SetPoolKey(string poolKey)
    {
        if (!string.IsNullOrEmpty(poolKey))
        {
            _poolKey = poolKey;
        }
    }

    public string GetPoolKey() => _poolKey;
    #endregion

    #region Protected Methods
    protected override void OnInitActor()
    {
        _isPlayer = false;
        MaxHp = MaxHp <= 0 ? 30 : MaxHp;
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
        }
    }

    protected override void OnUpdateNpcMovement(float deltaTime)
    {
        if (_isPaused)
        {
            return;
        }

        if (IsDead)
        {
            return;
        }

        if (_agent == null)
        {
            return;
        }

        if (_playerTarget == null || _playerTarget.IsDead)
        {
            return;
        }

        if (_agent != null && _agent.isStopped)
        {
            _agent.isStopped = false;
        }

        _agent.speed = _moveSpeed;
        _agent.destination = _playerTarget.transform.position;

        var dir = _playerTarget.transform.position - transform.position;
        dir.y = 0f;
        HandleAttack(deltaTime, dir.sqrMagnitude);
    }

    protected override void OnBeforeDeath()
    {
        if (_agent != null)
        {
            _agent.enabled = false;
        }

        // Base class raises EventBus event
        base.OnBeforeDeath();
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
        IsDead = false;
        CurrentHp = MaxHp;
        _attackTimer = 0f;
        _isPaused = false;
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
            _playerTarget?.TakeDamage(AttackDamage);
        }
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;

        if (_agent != null)
        {
            if (paused)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
            else
            {
                _agent.isStopped = false;
            }
        }
    }
    #endregion
}
