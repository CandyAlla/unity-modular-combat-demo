using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI_BattleSettlement shows battle result and provides retry/back controls.
public class UI_BattleSettlement : UIBase
{
    #region Inspector
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private TMP_Text _killText;
    [SerializeField] private TMP_Text _spawnText;
    [SerializeField] private TMP_Text _damageDealtText;
    [SerializeField] private TMP_Text _damageTakenText;
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
    public void Show(BattleResultData result)
    {
        if (result == null)
        {
            Debug.LogWarning("[UI_BattleSettlement] Show called with null result.");
            return;
        }

        if (_roomManager != null)
        {
            _roomManager.SetHudVisible(false);
        }

        if (_resultText != null)
        {
            _resultText.text = result.IsWin ? "Victory" : "Defeat";
        }

        if (_timeText != null)
        {
            _timeText.text = GlobalHelper.FormatTime(result.DurationSeconds);
        }

        var stats = result.Stats;
        if (stats != null)
        {
            if (_killText != null) _killText.text = $"Kills: {stats.TotalEnemyKills}";
            if (_spawnText != null) _spawnText.text = $"Spawns: {stats.TotalEnemySpawns}";
            if (_damageDealtText != null) _damageDealtText.text = $"Damage Dealt: {stats.PlayerDamageDealt}";
            if (_damageTakenText != null) _damageTakenText.text = $"Damage Taken: {stats.PlayerDamageTaken}";
        }

        Open();
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
        if (_roomManager != null)
        {
            _roomManager.SetHudVisible(true);
        }
        _roomManager?.RestartLevel();
    }

    private void OnClickBack()
    {
        if (_roomManager != null)
        {
            _roomManager.SetHudVisible(true);
        }
        GameClientManager.Instance.SetTransition(SceneStateId.Login);
    }
    #endregion
}
