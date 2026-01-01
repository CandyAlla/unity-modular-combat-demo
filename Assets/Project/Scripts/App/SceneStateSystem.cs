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
    Loading = 3,
    Test = 4,
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

        if (target != SceneStateId.Loading)
        {
            return await PerformTransitionViaLoading(target, nextManager, timeoutMs, externalToken);
        }

        return await PerformDirectTransition(target, nextManager, timeoutMs, externalToken);
    }
    #endregion

    #region Private Methods
    private async Task<bool> PerformTransitionViaLoading(SceneStateId target, ISceneManager nextManager, int timeoutMs, CancellationToken externalToken)
    {
        if (!_managers.TryGetValue(SceneStateId.Loading, out var loadingManager) || loadingManager == null)
        {
            Debug.LogWarning("[SceneStateSystem] Loading manager not registered. Falling back to direct load.");
            return await PerformDirectTransition(target, nextManager, timeoutMs, externalToken);
        }

        _isTransitioning = true;
        try
        {
            if (_currentManager != null)
            {
                Debug.Log($"[SceneStateSystem] Leaving {_currentId}");
                PoolManager.Inst?.DoBeforeLeavingScene();
                _currentManager.DoBeforeLeaving();
            }

            EventBus.OnClearAllDicDELEvents();

            if (!await LoadSceneAndEnter(SceneStateId.Loading, loadingManager, timeoutMs, externalToken))
            {
                return false;
            }

            // Give the GC and Asset Unloader a small window to settle in the "empty" scene.
            // This ensures a clear memory trough before the next heavy load starts.
            await Task.Delay(100);

            PoolManager.Inst?.DoBeforeLeavingScene();
            loadingManager.DoBeforeLeaving();
            EventBus.OnClearAllDicDELEvents();

            if (!await LoadSceneAndEnter(target, nextManager, timeoutMs, externalToken))
            {
                return false;
            }

            return true;
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async Task<bool> PerformDirectTransition(SceneStateId target, ISceneManager nextManager, int timeoutMs, CancellationToken externalToken)
    {
        _isTransitioning = true;
        try
        {
            if (_currentManager != null)
            {
                Debug.Log($"[SceneStateSystem] Leaving {_currentId}");
                PoolManager.Inst?.DoBeforeLeavingScene();
                _currentManager.DoBeforeLeaving();
            }

            EventBus.OnClearAllDicDELEvents();

            return await LoadSceneAndEnter(target, nextManager, timeoutMs, externalToken);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async Task<bool> LoadSceneAndEnter(SceneStateId target, ISceneManager manager, int timeoutMs, CancellationToken externalToken)
    {
        var sceneName = ResolveSceneName(target);
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[SceneStateSystem] Unsupported target state: {target}");
            return false;
        }

        Debug.Log($"[SceneStateSystem] Loading scene: {sceneName}");

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[SceneStateSystem] Failed to start loading scene: {sceneName}");
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
            return false;
        }

        if (cts.IsCancellationRequested && !op.isDone)
        {
            Debug.LogError($"[SceneStateSystem] Scene load timeout/cancelled for {sceneName}");
            return false;
        }

        PoolManager.Inst?.DoBeforeEnteringScene(sceneName);

        _currentId = target;
        _currentManager = manager;
        _currentManager.DoBeforeEntering();
        _currentManager.DoEntered();

        Debug.Log($"[SceneStateSystem] Entered state: {_currentId}");
        return true;
    }

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
            case SceneStateId.Loading:
                return GameConsts.MAP_LOADING;
            case SceneStateId.Test:
                return GameConsts.MAP_TESTSCENE;
            default:
                return null;
        }
    }
    #endregion
}
