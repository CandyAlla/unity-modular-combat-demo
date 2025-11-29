using UnityEngine;

// LoginSceneManager handles the lifecycle callbacks for the login scene.
// It only logs enter/exit to keep the flow transparent during early stages.
public class LoginSceneManager : ISceneManager    
{
    public SceneStateId Id => SceneStateId.Login;
    public void DoBeforeEntering()
    {
        Debug.Log("[LoginSceneManager] DoBeforeEntering");
    }

    public void DoEntered()
    {
        Debug.Log("[LoginSceneManager] DoEntered");
    }

    public void DoBeforeLeaving()
    {
        Debug.Log("[LoginSceneManager] DoBeforeLeaving");
    }
}
