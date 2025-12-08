using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneStateId
{
    GameEntry = 0,
    Login = 1,
    Battle = 2,
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

    public async void PerformTransition(SceneStateId target)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning($"[SceneStateSystem] Transition already in progress; requested {target} ignored.");
            return;
        }

        Debug.Log($"[SceneStateSystem] Begin transition: {_currentId} -> {target}");

        if (target == _currentId)
        {
            Debug.Log($"[SceneStateSystem] Already in target state: {target}, skipping.");
            return;
        }

        if (!_managers.TryGetValue(target, out var nextManager))
        {
            Debug.LogError($"[SceneStateSystem] No scene manager for target: {target}");
            return;
        }

        if (_currentManager != null)
        {
            Debug.Log($"[SceneStateSystem] Leaving {_currentId}");
            PoolManager.Inst?.DoBeforeLeavingScene();
            _currentManager.DoBeforeLeaving();
        }

        var sceneName = ResolveSceneName(target);
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[SceneStateSystem] Unsupported target state: {target}");
            return;
        }

        _isTransitioning = true;

        Debug.Log($"[SceneStateSystem] Loading scene: {sceneName}");

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[SceneStateSystem] Failed to start loading scene: {sceneName}");
            _isTransitioning = false;
            return;
        }

        try
        {
            while (!op.isDone)
            {
                await Task.Yield();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SceneStateSystem] Exception during scene load {sceneName}: {ex}");
            _isTransitioning = false;
            return;
        }

        try
        {
            PoolManager.Inst?.DoBeforeEnteringScene(sceneName);

            _currentId = target;
            _currentManager = nextManager;
            _currentManager.DoBeforeEntering();
            _currentManager.DoEntered();

            Debug.Log($"[SceneStateSystem] Entered state: {_currentId}");
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
                return "Map_GameEntry";
            case SceneStateId.Login:
                return "Map_Login";
            case SceneStateId.Battle:
                return "Map_BattleScene";
            default:
                return null;
        }
    }
    #endregion
}
