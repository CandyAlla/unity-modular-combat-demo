using System;
using System.Collections.Generic;
using UnityEngine;

// PoolManager creates a persistent pool root and handles per-scene pool setup.
// It exposes static APIs to init, spawn, and despawn pooled items keyed by string.
public class PoolManager : MonoBehaviour
{
    #region Types
    [Serializable]
    public class PoolConfig
    {
        public string Key;
        public GameObject Prefab;
        public int PreloadCount = 0;
    }
    #endregion

    #region Inspector
    [SerializeField] private List<PoolConfig> _poolConfigs = new List<PoolConfig>();
    #endregion

    #region Fields
    public static PoolManager Inst { get; private set; }

    private GameObject _poolManagerRoot;
    private GameObject _scenePoolRoot;
    private PoolModelCtrl _poolModelCtrl;
    private Transform _runtimeActorsRoot;
    #endregion

    #region Static API
    public static void CreatePoolManager()
    {
        if (Inst != null)
        {
            return;
        }

        var rootGo = new GameObject("PoolManagerRoot");
        DontDestroyOnLoad(rootGo);
        var manager = rootGo.AddComponent<PoolManager>();
        Inst = manager;
        manager.InitializeRoot(rootGo);
    }

    public static void InitPoolItem<T>(string key, GameObject prefab, int preloadCount = 0) where T : Component
    {
        if (Inst == null)
        {
            CreatePoolManager();
        }

        if (Inst._poolModelCtrl == null)
        {
            Debug.LogWarning("[PoolManager] InitPoolItem ignored: pool model controller not ready.");
            return;
        }

        Inst._poolModelCtrl.InitPoolItem<T>(key, prefab, preloadCount);
    }

    public static T SpawnItemFromPool<T>(string key, Vector3 position, Quaternion rotation) where T : Component
    {
        if (Inst == null)
        {
            CreatePoolManager();
        }

        if (Inst._poolModelCtrl == null)
        {
            Debug.LogWarning("[PoolManager] SpawnItemFromPool ignored: pool model controller not ready.");
            return null;
        }

        return Inst._poolModelCtrl.SpawnItemFromPool<T>(key, position, rotation, Inst._runtimeActorsRoot);
    }

    public static void DespawnItemToPool<T>(string key, T instance) where T : Component
    {
        if (Inst == null || instance == null)
        {
            return;
        }

        if (Inst._poolModelCtrl == null)
        {
            Debug.LogWarning("[PoolManager] DespawnItemToPool ignored: pool model controller not ready.");
            return;
        }

        Inst._poolModelCtrl.DespawnItemToPool(key, instance.gameObject);
    }
    #endregion

    #region Public Scene Hooks
    public void DoBeforeLeavingScene()
    {
        _poolModelCtrl?.ClearPools();

        if (_scenePoolRoot != null)
        {
            Destroy(_scenePoolRoot);
            _scenePoolRoot = null;
            _runtimeActorsRoot = null;
            _poolModelCtrl = null;
        }
    }

    public void DoBeforeEnteringScene(string sceneName)
    {
        if (_scenePoolRoot != null)
        {
            Destroy(_scenePoolRoot);
        }

        _scenePoolRoot = new GameObject($"ScenePoolRoot_{sceneName}");
        _scenePoolRoot.transform.SetParent(_poolManagerRoot.transform, false);

        var poolModelObj = new GameObject("PoolModelCtrl");
        poolModelObj.transform.SetParent(_scenePoolRoot.transform, false);
        _poolModelCtrl = poolModelObj.AddComponent<PoolModelCtrl>();

        var runtimeRoot = new GameObject("RuntimeActorsRoot");
        runtimeRoot.transform.SetParent(poolModelObj.transform, false);
        _runtimeActorsRoot = runtimeRoot.transform;

        _poolModelCtrl.Initialize(_runtimeActorsRoot);

        foreach (var config in _poolConfigs)
        {
            if (config != null && !string.IsNullOrEmpty(config.Key) && config.Prefab != null)
            {
                _poolModelCtrl.InitPoolItem<Component>(config.Key, config.Prefab, config.PreloadCount);
            }
        }
    }
    #endregion

    #region Private Methods
    public Transform RuntimeActorsRoot => _runtimeActorsRoot;

    private void InitializeRoot(GameObject rootGo)
    {
        _poolManagerRoot = rootGo;
    }
    #endregion
}
