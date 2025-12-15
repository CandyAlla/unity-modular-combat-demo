using UnityEngine;

// BattleSceneManager logs hooks for entering/leaving the BattleScene map.
// It is registered by the SceneStateSystem to show the scene flow chain.
// No gameplay logic is included in this step-one setup.
public class BattleSceneManager : ISceneManager
{
    #region Properties
    public SceneStateId Id => SceneStateId.Battle;
    #endregion

    #region Inspector
    [SerializeField] private MPRoomManager _roomManager;
    #endregion

    #region Scene Lifecycle
    public void DoBeforeEntering()
    {
        Debug.Log("[BattleSceneManager] DoBeforeEntering");
    }

    public void DoEntered()
    {
        Debug.Log("[BattleSceneManager] DoEntered");

        EnsureRoomManager();
        DataCtrl.Instance.InitAllChapterInfos();
        var stageId = GameClientManager.Instance != null ? GameClientManager.Instance.GetSelectedStageId() : 1;
        _roomManager.InitializeStage(stageId);
    }

    public void DoBeforeLeaving()
    {
        Debug.Log("[BattleSceneManager] DoBeforeLeaving");
    }
    #endregion

    #region Private Methods
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
    #endregion
}
