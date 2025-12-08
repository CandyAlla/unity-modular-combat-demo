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
        PoolManager.CreatePoolManager();
        if (UIManager.Inst == null)
        {
            var uiRoot = new GameObject("UIManager");
            uiRoot.AddComponent<UIManager>();
        }
        // Scene State System setup
        _sceneStateSystem = new SceneStateSystem();
        _dataCtrl = DataCtrl.Instance;
        _dataCtrl.InitAllChapterInfos();
        _sceneStateSystem.RegisterSceneManager(new LoginSceneManager());
        _sceneStateSystem.RegisterSceneManager(new BattleSceneManager());
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

    public void SetTransition(SceneStateId target)
    {
        Debug.Log($"[GameClientManager] SetTransition to {target}");
        _sceneStateSystem.PerformTransition(target);
    }
    #endregion
}
