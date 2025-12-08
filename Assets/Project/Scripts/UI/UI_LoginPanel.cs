using UnityEngine;
using UnityEngine.UI;

// UI_LoginPanel wires login buttons to the scene flow through GameClientManager.
// It only drives transitions and quit; no scene loading directly.
public class UI_LoginPanel : UIBase
{
    #region Inspector
    [SerializeField] private Button _enterGameButton;
    [SerializeField] private Button _quitButton;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
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
    #endregion

    #region Private Methods
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
    #endregion
}
