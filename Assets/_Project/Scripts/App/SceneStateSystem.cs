using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneStateId
{
    None = 0,
    BattleScene = 1
}


// SceneStateSystem is a tiny state machine that hands off to per-scene managers.
// It registers scene managers and drives async transitions in this AOT-only demo.
// Transition logging keeps the Map_GameEntry -> Map_BattleScene path visible.
public class SceneStateSystem
{
    private readonly Dictionary<SceneStateId, ISceneManager> _managers = new Dictionary<SceneStateId, ISceneManager>();

    private SceneStateId _currentId = SceneStateId.None;
    private ISceneManager _currentManager;

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
        Debug.Log($"[SceneStateSystem] PerformTransition -> {target}");

        if (!_managers.TryGetValue(target, out var nextManager))
        {
            Debug.LogError($"[SceneStateSystem] No scene manager for target: {target}");
            return;
        }

        if (_currentManager != null)
        {
            Debug.Log($"[SceneStateSystem] Leaving {_currentId}");
            _currentManager.DoBeforeLeaving();
        }

        if (target == SceneStateId.BattleScene)
        {
            var operation = SceneManager.LoadSceneAsync("Map_BattleScene");
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }
        else
        {
            Debug.LogWarning($"[SceneStateSystem] Transition target {target} not handled explicitly.");
        }

        _currentId = target;
        _currentManager = nextManager;
        _currentManager.DoBeforeEntering();
        _currentManager.DoEntered();
    }
}
