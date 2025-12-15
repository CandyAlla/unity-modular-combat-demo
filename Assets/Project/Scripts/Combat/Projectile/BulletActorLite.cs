using System.Collections.Generic;
using UnityEngine;

// BulletActorLite handles simple projectile movement, lifetime, collision, and pooling.
public class BulletActorLite : MonoBehaviour
{
    #region Fields
    [SerializeField] private float _radius = 0.25f;
    [SerializeField] private LayerMask _hitMask = ~0;

    private GameObject _owner;
    private bool _targetPlayers;
    private bool _targetNpcs;
    private Vector3 _direction = Vector3.forward;
    private float _speed = 10f;
    private float _lifeTime;
    private float _maxLifeTime = 3f;
    private float _damage = 10f;
    private string _poolKey;
    private bool _paused;
    private bool _recycling;
    private Vector3 _previousPosition;

    private static readonly List<BulletActorLite> _activeBullets = new List<BulletActorLite>();
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        if (!_activeBullets.Contains(this))
        {
            _activeBullets.Add(this);
        }
        _recycling = false;
        _lifeTime = 0f;
    }

    private void OnDisable()
    {
        _activeBullets.Remove(this);
    }

    private void Update()
    {
        if (_paused || (MPRoomManager.Inst != null && MPRoomManager.Inst.IsPaused))
        {
            return;
        }

        var delta = Time.deltaTime;
        var displacement = _direction * (_speed * delta);
        var start = transform.position;
        var end = start + displacement;

        TryHitRay(start, displacement);

        transform.position = end;
        _lifeTime += delta;

        TryHitOverlap();

        if (_lifeTime >= _maxLifeTime)
        {
            Recycle();
            return;
        }
    }
    #endregion

    #region Public Methods
    public void Init(BulletSpawnContext ctx)
    {
        _owner = ctx.Owner;
        _direction = ctx.Direction.sqrMagnitude > 0.001f ? ctx.Direction.normalized : Vector3.forward;
        _speed = ctx.Speed;
        _maxLifeTime = ctx.MaxLifeTime;
        _damage = ctx.Damage;
        _poolKey = ctx.PoolKey;
        _lifeTime = 0f;
        _paused = false;
        _recycling = false;
        _targetPlayers = false;
        _targetNpcs = false;
        _previousPosition = transform.position;

        if (_owner != null)
        {
            if (_owner.GetComponent<MPSoulActor>() != null)
            {
                _targetNpcs = true;
            }
            else if (_owner.GetComponent<MPNpcSoulActor>() != null)
            {
                _targetPlayers = true;
            }
            else
            {
                _targetPlayers = true;
                _targetNpcs = true;
            }
        }
        else
        {
            _targetPlayers = true;
            _targetNpcs = true;
        }
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
    }

    public static void SetPausedAll(bool paused)
    {
        foreach (var b in _activeBullets)
        {
            if (b != null)
            {
                b.SetPaused(paused);
            }
        }
    }

    public static void ClearAll()
    {
        var snapshot = new List<BulletActorLite>(_activeBullets);
        foreach (var b in snapshot)
        {
            if (b != null)
            {
                b.Recycle();
            }
        }
        _activeBullets.Clear();
    }
    #endregion

    #region Collision
    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject targetGo)
    {
        if (_recycling)
        {
            return;
        }

        if (targetGo == null || targetGo == _owner)
        {
            return;
        }

        var target = targetGo.GetComponent<MPCharacterSoulActorBase>();
        if (target != null && !target.IsDead && IsValidTarget(target))
        {
            if (target is MPNpcSoulActor && _owner != null && _owner.GetComponent<MPSoulActor>() != null)
            {
                MPRoomManager.Inst?.RegisterPlayerDamageDealt(Mathf.RoundToInt(_damage));
            }
            else if (target is MPSoulActor && _owner != null && _owner.GetComponent<MPNpcSoulActor>() != null)
            {
                MPRoomManager.Inst?.RegisterPlayerDamageTaken(Mathf.RoundToInt(_damage));
            }

            target.TakeDamage(Mathf.RoundToInt(_damage));
            Recycle();
            return;
        }

        // If using OverlapSphere instead of colliders, you could add it here.
    }
    #endregion

    #region Private Methods
    private void Recycle()
    {
        if (_recycling)
        {
            return;
        }

        _recycling = true;

        if (PoolManager.Inst != null && !string.IsNullOrEmpty(_poolKey))
        {
            PoolManager.DespawnItemToPool(_poolKey, this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool IsValidTarget(MPCharacterSoulActorBase target)
    {
        if (target == null || target.IsDead)
        {
            return false;
        }

        if (_targetPlayers && target is MPSoulActor)
        {
            return true;
        }

        if (_targetNpcs && target is MPNpcSoulActor)
        {
            return true;
        }

        return false;
    }

    private void TryHitOverlap()
    {
        var mask = _hitMask.value == 0 ? Physics.DefaultRaycastLayers : _hitMask.value;
        var hits = Physics.OverlapSphere(transform.position, _radius, mask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            var go = hits[i].gameObject;
            if (go == null || go == _owner)
            {
                continue;
            }

            var target = go.GetComponent<MPCharacterSoulActorBase>();
            if (target != null && IsValidTarget(target))
            {
                target.TakeDamage(Mathf.RoundToInt(_damage));
                Recycle();
                return;
            }
        }
    }

    private void TryHitRay(Vector3 start, Vector3 displacement)
    {
        var distance = displacement.magnitude;
        if (distance <= 0.0001f)
        {
            return;
        }

        var mask = _hitMask.value == 0 ? Physics.DefaultRaycastLayers : _hitMask.value;
        if (Physics.SphereCast(start, _radius, displacement.normalized, out var hit, distance, mask, QueryTriggerInteraction.Collide))
        {
            HandleHit(hit.collider.gameObject);
        }
    }
    #endregion
}

public struct BulletSpawnContext
{
    public GameObject Owner;
    public Vector3 Direction;
    public float Speed;
    public float MaxLifeTime;
    public float Damage;
    public string PoolKey;
}
