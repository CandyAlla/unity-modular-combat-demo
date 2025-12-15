using System.Collections.Generic;
using UnityEngine;

// DataCtrl loads ScriptableObject chapter configs and exposes runtime snapshots.
// It is a lightweight singleton cached in memory for stage lookups at battle entry.
public class DataCtrl
{
    #region Types
    public class StageEntry
    {
        public int StageId;
        public string DisplayName;
        public int Duration;
    }
    #endregion

    #region Fields
    private static DataCtrl _instance;
    public static DataCtrl Instance => _instance ??= new DataCtrl();

    private readonly Dictionary<int, MainChapterInfo> _dicStageInfos = new Dictionary<int, MainChapterInfo>();
    private readonly Dictionary<int, GameObject> _npcPrefabLookup = new Dictionary<int, GameObject>();
    private readonly List<StageEntry> _stageEntries = new List<StageEntry>();
    private readonly Dictionary<int, NpcAttributesConfig.NpcAttributesEntry> _npcAttributesLookup = new Dictionary<int, NpcAttributesConfig.NpcAttributesEntry>();
    private readonly Dictionary<int, HeroAttributesConfig.HeroAttributesEntry> _heroAttributesLookup = new Dictionary<int, HeroAttributesConfig.HeroAttributesEntry>();
    private readonly Dictionary<BuffType, BuffConfig.BuffEntry> _buffConfigLookup = new Dictionary<BuffType, BuffConfig.BuffEntry>();
    private bool _initialized;

    private DataCtrl() { }
    #endregion

    #region Public Methods
    public void InitAllChapterInfos()
    {
        if (_initialized)
        {
            return;
        }

        _dicStageInfos.Clear();
        _npcPrefabLookup.Clear();
        _stageEntries.Clear();
        _npcAttributesLookup.Clear();
        _heroAttributesLookup.Clear();
        _buffConfigLookup.Clear();
        LoadNpcAttributesConfig();
        LoadHeroAttributesConfig();
        LoadBuffConfig();
        LoadNpcAttributesConfig();
        LoadHeroAttributesConfig();
        LoadBuffConfig();
        var configs = Resources.LoadAll<MainChapterConfig>(GameConsts.PATH_CONFIG_MAIN_CHAPTER);
        System.Array.Sort(configs, (a, b) => string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));

        foreach (var config in configs)
        {
            if (config == null)
            {
                continue;
            }

            if (config.StageId <= 0)
            {
                Debug.LogWarning($"[DataCtrl] Skip config {config.name}: invalid StageId {config.StageId}");
                continue;
            }

            if (_dicStageInfos.ContainsKey(config.StageId))
            {
                Debug.LogWarning($"[DataCtrl] Duplicate StageId {config.StageId} from config {config.name}, skipping.");
                continue;
            }

            var waves = config.Waves ?? new List<NPCSpawnData>();
            var sanitizedWaves = new List<NPCSpawnData>();
            foreach (var wave in waves)
            {
                if (wave == null)
                {
                    continue;
                }

                sanitizedWaves.Add(wave);
            }

            var info = new MainChapterInfo
            {
                StageId = config.StageId,
                Duration = config.Duration,
                Monsters = sanitizedWaves
            };

            _dicStageInfos[info.StageId] = info;
            _stageEntries.Add(new StageEntry
            {
                StageId = info.StageId,
                DisplayName = config.name,
                Duration = info.Duration
            });
            Debug.Log($"[DataCtrl] Loaded chapter config for stage {info.StageId} with {info.Monsters.Count} waves.");
        }

        _initialized = true;
    }

    public void ReloadConfigs()
    {
        _initialized = false;
        InitAllChapterInfos();
    }

    public MainChapterInfo GetStageInfo(int stageId)
    {
        if (!_initialized)
        {
            InitAllChapterInfos();
        }

        if (_dicStageInfos.TryGetValue(stageId, out var info))
        {
            return info;
        }

        Debug.LogWarning($"[DataCtrl] Stage info not found for id: {stageId}");
        return null;
    }

    public GameObject GetNpcPrefab(int npcId)
    {
        return _npcPrefabLookup.TryGetValue(npcId, out var prefab) ? prefab : null;
    }

    public Dictionary<int, GameObject> GetNpcPrefabsSnapshot()
    {
        return new Dictionary<int, GameObject>(_npcPrefabLookup);
    }

    public List<StageEntry> GetAllStageEntries()
    {
        if (!_initialized)
        {
            InitAllChapterInfos();
        }

        return new List<StageEntry>(_stageEntries);
    }

    public string GetDiagnostics()
    {
        var stageCount = _dicStageInfos.Count;
        var prefabCount = _npcPrefabLookup.Count;
        var npcAttrCount = _npcAttributesLookup.Count;
        var heroAttrCount = _heroAttributesLookup.Count;
        var buffCount = _buffConfigLookup.Count;
        return $"Stages: {stageCount}, NPC prefabs: {prefabCount}, NPC attrs: {npcAttrCount}, Hero attrs: {heroAttrCount}, Buffs: {buffCount}";
    }

    public NpcAttributesConfig.NpcAttributesEntry GetNpcAttributes(int npcId)
    {
        _npcAttributesLookup.TryGetValue(npcId, out var attr);
        return attr;
    }

    public HeroAttributesConfig.HeroAttributesEntry GetHeroAttributes(int heroId = 1)
    {
        if (_heroAttributesLookup.TryGetValue(heroId, out var entry))
        {
            return entry;
        }

        // fallback to first available entry
        foreach (var kv in _heroAttributesLookup)
        {
            return kv.Value;
        }

        return null;
    }

    public Dictionary<BuffType, BuffConfig.BuffEntry> GetBuffConfigLookup()
    {
        return new Dictionary<BuffType, BuffConfig.BuffEntry>(_buffConfigLookup);
    }

    public BuffConfig.BuffEntry GetBuffConfig(BuffType type)
    {
        _buffConfigLookup.TryGetValue(type, out var entry);
        return entry;
    }
    #endregion

    #region Private Methods
    private void LoadNpcAttributesConfig()
    {
        var cfg = Resources.Load<NpcAttributesConfig>(GameConsts.PATH_CONFIG_NPC_ATTRIBUTES);
        if (cfg == null || cfg.Entries == null)
        {
            Debug.LogWarning("[DataCtrl] NpcAttributesConfig not found or empty.");
            return;
        }

        foreach (var entry in cfg.Entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (_npcAttributesLookup.ContainsKey(entry.NpcId))
            {
                Debug.LogWarning($"[DataCtrl] Duplicate NPC attr id {entry.NpcId}, skipping.");
                continue;
            }

            _npcAttributesLookup[entry.NpcId] = entry;
            if (entry.Prefab != null)
            {
                if (_npcPrefabLookup.ContainsKey(entry.NpcId))
                {
                    Debug.LogWarning($"[DataCtrl] Duplicate NPC prefab id {entry.NpcId}, keeping first.");
                }
                else
                {
                    _npcPrefabLookup[entry.NpcId] = entry.Prefab;
                }
            }
        }

        Debug.Log($"[DataCtrl] Loaded NPC attributes: {_npcAttributesLookup.Count}, prefabs: {_npcPrefabLookup.Count}");
    }

    private void LoadHeroAttributesConfig()
    {
        var cfg = Resources.Load<HeroAttributesConfig>(GameConsts.PATH_CONFIG_HERO_ATTRIBUTES);
        if (cfg == null || cfg.Entries == null)
        {
            Debug.LogWarning("[DataCtrl] HeroAttributesConfig not found.");
            return;
        }

        foreach (var entry in cfg.Entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (_heroAttributesLookup.ContainsKey(entry.HeroId))
            {
                Debug.LogWarning($"[DataCtrl] Duplicate hero attr id {entry.HeroId}, skipping.");
                continue;
            }

            _heroAttributesLookup[entry.HeroId] = entry;
        }

        Debug.Log($"[DataCtrl] Loaded Hero attributes: {_heroAttributesLookup.Count}");
    }

    private void LoadBuffConfig()
    {
        var cfg = Resources.Load<BuffConfig>(GameConsts.PATH_CONFIG_BUFF);
        if (cfg == null || cfg.Entries == null)
        {
            Debug.LogWarning("[DataCtrl] BuffConfig not found or Entries is null.");
            return;
        }

        foreach (var entry in cfg.Entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (_buffConfigLookup.ContainsKey(entry.Type))
            {
                Debug.LogWarning($"[DataCtrl] Duplicate buff type {entry.Type}, skipping.");
                continue;
            }

            _buffConfigLookup[entry.Type] = entry;
        }

        Debug.Log($"[DataCtrl] Loaded Buff entries: {_buffConfigLookup.Count}");
    }
    #endregion
}
