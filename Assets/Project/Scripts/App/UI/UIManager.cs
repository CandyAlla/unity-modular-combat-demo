using UnityEngine;

// UIManager is a simplified global UI entry that can open key UIs like battle settlement.
public class UIManager : MonoBehaviour
{
    #region Inspector
    [SerializeField] private UI_BattleSettlement _battleSettlementUI;
    #endregion

    #region Properties
    public static UIManager Inst { get; private set; }
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
    }
    #endregion

    #region Public Methods
    public void OpenBattleSettlement(bool isWin, float durationSeconds)
    {
        if (_battleSettlementUI == null)
        {
            _battleSettlementUI = FindObjectOfType<UI_BattleSettlement>(true);
            if (_battleSettlementUI == null)
            {
                Debug.LogWarning("[UIManager] UI_BattleSettlement not found.");
                return;
            }
        }

        _battleSettlementUI.SetTime(durationSeconds);
        _battleSettlementUI.Show(isWin);
        _battleSettlementUI.Open();
    }
    #endregion
}
