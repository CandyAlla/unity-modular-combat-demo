using System.Collections.Generic;
using UnityEngine;

// PoolModelCtrl manages per-prefab pools within a scene.
// It parents spawned instances under the provided runtimeActorsRoot.
public class PoolModelCtrl : MonoBehaviour
{
    #region Types
    private class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private readonly Transform _defaultParent;

        public ObjectPool(GameObject prefab, Transform defaultParent, int preload)
        {
            _prefab = prefab;
            _defaultParent = defaultParent;
            Preload(preload);
        }

        public GameObject Get(Vector3 position, Quaternion rotation, Transform parentOverride = null)
        {
            var parent = parentOverride != null ? parentOverride : _defaultParent;

            GameObject instance = null;
            while (_pool.Count > 0 && instance == null)
            {
                instance = _pool.Dequeue();
            }

            if (instance == null)
            {
                if (_prefab == null)
                {
                    Debug.LogWarning("[PoolModelCtrl] Prefab missing for pool.");
                    return null;
                }

                instance = Instantiate(_prefab, position, rotation, parent);
            }
            else
            {
                instance.transform.SetParent(parent, false);
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.transform.localScale = Vector3.one;
                instance.SetActive(true);
            }

            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.SetActive(false);
            _pool.Enqueue(instance);
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        private void Preload(int count)
        {
            if (_prefab == null || count <= 0)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var obj = Instantiate(_prefab, _defaultParent);
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }
    }
    #endregion

    #region Fields
    private readonly Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();
    private Transform _runtimeActorsRoot;
    #endregion

    #region Public Methods
    public void Initialize(Transform runtimeActorsRoot)
    {
        _runtimeActorsRoot = runtimeActorsRoot;
    }

    public void InitPoolItem<T>(string key, GameObject prefab, int preloadCount) where T : Component
    {
        if (string.IsNullOrEmpty(key) || prefab == null)
        {
            Debug.LogWarning("[PoolModelCtrl] InitPoolItem missing key or prefab.");
            return;
        }

        if (_pools.ContainsKey(key))
        {
            return;
        }

        var pool = new ObjectPool(prefab, _runtimeActorsRoot, preloadCount);
        _pools.Add(key, pool);
    }

    public T SpawnItemFromPool<T>(string key, Vector3 position, Quaternion rotation, Transform parentOverride = null) where T : Component
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            Debug.LogWarning($"[PoolModelCtrl] Pool not found for key: {key}");
            return null;
        }

        var obj = pool.Get(position, rotation, parentOverride);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    public void DespawnItemToPool(string key, GameObject instance)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            Debug.LogWarning($"[PoolModelCtrl] Pool not found for key: {key}");
            return;
        }

        pool.Release(instance);
    }

    public void ClearPools()
    {
        foreach (var pool in _pools.Values)
        {
            pool.Clear();
        }

        _pools.Clear();
    }
    #endregion
}
