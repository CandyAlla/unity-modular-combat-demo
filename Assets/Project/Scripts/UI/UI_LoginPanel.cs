using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

// UI_LoginPanel wires login buttons to the scene flow through GameClientManager.
// It only drives transitions and quit; no scene loading directly.
public class UI_LoginPanel : UIBase
{
    #region Inspector
    [SerializeField] private Button _enterGameButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Transform _stageListRoot;
    [SerializeField] private Button _stageItemTemplate;
    [SerializeField] private TMP_Text _stageDisplayText;
    [SerializeField] private TMP_Text _diagnosticText;
    #endregion

    #region Fields
    private int _selectedStageId = 1;
    private readonly List<Button> _spawnedStageButtons = new List<Button>();
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        if (_enterGameButton != null)
            _enterGameButton.onClick.AddListener(OnClickEnterGame);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnClickQuit);

        SyncSelection();
        BuildStageList();
        UpdateDiagnostics();
        UpdateEnterButtonState(false);
    }

    private void OnDestroy()
    {
        if (_enterGameButton != null)
            _enterGameButton.onClick.RemoveListener(OnClickEnterGame);

        if (_quitButton != null)
            _quitButton.onClick.RemoveListener(OnClickQuit);

        foreach (var btn in _spawnedStageButtons)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
            }
        }
        _spawnedStageButtons.Clear();
    }
    #endregion

    #region Private Methods
    protected override void OnOpenUI()
    {
        base.OnOpenUI();
        if (_enterGameButton != null)
        {
            SetDefaultSelectable(_enterGameButton);
        }
    }

    private void OnClickEnterGame()
    {
        Debug.Log("[LoginUI] Enter Game clicked");
        if (DataCtrl.Instance.GetStageInfo(_selectedStageId) == null)
        {
            Debug.LogWarning("[LoginUI] Cannot enter game: no valid stage selected.");
            return;
        }
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

    private void OnClickSelectStage(int stageId, string stageName)
    {
        var info = DataCtrl.Instance.GetStageInfo(stageId);
        if (info == null)
        {
            Debug.LogWarning($"[LoginUI] Stage {stageId} not found in DataCtrl, selection ignored.");
            return;
        }

        _selectedStageId = stageId;
        GameClientManager.Instance?.SetSelectedStageId(stageId);
        if (_stageDisplayText != null)
        {
            _stageDisplayText.text = string.IsNullOrEmpty(stageName) ? $"Stage {stageId}" : stageName;
        }
        Debug.Log($"[LoginUI] Stage selected: {stageName} (ID={stageId})");
        UpdateDiagnostics();
        UpdateEnterButtonState(true);
    }

    private void SyncSelection()
    {
        var current = GameClientManager.Instance != null ? GameClientManager.Instance.GetSelectedStageId() : _selectedStageId;
        _selectedStageId = current;
        var displayName = ResolveStageName(_selectedStageId);
        if (_stageDisplayText != null)
        {
            _stageDisplayText.text = string.IsNullOrEmpty(displayName) ? $"Stage {_selectedStageId}" : displayName;
        }
        UpdateEnterButtonState(!string.IsNullOrEmpty(displayName));
    }

    private void BuildStageList()
    {
        var entries = DataCtrl.Instance.GetAllStageEntries();
        if (_stageItemTemplate == null || _stageListRoot == null)
        {
            Debug.LogWarning("[LoginUI] Stage list not built: missing template or root.");
            return;
        }

        foreach (var btn in _spawnedStageButtons)
        {
            if (btn != null)
            {
                Destroy(btn.gameObject);
            }
        }
        _spawnedStageButtons.Clear();

        _stageItemTemplate.gameObject.SetActive(false);

        foreach (var entry in entries)
        {
            var btnObj = Instantiate(_stageItemTemplate, _stageListRoot);
            btnObj.gameObject.SetActive(true);
            var label = btnObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = string.IsNullOrEmpty(entry.DisplayName) ? $"Stage {entry.StageId}" : entry.DisplayName;
            }

            var capturedId = entry.StageId;
            var capturedName = entry.DisplayName;
            btnObj.onClick.AddListener(() => OnClickSelectStage(capturedId, capturedName));

            _spawnedStageButtons.Add(btnObj);
        }
    }

    private string ResolveStageName(int stageId)
    {
        var entries = DataCtrl.Instance.GetAllStageEntries();
        foreach (var entry in entries)
        {
            if (entry != null && entry.StageId == stageId)
            {
                return string.IsNullOrEmpty(entry.DisplayName) ? $"Stage {stageId}" : entry.DisplayName;
            }
        }
        return string.Empty;
    }

    private void UpdateDiagnostics()
    {
        if (_diagnosticText == null)
        {
            return;
        }

        _diagnosticText.text = DataCtrl.Instance.GetDiagnostics();
    }

    private void UpdateEnterButtonState(bool enabled)
    {
        if (_enterGameButton != null)
        {
            _enterGameButton.interactable = enabled;
        }
    }
    #endregion
}
