using System;
using UnityEngine;
using UnityEngine.UI;

// UI_BattlePanel handles in-battle UI interactions such as exiting back to login.
// It wires the exit button to GameClientManager for scene transitions instead of loading scenes directly.
// Keep it focused on UI events only; scene flow lives in the managers/state system.
public class UI_BattlePanel : MonoBehaviour
{
    [SerializeField] private Button _exitBattleButton;

    private void Awake()
    {
        if(_exitBattleButton != null)
            _exitBattleButton.onClick.AddListener(OnClickExitBattle);
    }

    private void OnDestroy()
    {
        if(_exitBattleButton != null)
            _exitBattleButton.onClick.RemoveListener(OnClickExitBattle);
    }

    private void OnClickExitBattle()
    {
        Debug.Log("[BattleUI] Exit Battle clicked");
        GameClientManager.Instance.SetTransition(SceneStateId.Login);
    }
}
