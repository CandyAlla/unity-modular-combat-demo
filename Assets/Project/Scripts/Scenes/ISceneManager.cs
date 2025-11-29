
// ISceneManager defines the minimal hooks each scene manager must expose.
// Implementations log or handle before/after enter and before leave transitions.
// SceneStateSystem looks up managers by Id to drive scene changes.
public interface ISceneManager
{
    SceneStateId Id { get; }
    void DoBeforeEntering();
    void DoEntered();
    void DoBeforeLeaving();
}
