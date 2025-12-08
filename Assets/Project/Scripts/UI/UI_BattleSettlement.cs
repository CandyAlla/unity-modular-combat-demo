using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI_BattleSettlement shows battle result and provides retry/back controls.
public class UI_BattleSettlement : UIBase
{
    #region Inspector
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private MPRoomManager _roomManager;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
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
        if (_resultText != null)
        {
            _resultText.text = isWin ? "Victory" : "Defeat";
        }

        Open();
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
    protected override void OnOpenUI()
    {
        base.OnOpenUI();
        if (_retryButton != null)
        {
            SetDefaultSelectable(_retryButton);
        }
    }

    private void OnClickRetry()
    {
        Close();
        _roomManager?.RestartLevel();
    }

    private void OnClickBack()
    {
        GameClientManager.Instance.SetTransition(SceneStateId.Login);
    }
    #endregion
}
