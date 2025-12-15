using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;

public class UI_DebugPanel : UIBase
{
    #region Inspector
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private float _refreshInterval = 0.5f;

    [Header("Debug Controls")]
    [SerializeField] private UnityEngine.UI.Button _btnSpawnEnemy;
    [SerializeField] private UnityEngine.UI.Button _btnResetHero;
    [SerializeField] private UnityEngine.UI.Button _btnSpawnEnemyStatic;
    [Header("Buff Selection")]
    [SerializeField] private TMP_Dropdown _buffDropdown;
    [SerializeField] private UnityEngine.UI.Button _btnApplyBuffHero;
    [SerializeField] private UnityEngine.UI.Button _btnApplyBuffNpcs;
    [SerializeField] private TMP_Dropdown _npcDropdown;
    [SerializeField] private Button _btnAllyBuffToAllNpcs;
    #endregion

    #region Fields
    private float _timer;
    private StringBuilder _sb = new StringBuilder();
    private readonly System.Collections.Generic.List<BuffType> _buffOptions = new System.Collections.Generic.List<BuffType>();
    private BuffType _selectedBuff = BuffType.None;
    private readonly System.Collections.Generic.List<MPNpcSoulActor> _npcOptions = new System.Collections.Generic.List<MPNpcSoulActor>();
    private int _selectedNpcIndex = -1;
    private int _lastNpcCount = 0;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        
        if (_btnSpawnEnemy != null) _btnSpawnEnemy.onClick.AddListener(() => MPRoomManager.Inst?.DebugSpawnEnemy(101, 1, false));
        if (_btnSpawnEnemyStatic != null) _btnSpawnEnemyStatic.onClick.AddListener(() => MPRoomManager.Inst?.DebugSpawnEnemy(101, 1, true));
        if (_btnResetHero != null) _btnResetHero.onClick.AddListener(() => MPRoomManager.Inst?.DebugResetHero());
        if (_btnApplyBuffHero != null) _btnApplyBuffHero.onClick.AddListener(ApplySelectedBuffHero);
        if (_btnApplyBuffNpcs != null) _btnApplyBuffNpcs.onClick.AddListener(ApplySelectedBuffNpcs);
        if (_btnAllyBuffToAllNpcs != null) _btnAllyBuffToAllNpcs.onClick.AddListener(ApplySelectedBuffAllNpcs);
        PopulateBuffDropdown();
        PopulateNpcDropdown();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _refreshInterval)
        {
            _timer = 0f;
            RefreshDisplay();
        }
    }

    private void OnDestroy()
    {
        if (_btnApplyBuffHero != null) _btnApplyBuffHero.onClick.RemoveListener(ApplySelectedBuffHero);
        if (_btnApplyBuffNpcs != null) _btnApplyBuffNpcs.onClick.RemoveListener(ApplySelectedBuffNpcs);
        if (_btnSpawnEnemy != null) _btnSpawnEnemy.onClick.RemoveAllListeners();
        if (_btnSpawnEnemyStatic != null) _btnSpawnEnemyStatic.onClick.RemoveAllListeners();
        if (_btnBuffHeroSpeed != null) _btnBuffHeroSpeed.onClick.RemoveAllListeners();
        if (_btnBuffHeroAtk != null) _btnBuffHeroAtk.onClick.RemoveAllListeners();
        if (_btnBuffNpcSlow != null) _btnBuffNpcSlow.onClick.RemoveAllListeners();
        if (_btnBuffNpcStun != null) _btnBuffNpcStun.onClick.RemoveAllListeners();
        if (_btnResetHero != null) _btnResetHero.onClick.RemoveAllListeners();

        if (_buffDropdown != null)
        {
            _buffDropdown.onValueChanged.RemoveAllListeners();
        }

        if (_npcDropdown != null)
        {
            _npcDropdown.onValueChanged.RemoveAllListeners();
        }

        _npcOptions.Clear();
        _buffOptions.Clear();
    }
    #endregion

    #region Private Methods
    private void RefreshDisplay()
    {
        if (_statsText == null) return;

        _sb.Clear();
        _sb.AppendLine("=== Runtime Debug ===");

        // 1. Level Info
        if (MPRoomManager.Inst != null)
        {
            _sb.AppendLine($"State: {MPRoomManager.Inst.State}");
            _sb.AppendLine($"Level Time: {MPRoomManager.Inst.GetCurrentTime():F1}s");
            _sb.AppendLine($"Enemies Alive: {MPRoomManager.Inst.AliveEnemyCount}");
        }
        else
        {
            _sb.AppendLine("[MPRoomManager not found]");
        }

        _sb.AppendLine();

        // 2. Player Info
        var player = FindObjectOfType<MPSoulActor>(); // Simple lookup for demo
        if (player != null)
        {
            _sb.AppendLine($"Player: {player.name}");
            _sb.AppendLine($"HP: {player.CurrentHpValue} / {player.MaxHpValue}");
            
            if (player.AttributeComponent != null)
            {
                _sb.AppendLine($"Speed: {player.AttributeComponent.GetValue(AttributeType.MoveSpeed):F1}");
                _sb.AppendLine($"Attack: {player.AttributeComponent.GetValue(AttributeType.AttackPower):F0}");
            }

            _sb.AppendLine("--- Buffs ---");
            if (player.BuffLayerMgr != null && player.BuffLayerMgr.ActiveBuffs != null)
            {
                foreach (var kvp in player.BuffLayerMgr.ActiveBuffs)
                {
                    var buff = kvp.Value;
                    _sb.AppendLine($"- {buff.Config.BuffName}: {buff.Stacks} stack(s) ({buff.RemainingTime:F1}s left)");
                }
            }
            else
            {
                _sb.AppendLine("(No Active Buffs)");
            }
        }
        else
        {
            _sb.AppendLine("[No Local Player Found]");
        }

        _sb.AppendLine();
        // 3. NPC Info (all)
        var npcs = FindObjectsOfType<MPNpcSoulActor>();
        if (npcs != null && npcs.Length > 0)
        {
            foreach (var npc in npcs)
            {
                if (npc == null) continue;
                _sb.AppendLine($"NPC [{npc.GetUniqueId()}]: {npc.name}");
                _sb.AppendLine($"HP: {npc.CurrentHpValue} / {npc.MaxHpValue}");
                if (npc.AttributeComponent != null)
                {
                    _sb.AppendLine($"Speed: {npc.AttributeComponent.GetValue(AttributeType.MoveSpeed):F1}");
                    _sb.AppendLine($"Attack: {npc.AttributeComponent.GetValue(AttributeType.AttackPower):F0}");
                }

                _sb.AppendLine("--- Buffs ---");
                if (npc.BuffLayerMgr != null && npc.BuffLayerMgr.ActiveBuffs != null && npc.BuffLayerMgr.ActiveBuffs.Count > 0)
                {
                    foreach (var kv in npc.BuffLayerMgr.ActiveBuffs)
                    {
                        var buff = kv.Value;
                        _sb.AppendLine($"- {buff.Config.BuffName}: {buff.Stacks} stack(s) ({buff.RemainingTime:F1}s left)");
                    }
                }
                else
                {
                    _sb.AppendLine("(No Active Buffs)");
                }

                _sb.AppendLine();
            }
        }
        else
        {
            _sb.AppendLine("[No NPC Found]");
        }

        _statsText.text = _sb.ToString();
        PopulateNpcDropdown(); // keep NPC list fresh
    }

    private void PopulateBuffDropdown()
    {
        if (_buffDropdown == null)
        {
            return;
        }

        _buffDropdown.ClearOptions();
        _buffOptions.Clear();

        var lookup = DataCtrl.Instance.GetBuffConfigLookup();
        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
        foreach (var kv in lookup)
        {
            if (kv.Value == null) continue;
            _buffOptions.Add(kv.Key);
            var label = string.IsNullOrEmpty(kv.Value.BuffName) ? kv.Key.ToString() : $"{kv.Key} - {kv.Value.BuffName}";
            options.Add(new TMP_Dropdown.OptionData(label));
        }

        if (options.Count == 0)
        {
            _buffDropdown.AddOptions(new System.Collections.Generic.List<string> { "No Buffs" });
            _selectedBuff = BuffType.None;
            return;
        }

        _buffDropdown.AddOptions(options);
        _buffDropdown.onValueChanged.RemoveAllListeners();
        _buffDropdown.onValueChanged.AddListener(idx =>
        {
            if (idx >= 0 && idx < _buffOptions.Count)
            {
                _selectedBuff = _buffOptions[idx];
            }
        });

        _selectedBuff = _buffOptions[0];
    }

    private void ApplySelectedBuffHero()
    {
        if (_selectedBuff == BuffType.None) return;
        MPRoomManager.Inst?.DebugBuffHero(_selectedBuff);
    }

    private void ApplySelectedBuffNpcs()
    {
        if (_selectedBuff == BuffType.None) return;

        // If a specific NPC is selected, only apply to that one; otherwise apply to all.
        if (_npcDropdown != null && _selectedNpcIndex >= 0 && _selectedNpcIndex < _npcOptions.Count)
        {
            var npc = _npcOptions[_selectedNpcIndex];
            if (npc != null && !npc.IsDead)
            {
                npc.TryAddBuffStack(_selectedBuff);
            }
        }
        else
        {
            MPRoomManager.Inst?.DebugBuffAllNpcs(_selectedBuff);
        }
    }

    public void ApplySelectedBuffAllNpcs()
    {
        if (_selectedBuff == BuffType.None) return;
        MPRoomManager.Inst?.DebugBuffAllNpcs(_selectedBuff);
    }

    private void PopulateNpcDropdown()
    {
        if (_npcDropdown == null)
        {
            return;
        }

        var npcs = FindObjectsOfType<MPNpcSoulActor>();
        var aliveNpcs = new System.Collections.Generic.List<MPNpcSoulActor>();
        foreach (var npc in npcs)
        {
            if (npc == null || npc.IsDead) continue;
            aliveNpcs.Add(npc);
        }

        // Only refresh options if count changed (avoid jumping selection)
        if (aliveNpcs.Count != _lastNpcCount)
        {
            _npcDropdown.onValueChanged.RemoveAllListeners();
            _npcDropdown.ClearOptions();
            _npcOptions.Clear();

            if (aliveNpcs.Count == 0)
            {
                _npcDropdown.AddOptions(new System.Collections.Generic.List<string> { "All NPCs" });
                _selectedNpcIndex = -1;
            }
            else
            {
                var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
                foreach (var npc in aliveNpcs)
                {
                    _npcOptions.Add(npc);
                    var label = string.IsNullOrEmpty(npc.GetUniqueId()) ? npc.name : npc.GetUniqueId();
                    options.Add(new TMP_Dropdown.OptionData(label));
                }

                _npcDropdown.AddOptions(options);
                // Preserve previous selection if possible
                if (_selectedNpcIndex >= 0 && _selectedNpcIndex < _npcOptions.Count)
                {
                    _npcDropdown.value = _selectedNpcIndex;
                }
                else
                {
                    _selectedNpcIndex = 0;
                    _npcDropdown.value = 0;
                }

                _npcDropdown.onValueChanged.AddListener(idx =>
                {
                    _selectedNpcIndex = idx;
                });
            }

            _lastNpcCount = aliveNpcs.Count;
        }
    }
    #endregion
}
