using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI_BattlePanel handles in-battle UI interactions such as starting and exiting.
// It wires buttons to GameClientManager and MPRoomManager instead of loading scenes directly.
// Keep it focused on UI events only; scene flow lives in the managers/state system.
public class UI_BattlePanel : UIBase
{
    #region Inspector
    [SerializeField] private Button _exitBattleButton;
    [SerializeField] private Button _startBattleButton;
    [SerializeField] private MPRoomManager _roomManager;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _startButtonLabel;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        if(_exitBattleButton != null)
            _exitBattleButton.onClick.AddListener(OnClickExitBattle);

        if (_startBattleButton != null)
            _startBattleButton.onClick.AddListener(OnClickStartBattle);

        if (_roomManager == null)
            _roomManager = FindObjectOfType<MPRoomManager>();

        if (_startButtonLabel == null && _startBattleButton != null)
            _startButtonLabel = _startBattleButton.GetComponentInChildren<TMP_Text>();

        UpdateStartButtonLabel();
    }

    private void OnDestroy()
    {
        if(_exitBattleButton != null)
            _exitBattleButton.onClick.RemoveListener(OnClickExitBattle);

        if (_startBattleButton != null)
            _startBattleButton.onClick.RemoveListener(OnClickStartBattle);
    }

    private void Update()
    {
        UpdateTimer();
        UpdateStartButtonLabel();
    }
    #endregion

    #region Private Methods
    protected override void OnOpenUI()
    {
        base.OnOpenUI();
        if (_startBattleButton != null)
        {
            SetDefaultSelectable(_startBattleButton);
        }
    }

    private void UpdateTimer()
    {
        if (_timerText == null)
        {
            return;
        }

        if (_roomManager == null)
        {
            _timerText.text = "--:--";
            return;
        }

        var state = _roomManager.State;
        var elapsed = _roomManager.IsLevelRunning ? _roomManager.GetCurrentTime() : 0f;
        var duration = _roomManager.GetLevelDuration();

        _timerText.text = $"{GlobalHelper.FormatTime(elapsed)} / {GlobalHelper.FormatTime(duration)} ({state})";
    }

    private void OnClickStartBattle()
    {
        Debug.Log("[BattleUI] Start Battle clicked");
        if (_roomManager == null)
        {
            Debug.LogWarning("[BattleUI] MPRoomManager not found; cannot control battle.");
            return;
        }

        switch (_roomManager.State)
        {
            case MPRoomManager.RoomState.NotStarted:
                _roomManager.StartBattle();
                break;
            case MPRoomManager.RoomState.Running:
                _roomManager.PauseBattle();
                break;
            case MPRoomManager.RoomState.Paused:
                _roomManager.ResumeBattle();
                break;
            case MPRoomManager.RoomState.Finished:
                Debug.LogWarning("[BattleUI] Battle already finished; start button disabled.");
                break;
        }

        UpdateStartButtonLabel();
    }

    private void OnClickExitBattle()
    {
        Debug.Log("[BattleUI] Exit Battle clicked");
        GameClientManager.Instance.SetTransition(SceneStateId.Login);
    }

    private void UpdateStartButtonLabel()
    {
        if (_startButtonLabel == null)
        {
            return;
        }

        if (_roomManager == null)
        {
            _startButtonLabel.text = "Start";
            return;
        }

        switch (_roomManager.State)
        {
            case MPRoomManager.RoomState.NotStarted:
                _startButtonLabel.text = "Start";
                break;
            case MPRoomManager.RoomState.Running:
                _startButtonLabel.text = "Pause";
                break;
            case MPRoomManager.RoomState.Paused:
                _startButtonLabel.text = "Resume";
                break;
            case MPRoomManager.RoomState.Finished:
                _startButtonLabel.text = "Finished";
                break;
        }
    }
    #endregion
}
