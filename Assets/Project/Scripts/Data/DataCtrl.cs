using System.Collections.Generic;
using UnityEngine;

// DataCtrl loads ScriptableObject chapter configs and exposes runtime snapshots.
// It is a lightweight singleton cached in memory for stage lookups at battle entry.
public class DataCtrl
{
    #region Fields
    private static DataCtrl _instance;
    public static DataCtrl Instance => _instance ??= new DataCtrl();

    private readonly Dictionary<int, MainChapterInfo> _dicStageInfos = new Dictionary<int, MainChapterInfo>();
    private readonly Dictionary<int, GameObject> _npcPrefabLookup = new Dictionary<int, GameObject>();
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
        LoadNpcPrefabConfigs();
        var configs = Resources.LoadAll<MainChapterConfig>(string.Empty);

        foreach (var config in configs)
        {
            if (config == null)
            {
                continue;
            }

            var info = new MainChapterInfo
            {
                StageId = config.StageId,
                Duration = config.Duration,
                Monsters = new List<NPCSpawnData>(config.Waves ?? new List<NPCSpawnData>())
            };

            _dicStageInfos[info.StageId] = info;
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
    #endregion

    #region Private Methods
    private void LoadNpcPrefabConfigs()
    {
        var prefabConfigs = Resources.LoadAll<NpcPrefabConfig>(string.Empty);
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
                    _npcPrefabLookup[entry.NpcId] = entry.Prefab;
                }
            }
        }

        Debug.Log($"[DataCtrl] Loaded NPC prefab mappings: {_npcPrefabLookup.Count}");
    }
    #endregion
}
