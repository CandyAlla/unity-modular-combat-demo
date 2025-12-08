using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// UIManager is a simplified global UI entry that can open key UIs like battle settlement.
public class UIManager : MonoBehaviour
{
    #region Inspector
    [SerializeField] private UI_BattleSettlement _battleSettlementUI;
    #endregion

    #region Properties
    public static UIManager Inst { get; private set; }
    #endregion

    #region Fields
    private readonly Dictionary<string, UIBase> _uiRegistry = new Dictionary<string, UIBase>();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Inst != null && Inst != this)
        {
            Destroy(gameObject);
            return;
        }

        Inst = this;
        DontDestroyOnLoad(gameObject);

        if (_battleSettlementUI == null)
        {
            _battleSettlementUI = FindObjectOfType<UI_BattleSettlement>(true);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        RebuildRegistry();
    }

    private void OnDestroy()
    {
        if (Inst == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Inst = null;
        }
    }
    #endregion

    #region Public Methods
    public void RegisterUI(string key, UIBase ui)
    {
        if (string.IsNullOrEmpty(key) || ui == null)
        {
            return;
        }

        _uiRegistry[key] = ui;
    }

    public bool TryGetUI<T>(string key, out T ui) where T : UIBase
    {
        if (_uiRegistry.TryGetValue(key, out var baseUi) && baseUi is T typed)
        {
            ui = typed;
            return true;
        }

        ui = null;
        return false;
    }

    public void OpenBattleSettlement(bool isWin, float durationSeconds)
    {
        if (_battleSettlementUI == null && !TryGetUI("BattleSettlement", out _battleSettlementUI))
        {
            _battleSettlementUI = FindObjectOfType<UI_BattleSettlement>(true);
            if (_battleSettlementUI != null)
            {
                RegisterUI("BattleSettlement", _battleSettlementUI);
            }
        }

        if (_battleSettlementUI == null)
        {
            Debug.LogWarning("[UIManager] UI_BattleSettlement not found.");
            return;
        }

        _battleSettlementUI.SetTime(durationSeconds);
        _battleSettlementUI.Show(isWin);
        _battleSettlementUI.Open();
    }
    #endregion

    #region Private Methods
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebuildRegistry();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        RebuildRegistry();
    }

    private void RebuildRegistry()
    {
        _uiRegistry.Clear();
        var uis = FindObjectsOfType<UIBase>(true);
        foreach (var ui in uis)
        {
            if (ui != null)
            {
                var key = string.IsNullOrEmpty(ui.UIKey) ? ui.name : ui.UIKey;
                RegisterUI(key, ui);
            }
        }

        if (_battleSettlementUI == null)
        {
            TryGetUI("BattleSettlement", out _battleSettlementUI);
        }
    }
    #endregion
}
