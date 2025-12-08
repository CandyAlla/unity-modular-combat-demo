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
        LoadNpcPrefabConfigs();
        var configs = Resources.LoadAll<MainChapterConfig>(string.Empty);
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
    #endregion

    #region Private Methods
    private void LoadNpcPrefabConfigs()
    {
        var prefabConfigs = Resources.LoadAll<NpcPrefabConfig>(string.Empty);
        System.Array.Sort(prefabConfigs, (a, b) => string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        foreach (var cfg in prefabConfigs)
        {
            if (cfg == null || cfg.Entries == null)
            {
                continue;
            }

            foreach (var entry in cfg.Entries)
            {
                if (entry != null && entry.Prefab != null)
                {
                    if (_npcPrefabLookup.ContainsKey(entry.NpcId))
                    {
                        Debug.LogWarning($"[DataCtrl] Duplicate npcId {entry.NpcId} in prefab configs; keeping first entry, skipping {cfg.name}.");
                        continue;
                    }

                    _npcPrefabLookup[entry.NpcId] = entry.Prefab;
                }
                else
                {
                    Debug.LogWarning("[DataCtrl] Prefab config entry missing prefab or null entry, skipped.");
                }
            }
        }

        Debug.Log($"[DataCtrl] Loaded NPC prefab mappings: {_npcPrefabLookup.Count}");
    }
    #endregion
}
