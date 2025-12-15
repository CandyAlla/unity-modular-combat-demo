using UnityEngine;

// GameClientManager owns high-level client lifecycle and scene transitions.
// It is created by GameEntry and persists across scene loads for the demo loop.
// Logs focus on the minimal boot-to-battle chain without gameplay.
public class GameClientManager : MonoBehaviour
{
    #region Fields
    public static GameClientManager Instance { get; private set; }

    private SceneStateSystem _sceneStateSystem;
    private DataCtrl _dataCtrl;
    private int _selectedStageId = 1;
    private bool _initialized;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Public Methods
    public void OnInit()
    {
        if (_initialized)
        {
            Debug.Log("[GameClientManager] OnInit skipped (already initialized).");
            return;
        }

        PoolManager.CreatePoolManager();
        EnsureUIManager();

        _sceneStateSystem = new SceneStateSystem();
        _dataCtrl = DataCtrl.Instance;
        _dataCtrl.InitAllChapterInfos();
        _sceneStateSystem.RegisterSceneManager(new LoginSceneManager());
        _sceneStateSystem.RegisterSceneManager(new BattleSceneManager());
        _sceneStateSystem.RegisterSceneManager(new TestSceneManager());
        _initialized = true;
        Debug.Log("[GameClientManager] OnInit");
    }

    public void OnGameBegin()
    {
        if (_sceneStateSystem == null)
        {
            Debug.LogError("[GameClientManager] OnGameBegin called before OnInit.");
            return;
        }

        Debug.Log("[GameClientManager] OnGameBegin");
        SetTransition(SceneStateId.Login);
    }

    public async void SetTransition(SceneStateId target)
    {
        Debug.Log($"[GameClientManager] SetTransition to {target}");
        var success = await _sceneStateSystem.PerformTransition(target);
        if (!success)
        {
            Debug.LogError($"[GameClientManager] Transition to {target} failed.");
        }
    }

    public void SetSelectedStageId(int stageId)
    {
        _selectedStageId = Mathf.Max(1, stageId);
        Debug.Log($"[GameClientManager] Selected stage set to {_selectedStageId}");
    }

    public int GetSelectedStageId() => _selectedStageId;

    private void EnsureUIManager()
    {
        if (UIManager.Inst != null)
        {
            return;
        }

        var existing = FindObjectOfType<UIManager>();
        if (existing != null)
        {
            return;
        }

        var uiRoot = new GameObject("UIManager");
        uiRoot.AddComponent<UIManager>();
    }
    #endregion
}
