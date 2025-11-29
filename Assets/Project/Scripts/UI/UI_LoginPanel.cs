using UnityEngine;
using UnityEngine.UI;

// UI_LoginPanel wires login buttons to the scene flow through GameClientManager.
// It only drives transitions and quit; no scene loading directly.
public class UI_LoginPanel : MonoBehaviour
{
    [SerializeField] private Button _enterGameButton;
    [SerializeField] private Button _quitButton;

    private void Awake()
    {
        if (_enterGameButton != null)
            _enterGameButton.onClick.AddListener(OnClickEnterGame);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnClickQuit);
    }

    private void OnDestroy()
    {
        if (_enterGameButton != null)
            _enterGameButton.onClick.RemoveListener(OnClickEnterGame);

        if (_quitButton != null)
            _quitButton.onClick.RemoveListener(OnClickQuit);
    }

    private void OnClickEnterGame()
    {
        Debug.Log("[LoginUI] Enter Game clicked");
        GameClientManager.Instance.SetTransition(SceneStateId.Battle);
    }

    private void OnClickQuit()
    {
        Debug.Log("[LoginUI] Quit clicked");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
