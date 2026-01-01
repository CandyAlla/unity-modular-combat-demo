using UnityEngine;

// LoadingSceneManager handles the lightweight loading scene lifecycle.
public class LoadingSceneManager : ISceneManager
{
    public SceneStateId Id => SceneStateId.Loading;

    public void DoBeforeEntering()
    {
        Debug.Log("[LoadingSceneManager] DoBeforeEntering");
    }

    public void DoEntered()
    {
        Debug.Log("[LoadingSceneManager] DoEntered - Triggering Memory Cleanup (Memory Trough)");
        
        // Force Unity to unload any assets that are no longer referenced in the current "empty" scene.
        Resources.UnloadUnusedAssets();
        
        // Force the C# Garbage Collector to run.
        System.GC.Collect();
    }

    public void DoBeforeLeaving()
    {
        Debug.Log("[LoadingSceneManager] DoBeforeLeaving");
    }
}
