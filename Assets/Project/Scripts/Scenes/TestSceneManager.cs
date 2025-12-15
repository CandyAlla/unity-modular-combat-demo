using UnityEngine;

public class TestSceneManager : ISceneManager
{
    #region Properties
    public SceneStateId Id => SceneStateId.Test;
    #endregion
    [SerializeField] private MPRoomManager _roomManager;
    #region Scene Lifecycle
    public void DoBeforeEntering()
    {
        Debug.Log("[TestSceneManager] DoBeforeEntering");
    }

    public void DoEntered()
    {
        Debug.Log("[TestSceneManager] DoEntered");
        EnsureRoomManager();
        DataCtrl.Instance.InitAllChapterInfos();
        _roomManager.InitializeStage(1);
        _roomManager.StartBattle();
        // Try to open debug panel if it exists in the scene
        var debugPanel = UnityEngine.Object.FindObjectOfType<UI_DebugPanel>(true);
        if (debugPanel != null)
        {
            debugPanel.Open();
        }
        else
        {
            Debug.LogWarning("[TestSceneManager] UI_DebugPanel not found in scene.");
        }
    }

    public void DoBeforeLeaving()
    {
        Debug.Log("[TestSceneManager] DoBeforeLeaving");
    }
    #endregion
    
    
    private void EnsureRoomManager()
    {
        if (_roomManager == null)
        {
            _roomManager = Object.FindObjectOfType<MPRoomManager>();
        }

        if (_roomManager == null)
        {
            Debug.LogError("[BattleSceneManager] MPRoomManager not found in scene.");
        }
    }
}
