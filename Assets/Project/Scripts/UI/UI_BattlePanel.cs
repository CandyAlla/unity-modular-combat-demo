using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI_BattlePanel handles in-battle UI interactions such as starting and exiting.
// It wires buttons to GameClientManager and MPRoomManager instead of loading scenes directly.
// Keep it focused on UI events only; scene flow lives in the managers/state system.
public class UI_BattlePanel : MonoBehaviour
{
    #region Inspector
    [SerializeField] private Button _exitBattleButton;
    [SerializeField] private Button _startBattleButton;
    [SerializeField] private MPRoomManager _roomManager;
    [SerializeField] private TMP_Text _timerText;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if(_exitBattleButton != null)
            _exitBattleButton.onClick.AddListener(OnClickExitBattle);

        if (_startBattleButton != null)
            _startBattleButton.onClick.AddListener(OnClickStartBattle);

        if (_roomManager == null)
            _roomManager = FindObjectOfType<MPRoomManager>();
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
    }
    #endregion

    #region Private Methods
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
        var elapsed = _roomManager.ElapsedTime;
        var duration = _roomManager.DurationSeconds;

        _timerText.text = $"{GlobalHelper.FormatTime(elapsed)} / {GlobalHelper.FormatTime(duration)} ({state})";
    }

    private void OnClickStartBattle()
    {
        Debug.Log("[BattleUI] Start Battle clicked");
        if (_roomManager != null)
        {
            _roomManager.StartBattle();
        }
        else
        {
            Debug.LogWarning("[BattleUI] MPRoomManager not found; cannot start battle.");
        }
    }

    private void OnClickExitBattle()
    {
        Debug.Log("[BattleUI] Exit Battle clicked");
        GameClientManager.Instance.SetTransition(SceneStateId.Login);
    }
    #endregion
}
