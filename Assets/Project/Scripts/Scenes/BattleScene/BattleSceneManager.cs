using UnityEngine;

// BattleSceneManager logs hooks for entering/leaving the BattleScene map.
// It is registered by the SceneStateSystem to show the scene flow chain.
// No gameplay logic is included in this step-one setup.
public class BattleSceneManager : ISceneManager
{
    public SceneStateId Id => SceneStateId.Battle;

    public void DoBeforeEntering()
    {
        Debug.Log("[BattleSceneManager] DoBeforeEntering");
    }

    public void DoEntered()
    {
        Debug.Log("[BattleSceneManager] DoEntered");
    }

    public void DoBeforeLeaving()
    {
        Debug.Log("[BattleSceneManager] DoBeforeLeaving");
    }
}
