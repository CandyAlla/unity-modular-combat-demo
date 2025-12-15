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
    [Header("Active Skill")]
    [SerializeField] private Button _activeSkillButton;
    [SerializeField] private Button _activeSecondSkillButton;
    [SerializeField] private Image _activeSkillCooldownMask;
    [SerializeField] private Image _activeSecondSkillCooldownMask;
    [SerializeField] private float _cooldownMaskRotateSpeed = 180f;
    [SerializeField] private MPSoulActor _player;
    #endregion

    #region Fields
    private MPSkillActorLite _skillActor;
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

        if (_activeSkillButton != null)
            _activeSkillButton.onClick.AddListener(OnClickActiveSkill);
        
        if(_activeSecondSkillButton != null)
            _activeSecondSkillButton.onClick.AddListener(OnClickSecondActiveSkill);

        UpdateStartButtonLabel();
    }

    private void OnDestroy()
    {
        if(_exitBattleButton != null)
            _exitBattleButton.onClick.RemoveListener(OnClickExitBattle);

        if (_startBattleButton != null)
            _startBattleButton.onClick.RemoveListener(OnClickStartBattle);

        if (_activeSkillButton != null)
            _activeSkillButton.onClick.RemoveListener(OnClickActiveSkill);
        
        if(_activeSecondSkillButton != null)
            _activeSecondSkillButton.onClick.RemoveListener(OnClickSecondActiveSkill);
    }

    private void Update()
    {
        UpdateTimer();
        UpdateStartButtonLabel();
        UpdateSkillUI(_activeSkillButton, _activeSkillCooldownMask, SkillSlot.Active);
        UpdateSkillUI(_activeSecondSkillButton, _activeSecondSkillCooldownMask, SkillSlot.Secondary);
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
        var duration = _roomManager.GetCurrentStageDuration();

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

    private void OnClickSecondActiveSkill()
    {
        if (_player == null)
        {
            TryCacheActors();
        }

        if (_player != null)
        {
            _player.TryCastSecondarySkillFromUI();
        }
        
    }
    
    private void OnClickActiveSkill()
    {
        if (_player == null)
        {
            TryCacheActors();
        }

        if (_player != null)
        {
            _player.TryCastActiveSkillFromUI();
        }
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

    private enum SkillSlot
    {
        Active,
        Secondary
    }

    private void UpdateSkillUI(Button button, Image mask, SkillSlot slot)
    {
        if (button == null)
        {
            return;
        }

        TryCacheActors();

        // Keep button visible; just disable when not usable.
        if (!button.gameObject.activeSelf)
        {
            button.gameObject.SetActive(true);
        }

        var running = _roomManager != null && _roomManager.State == MPRoomManager.RoomState.Running && !_roomManager.IsPaused;
        var playerReady = _player != null && !_player.IsDead;

        float remaining = 0f;
        float total = 0f;
        bool ready = false;
        bool hasSkill = false;

        if (_skillActor != null)
        {
            hasSkill = slot == SkillSlot.Active
                ? _skillActor.TryGetActiveSkillCooldown(out remaining, out total, out ready)
                : _skillActor.TryGetSecondarySkillCooldown(out remaining, out total, out ready);
        }

        var interactable = running && playerReady && (!hasSkill || ready);
        button.interactable = interactable;

        if (mask != null)
        {
            var showMask = hasSkill && !ready && total > 0f;
            mask.gameObject.SetActive(showMask);
            if (showMask)
            {
                var fill = total <= 0.0001f ? 0f : Mathf.Clamp01(remaining / Mathf.Max(total, 0.0001f));
                mask.fillAmount = fill;
            }
            else
            {
                mask.fillAmount = 0f;
            }
        }
    }

    private void TryCacheActors()
    {
        if (_roomManager == null)
        {
            _roomManager = FindObjectOfType<MPRoomManager>();
        }

        if (_player == null && _roomManager != null)
        {
            _player = _roomManager.LocalPlayer;
        }

        if (_player == null)
        {
            _player = FindObjectOfType<MPSoulActor>();
        }

        if (_player != null && _skillActor == null)
        {
            _skillActor = _player.GetSkillActor();
        }

        if (_player != null && _skillActor == null)
        {
            _skillActor = _player.GetComponent<MPSkillActorLite>();
        }
    }
    #endregion
}
