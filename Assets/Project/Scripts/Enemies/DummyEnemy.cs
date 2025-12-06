using UnityEngine;

// DummyEnemy is a placeholder enemy that reports its destruction to the room manager.
// It does not move or attack; it only adjusts alive counts on spawn/despawn.
public class DummyEnemy : MonoBehaviour
{
    #region Fields
    private MPRoomManager _roomManager;
    [SerializeField] private string _poolKey = "Enemy_Dummy";
    [SerializeField] private float _autoDespawnSeconds = 10f;
    private float _aliveTime;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _roomManager = FindObjectOfType<MPRoomManager>();
        _aliveTime = 0f;
    }

    private void OnEnable()
    {
        _aliveTime = 0f;
    }

    private void Update()
    {
        _aliveTime += Time.deltaTime;
        if (_aliveTime >= _autoDespawnSeconds)
        {
            Despawn();
        }
    }

    private void OnDestroy()
    {
        if (_roomManager != null)
        {
            _roomManager.RegisterEnemyDestroyed();
        }
    }
    #endregion

    #region Private Methods
    private void Despawn()
    {
        if (_roomManager != null)
        {
            _roomManager.RegisterEnemyDestroyed();
        }

        var pool = PoolManager.Inst;

        if (pool != null)
        {
            PoolManager.DespawnItemToPool(_poolKey, this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
}
