using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneStateId
{
    GameEntry = 0,
    Login = 1,
    Battle = 2,
    Test = 3,
}


// SceneStateSystem is a tiny state machine that hands off to per-scene managers.
// It registers scene managers and drives async transitions in this AOT-only demo.
// Transition logging keeps the Map_GameEntry -> Map_LoginScene path visible.
public class SceneStateSystem
{
    #region Fields
    private readonly Dictionary<SceneStateId, ISceneManager> _managers = new Dictionary<SceneStateId, ISceneManager>();

    private SceneStateId _currentId = SceneStateId.GameEntry;
    private ISceneManager _currentManager;
    private bool _isTransitioning;
    #endregion

    #region Public Methods
    public void RegisterSceneManager(ISceneManager manager)
    {
        if (manager == null)
        {
            Debug.LogWarning("[SceneStateSystem] Tried to register null scene manager.");
            return;
        }

        _managers[manager.Id] = manager;
        Debug.Log($"[SceneStateSystem] Registered manager for {manager.Id}");
    }

    public async Task<bool> PerformTransition(SceneStateId target, int timeoutMs = 30000, CancellationToken externalToken = default)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning($"[SceneStateSystem] Transition already in progress; requested {target} ignored.");
            return false;
        }

        Debug.Log($"[SceneStateSystem] Begin transition: {_currentId} -> {target}");

        if (target == _currentId)
        {
            Debug.Log($"[SceneStateSystem] Already in target state: {target}, skipping.");
            return true;
        }

        if (!_managers.TryGetValue(target, out var nextManager))
        {
            Debug.LogError($"[SceneStateSystem] No scene manager for target: {target}");
            return false;
        }

        if (_currentManager != null)
        {
            Debug.Log($"[SceneStateSystem] Leaving {_currentId}");
            PoolManager.Inst?.DoBeforeLeavingScene();
            _currentManager.DoBeforeLeaving();
        }

        // Clear global event bus to avoid stale scene references
        EventBus.OnClearAllDicDELEvents();

        var sceneName = ResolveSceneName(target);
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[SceneStateSystem] Unsupported target state: {target}");
            return false;
        }

        _isTransitioning = true;

        Debug.Log($"[SceneStateSystem] Loading scene: {sceneName}");

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[SceneStateSystem] Failed to start loading scene: {sceneName}");
            _isTransitioning = false;
            return false;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        if (timeoutMs > 0)
        {
            cts.CancelAfter(timeoutMs);
        }

        try
        {
            while (!op.isDone && !cts.Token.IsCancellationRequested)
            {
                await Task.Yield();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SceneStateSystem] Exception during scene load {sceneName}: {ex}");
            _isTransitioning = false;
            return false;
        }

        if (cts.IsCancellationRequested && !op.isDone)
        {
            Debug.LogError($"[SceneStateSystem] Scene load timeout/cancelled for {sceneName}");
            _isTransitioning = false;
            return false;
        }

        try
        {
            PoolManager.Inst?.DoBeforeEnteringScene(sceneName);

            _currentId = target;
            _currentManager = nextManager;
            _currentManager.DoBeforeEntering();
            _currentManager.DoEntered();

            Debug.Log($"[SceneStateSystem] Entered state: {_currentId}");
            return true;
        }
        finally
        {
            _isTransitioning = false;
        }
    }
    #endregion

    #region Private Methods
    private string ResolveSceneName(SceneStateId target)
    {
        switch (target)
        {
            case SceneStateId.GameEntry:
                return GameConsts.MAP_GAMEENTRY;
            case SceneStateId.Login:
                return GameConsts.MAP_LOGIN;
            case SceneStateId.Battle:
                return GameConsts.MAP_BATTLESCENE;
            case SceneStateId.Test:
                return GameConsts.MAP_TESTSCENE;
            default:
                return null;
        }
    }
    #endregion
}
