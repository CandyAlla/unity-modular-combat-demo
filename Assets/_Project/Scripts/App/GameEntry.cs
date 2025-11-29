using UnityEngine;

// GameEntry is the MonoBehaviour entry point that bootstraps the client.
// It lives in the Map_GameEntry scene and spins up the client manager root.
// Logging here helps trace the startup flow end to end.
public class GameEntry : MonoBehaviour
{
    public static GameEntry Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[GameEntry] Awake");
    }

    private void Start()
    {
        if (GameClientManager.Instance == null)
        {
            var root = GameObject.Find("__GameClientRoot");
            if (root == null)
            {
                root = new GameObject("__GameClientRoot");
            }

            var manager = root.GetComponent<GameClientManager>();
            if (manager == null)
            {
                manager = root.AddComponent<GameClientManager>();
            }
        }

        GameClientManager.Instance.OnInit();
        GameClientManager.Instance.OnGameBegin();
    }
}
