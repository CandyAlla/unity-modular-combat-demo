using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI_BattleSettlement shows battle result and provides retry/back controls.
public class UI_BattleSettlement : MonoBehaviour
{
    #region Inspector
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private MPRoomManager _roomManager;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        gameObject.SetActive(false);

        if (_roomManager == null)
        {
            _roomManager = FindObjectOfType<MPRoomManager>();
        }

        if (_retryButton != null)
        {
            _retryButton.onClick.AddListener(OnClickRetry);
        }

        if (_backButton != null)
        {
            _backButton.onClick.AddListener(OnClickBack);
        }
    }
    #endregion

    #region Public Methods
    public void Show(bool isWin)
    {
        gameObject.SetActive(true);
        if (_resultText != null)
        {
            _resultText.text = isWin ? "Victory" : "Defeat";
        }
    }

    public void SetTime(float seconds)
    {
        if (_timeText == null)
        {
            return;
        }

        _timeText.text = GlobalHelper.FormatTime(seconds);
    }
    #endregion

    #region Private Methods
    private void OnClickRetry()
    {
        gameObject.SetActive(false);
        _roomManager?.RestartLevel();
    }

    private void OnClickBack()
    {
        GameClientManager.Instance.SetTransition(SceneStateId.Login);
    }
    #endregion
}
