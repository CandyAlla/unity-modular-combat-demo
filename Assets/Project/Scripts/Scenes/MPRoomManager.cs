using System;
using System.Collections.Generic;
using UnityEngine;

// MPRoomManager drives the single-player battle timeline, spawning, and centralized ticking.
public class MPRoomManager : MonoBehaviour
{
    #region Types
    public enum RoomState
    {
        NotStarted,
        Running,
        Paused,
        Finished
    }

    private enum LevelStatus
    {
        Idle,
        Running,
        Paused,
        Over
    }
    #endregion

    #region Inspector
    [SerializeField] private int _stageId = 1;
    [SerializeField] private GameObject _dummyEnemyPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Transform _runtimeActorsRoot;
    [SerializeField] private string _enemyPoolKey = "Enemy_Dummy";
    [SerializeField] private MPCamManager _camManager;
    [Header("Fallback Prefabs")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _floatTextPrefab;
    [SerializeField] private GameObject _playerHudPrefab;
    [SerializeField] private Transform _hudCanvasRoot;
    [SerializeField] private bool _disableWaveSpawningInThisScene = false;
    [SerializeField] private MPSoulActor _localPlayer;
    #endregion

    #region Fields
    private RoomState _state = RoomState.NotStarted;
    private MainChapterInfo _chapterInfo;
    private readonly Dictionary<int, List<NPCSpawnData>> _secondToWaves = new Dictionary<int, List<NPCSpawnData>>();
    private readonly Dictionary<int, GameObject> _npcPrefabLookup = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, string> _npcPoolKeyLookup = new Dictionary<int, string>();
    private int _aliveEnemyCount;
    private bool _isPaused;
    private readonly List<MPCharacterSoulActorBase> _actors = new List<MPCharacterSoulActorBase>();
    private readonly List<MPNpcSoulActor> _npcs = new List<MPNpcSoulActor>();
    private readonly Dictionary<MPCharacterSoulActorBase, PlayerHUD> _actorHuds = new Dictionary<MPCharacterSoulActorBase, PlayerHUD>();
    private int _npcInstanceCounter = 0;
    private Vector3 _playerSpawnPosition;
    private Quaternion _playerSpawnRotation;
    [Header("Level Timing")]
    private LevelStatus _levelStatus = LevelStatus.Idle;
    private bool _isWin;
    private float _currentTime;
    private int _currentSecond;
    private int _lastSecond = -1;
    
    #endregion

    #region Properties
    public static MPRoomManager Inst { get; private set; }
    public RoomState State => _state;
    public int AliveEnemyCount => _aliveEnemyCount;
    public bool IsPaused => _isPaused;
    public bool IsLevelRunning => _levelStatus == LevelStatus.Running;
    public bool IsLevelOver => _levelStatus == LevelStatus.Over;
    public System.Action<int> OnLevelSecondTick;
    public MPSoulActor LocalPlayer => _localPlayer;

    #region Debug API
    public void DebugSpawnEnemy(int npcId, int count, bool disableMovement = false)
    {
        var prefab = ResolveNpcPrefab(npcId);
        var poolKey = GetPoolKeyForNpc(npcId);
        var npcAttrs = DataCtrl.Instance.GetNpcAttributes(npcId);
        
        Debug.Log($"[MPRoomManager] Debug Spawn: {count}x NPC {npcId}");
        for (int i = 0; i < count; i++)
        {
            // Spawn around player or origin
            var origin = _localPlayer != null ? _localPlayer.transform.position : Vector3.zero;
            var pos = ResolveSpawnPositionForSpawn(origin, null, i); 
            var npc = SpawnEnemy(npcId, npcAttrs, prefab, poolKey, pos);
            if (npc != null && disableMovement)
            {
                npc.SetMovementEnabled(false);
            }
        }
    }

    public void DebugBuffHero(BuffType type)
    {
        if (_localPlayer != null)
        {
            _localPlayer.TryAddBuffStack(type);
            Debug.Log($"[MPRoomManager] Debug Buff Hero: {type}");
        }
    }

    public void DebugBuffAllNpcs(BuffType type)
    {
        foreach (var npc in _npcs)
        {
            if (npc != null && !npc.IsDead)
            {
                npc.TryAddBuffStack(type);
            }
        }
        Debug.Log($"[MPRoomManager] Debug Buff All NPCs: {type}");
    }

    public void DebugResetHero()
    {
        ResetPlayerForRestart();
        Debug.Log("[MPRoomManager] Debug Reset Hero");
    }
    #endregion
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

        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("[MPRoomManager] No spawn points assigned; will fall back to origin.");
        }
    }

    private void OnDestroy()
    {
        if (Inst == this)
        {
            Inst = null;
        }
    }

    private void OnEnable()
    {
        EventBus.OnAttach<PlayerDeadEvent>(OnPlayerDeadEvent);
        EventBus.OnAttach<EnemyDeadEvent>(OnEnemyDeadEvent);
    }

    private void OnDisable()
    {
        EventBus.OnDetach<PlayerDeadEvent>(OnPlayerDeadEvent);
        EventBus.OnDetach<EnemyDeadEvent>(OnEnemyDeadEvent);
    }
    #endregion

    #region Public Methods
    public void InitializeStage(int stageId)
    {
        _stageId = stageId;
        _chapterInfo = DataCtrl.Instance.GetStageInfo(_stageId);
        if (_runtimeActorsRoot == null && PoolManager.Inst != null)
        {
            _runtimeActorsRoot = PoolManager.Inst.RuntimeActorsRoot;
        }

        _npcPrefabLookup.Clear();
        _npcPoolKeyLookup.Clear();

        var globalPrefabs = DataCtrl.Instance.GetNpcPrefabsSnapshot();
        foreach (var kvp in globalPrefabs)
        {
            if (kvp.Value != null)
            {
                _npcPrefabLookup[kvp.Key] = kvp.Value;
            }
        }

        if (_chapterInfo == null)
        {
            Debug.LogError($"[MPRoomManager] No chapter info found for stage {_stageId}");
            return;
        }

        _secondToWaves.Clear();
        var npcIds = new HashSet<int>();
        if (_chapterInfo.Monsters != null)
        {
            foreach (var wave in _chapterInfo.Monsters)
            {
                if (!_secondToWaves.TryGetValue(wave.Time, out var list))
                {
                    list = new List<NPCSpawnData>();
                    _secondToWaves[wave.Time] = list;
                }
                list.Add(wave);
                npcIds.Add(wave.NpcId);
            }
        }

        foreach (var npcId in npcIds)
        {
            var prefab = ResolveNpcPrefab(npcId);
            if (prefab == null && _dummyEnemyPrefab != null)
            {
                prefab = _dummyEnemyPrefab;
                _npcPrefabLookup[npcId] = prefab;
            }

            var poolKey = GetPoolKeyForNpc(npcId);
            _npcPoolKeyLookup[npcId] = poolKey;

            if (prefab != null)
            {
                var preload = Mathf.Max(0, _chapterInfo.Duration / 10);
                PoolManager.InitPoolItem<MPNpcSoulActor>(poolKey, prefab, preload);
            }
            else
            {
                Debug.LogWarning($"[MPRoomManager] No prefab found for NPC {npcId}; spawns will be skipped.");
            }
        }
        
        PoolManager.InitPoolItem<SoulFloatingText>("UI_FloatText", _floatTextPrefab, 10);
        if (_playerHudPrefab != null)
        {
            PoolManager.InitPoolItem<PlayerHUD>("UI_PlayerHUD", _playerHudPrefab, 10);
        }
        

        _state = RoomState.NotStarted;
        _aliveEnemyCount = 0;
        _isPaused = false;
        _levelStatus = LevelStatus.Idle;
        _isWin = false;
        _currentTime = 0f;
        _currentSecond = 0;
        _lastSecond = -1;
        _actors.Clear();
        ReleaseAllHuds();

        if (_localPlayer == null)
        {
            _localPlayer = FindObjectOfType<MPSoulActor>();
            if (_localPlayer == null)
            {
                _localPlayer = SpawnFallbackPlayer();
                if (_localPlayer == null)
                {
                    Debug.LogError("[MPRoomManager] Local player not found. Spawning logic will not proceed.");
                }
            }
        }

        if (_localPlayer != null)
        {
            RegisterActor(_localPlayer);
            _playerSpawnPosition = _localPlayer.transform.position;
            _playerSpawnRotation = _localPlayer.transform.rotation;
        }

        if (_camManager == null)
        {
            _camManager = MPCamManager.Inst != null ? MPCamManager.Inst : FindObjectOfType<MPCamManager>();
        }

        if (_camManager != null && _localPlayer != null)
        {
            _camManager.OnInitCam(_localPlayer);
            _localPlayer.OnSetMPCamMgr(_camManager);
        }
        else if (_camManager == null)
        {
            Debug.LogWarning("[MPRoomManager] MPCamManager not found during initialization.");
        }

        var durationLog = _chapterInfo.Duration;
        Debug.Log($"[MPRoomManager] Initialized stage {_stageId} with duration {durationLog}s and {_secondToWaves.Count} wave times.");
    }

    public void StartBattle()
    {
        if (_chapterInfo == null)
        {
            Debug.LogWarning("[MPRoomManager] StartBattle called before InitializeStage.");
            return;
        }

        if (_state == RoomState.Finished)
        {
            Debug.LogWarning("[MPRoomManager] StartBattle ignored: battle already finished.");
            return;
        }

        _state = RoomState.Running;
        _isPaused = false;
        _levelStatus = LevelStatus.Running;
        BeginTimeCounting();
        Debug.Log("[MPRoomManager] Battle started.");
    }

    public void PauseBattle()
    {
        if (_state != RoomState.Running)
        {
            Debug.LogWarning($"[MPRoomManager] PauseBattle ignored in state {_state}.");
            return;
        }

        _state = RoomState.Paused;
        _levelStatus = LevelStatus.Paused;
        TogglePause();
        Debug.Log("[MPRoomManager] Battle paused.");
    }

    public void ResumeBattle()
    {
        if (_state != RoomState.Paused)
        {
            Debug.LogWarning($"[MPRoomManager] ResumeBattle ignored in state {_state}.");
            return;
        }

        _state = RoomState.Running;
        _levelStatus = LevelStatus.Running;
        TogglePause();
        Debug.Log("[MPRoomManager] Battle resumed.");
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        Debug.Log($"[MPRoomManager] Pause toggled: {_isPaused}");

        SetNpcsPaused(_isPaused);
        BulletActorLite.SetPausedAll(_isPaused);

        foreach (var actor in _actors)
        {
            actor?.SetHurtPaused(_isPaused);
        }
    }

    public void RegisterEnemyDestroyed()
    {
        _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);
    }

    public void OnSetMPCamManager(MPCamManager camMgr)
    {
        _camManager = camMgr;
    }

    public void OnEnemySpawned(MPNpcSoulActor enemy)
    {
        _aliveEnemyCount++;
    }

    public void OnEnemyDead(MPNpcSoulActor actor)
    {
        RegisterEnemyDestroyed();
        UnregisterNpc(actor);
        UnregisterActor(actor);
        Debug.Log("[MPRoomManager] Enemy dead reported.");
    }

    private void OnEnemyDeadEvent(EnemyDeadEvent evt)
    {
        OnEnemyDead(evt.Actor);
    }

    public void OnPlayerDead()
    {
        if (_state != RoomState.Running)
        {
            return;
        }

        Debug.Log("[MPRoomManager] Player dead, ending battle (fail).");
        EndLevel(false);
    }

    private void OnPlayerDeadEvent(PlayerDeadEvent evt)
    {
        OnPlayerDead();
    }

    public float GetCurrentTime() => _currentTime;

    public float GetCurrentStageDuration()
    {
        if (_disableWaveSpawningInThisScene)
        {
            return float.PositiveInfinity;
        }
        return _chapterInfo != null ? _chapterInfo.Duration : 0f;
    }

    public void RegisterActor(MPCharacterSoulActorBase actor)
    {
        if (actor == null || _actors.Contains(actor))
        {
            return;
        }

        _actors.Add(actor);
        AttachHud(actor);
    }

    public void BeginTimeCounting()
    {
        _levelStatus = LevelStatus.Running;
        _isWin = false;
        _currentTime = 0f;
        _currentSecond = 0;
        _lastSecond = -1;
    }

    public void EndLevel(bool isWin)
    {
        EndTimeCounting(isWin);
    }

    private void EndTimeCounting(bool isWin)
    {
        if (_levelStatus == LevelStatus.Over)
        {
            return;
        }

        _state = RoomState.Finished;
        _levelStatus = LevelStatus.Over;
        _isWin = isWin;
        _isPaused = true;
        SetNpcsPaused(true);
        OnLevelOver(isWin);
    }

    private void OnLevelOver(bool isWin)
    {
        Debug.Log($"[MPRoomManager] Level over. Win={isWin}");

        UIManager.Inst?.OpenBattleSettlement(isWin, _currentTime);
    }

    public void RestartLevel()
    {
        if (_chapterInfo == null)
        {
            Debug.LogWarning("[MPRoomManager] RestartLevel called without initialized stage; re-initializing.");
            InitializeStage(_stageId);
            return;
        }

        ClearNpcs();
        _actors.Clear();
        _aliveEnemyCount = 0;
        BulletActorLite.ClearAll();

        ResetPlayerForRestart();

        // Reset level state flags and timers
        _isPaused = false;
        _levelStatus = LevelStatus.Running;
        _isWin = false;
        _currentTime = 0f;
        _currentSecond = 0;
        _lastSecond = -1;
        _state = RoomState.Running;

        // Rebuild wave mapping to ensure consistency with current chapter info
        _secondToWaves.Clear();
        if (_chapterInfo.Monsters != null)
        {
            foreach (var wave in _chapterInfo.Monsters)
            {
                if (wave == null)
                {
                    continue;
                }

                if (!_secondToWaves.TryGetValue(wave.Time, out var list))
                {
                    list = new List<NPCSpawnData>();
                    _secondToWaves[wave.Time] = list;
                }
                list.Add(wave);
            }
        }

        BeginTimeCounting();
        Debug.Log("[MPRoomManager] Level restarted.");
    }

    public void UnregisterActor(MPCharacterSoulActorBase actor)
    {
        if (actor == null)
        {
            return;
        }

        _actors.Remove(actor);
        ReleaseHud(actor);
    }

    private void ResetPlayerForRestart()
    {
        if (_localPlayer == null)
        {
            _localPlayer = FindObjectOfType<MPSoulActor>();
        }

        if (_localPlayer == null)
        {
            Debug.LogWarning("[MPRoomManager] No local player to reset on restart.");
            return;
        }

        if (!_actors.Contains(_localPlayer))
        {
            RegisterActor(_localPlayer);
        }

        if (!_localPlayer.gameObject.activeSelf)
        {
            _localPlayer.gameObject.SetActive(true);
        }

        _localPlayer.transform.position = _playerSpawnPosition;
        _localPlayer.transform.rotation = _playerSpawnRotation;
        _localPlayer.ResetForRestart();

        if (_camManager == null)
        {
            _camManager = MPCamManager.Inst != null ? MPCamManager.Inst : FindObjectOfType<MPCamManager>();
        }

        if (_camManager != null)
        {
            _camManager.OnInitCam(_localPlayer);
            _localPlayer.OnSetMPCamMgr(_camManager);
        }
        else
        {
            _camManager = SpawnFallbackCamera();
            if (_camManager == null)
            {
                Debug.LogWarning("[MPRoomManager] MPCamManager not found on restart.");
            }
            else
            {
                _camManager.OnInitCam(_localPlayer);
                _localPlayer.OnSetMPCamMgr(_camManager);
            }
        }
    }
    #endregion

    #region Unity Lifecycle Updates
    private void Update()
    {
        if (_state != RoomState.Running || _isPaused || _levelStatus != LevelStatus.Running)
        {
            return;
        }

        var deltaTime = Time.deltaTime;
        TickLevel(deltaTime);
        TickActors(deltaTime);
    }
    #endregion

    #region Private Methods
    private void TickLevel(float deltaTime)
    {
        _currentTime += deltaTime;
        var second = Mathf.FloorToInt(_currentTime);
        if (second != _lastSecond)
        {
            _currentSecond = second;
            _lastSecond = second;
            OnLevelSecondTickInternal(second);
        }
    }

    private void OnLevelSecondTickInternal(int second)
    {
        OnLevelSecondTick?.Invoke(second);
        OnSecondTick(second);

        if (_levelStatus == LevelStatus.Running &&  _currentTime >= GetCurrentStageDuration())
        {
            var playerAlive = _localPlayer != null && !_localPlayer.IsDead;
            if (playerAlive)
            {
                EndLevel(true);
            }
        }
    }

    private void TickActors(float deltaTime)
    {
        for (var i = _actors.Count - 1; i >= 0; i--)
        {
            var actor = _actors[i];
            if (actor == null)
            {
                _actors.RemoveAt(i);
                continue;
            }

            if (actor.IsDead)
            {
                continue;
            }

            try
            {
                actor.TickBuffs(deltaTime);
                actor.TickActor(deltaTime);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MPRoomManager] Exception ticking actor {actor.name}: {ex}");
            }
        }
    }

    private void OnSecondTick(int second)
    {
        Debug.Log($"[MPRoomManager] Second {second} reached.");

        if (_disableWaveSpawningInThisScene)
        {
            return;
        }

        if (_secondToWaves.TryGetValue(second, out var waves) && waves != null)
        {
            foreach (var wave in waves)
            {
                HandleWaveSpawn(wave, second);
            }
        }
    }

    private void HandleWaveSpawn(NPCSpawnData wave, int second)
    {
        var basePos = ResolveSpawnPosition(wave);
        var prefab = ResolveNpcPrefab(wave);
        var poolKey = ResolvePoolKey(wave);
        Debug.Log($"[MPRoomManager] Second {second}: spawn {wave.NpcCount}x NPC {wave.NpcId} (SpawnPointIndex {wave.SpawnPointIndex}, BasePos {basePos}). Prefab={prefab?.name ?? "None"} PoolKey={poolKey}");

        SpawnWave(wave, prefab, poolKey, basePos);
    }

    private void SpawnWave(NPCSpawnData wave, GameObject prefab, string poolKey, Vector3 basePos)
    {
        if (prefab == null || wave == null)
        {
            Debug.LogWarning("[MPRoomManager] SpawnWave skipped: missing prefab or wave data.");
            return;
        }

        var spawnCount = Mathf.Max(0, wave.NpcCount);
        var npcAttrs = DataCtrl.Instance.GetNpcAttributes(wave.NpcId);

        for (var i = 0; i < spawnCount; i++)
        {
            var position = ResolveSpawnPositionForSpawn(basePos, wave, i);
            SpawnEnemy(wave.NpcId, npcAttrs, prefab, poolKey, position);
        }
    }

    private Vector3 ResolveSpawnPosition(NPCSpawnData wave)
    {
        if (wave != null && wave.SpawnPosition != Vector3.zero)
        {
            return wave.SpawnPosition;
        }

        if (_spawnPoints != null &&
            wave != null &&
            wave.SpawnPointIndex >= 0 &&
            wave.SpawnPointIndex < _spawnPoints.Length &&
            _spawnPoints[wave.SpawnPointIndex] != null)
        {
            return _spawnPoints[wave.SpawnPointIndex].position;
        }

        return Vector3.zero;
    }

    private Vector3 ResolveSpawnPositionByIndex(NPCSpawnData wave, int iteration)
    {
        if (_spawnPoints != null &&
            wave != null &&
            wave.SpawnPointIndex >= 0 &&
            wave.SpawnPointIndex < _spawnPoints.Length &&
            _spawnPoints[wave.SpawnPointIndex] != null)
        {
            return _spawnPoints[wave.SpawnPointIndex].position;
        }

        // Simple fallback: cycle through available spawn points.
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            var idx = iteration % _spawnPoints.Length;
            return _spawnPoints[idx] != null ? _spawnPoints[idx].position : Vector3.zero;
        }

        return Vector3.zero;
    }

    private Vector3 ResolveSpawnPositionForSpawn(Vector3 basePos, NPCSpawnData wave, int iteration)
    {
        var position = basePos;

        if (position == Vector3.zero)
        {
            position = ResolveSpawnPositionByIndex(wave, iteration);
        }

        var radius = UnityEngine.Random.Range(0.5f, 1f);
        var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

        return position + offset;
    }

    private MPNpcSoulActor SpawnEnemy(int npcId, NpcAttributesConfig.NpcAttributesEntry attrs, GameObject prefab, string poolKey, Vector3 position)
    {
        GameObject enemy = null;
        if (PoolManager.Inst != null)
        {
            var parent = PoolManager.Inst.RuntimeActorsRoot != null ? PoolManager.Inst.RuntimeActorsRoot : _runtimeActorsRoot;
            enemy = PoolManager.SpawnItemFromPool<MPNpcSoulActor>(poolKey, position, Quaternion.identity)?.gameObject;
            if (enemy != null && parent != null && enemy.transform.parent != parent)
            {
                enemy.transform.SetParent(parent, true);
            }
        }
        else
        {
            if (prefab == null)
            {
                Debug.LogWarning("[MPRoomManager] No enemy prefab assigned and no PoolManager available.");
                return null;
            }

            var parent = PoolManager.Inst != null ? PoolManager.Inst.RuntimeActorsRoot : _runtimeActorsRoot;
            enemy = Instantiate(prefab, position, Quaternion.identity, parent);
        }

        if (enemy == null)
        {
            Debug.LogWarning("[MPRoomManager] Failed to spawn enemy.");
            return null;
        }

        if (_runtimeActorsRoot != null && enemy.transform.parent != _runtimeActorsRoot)
        {
            enemy.transform.SetParent(_runtimeActorsRoot, true);
        }

        var npc = enemy.GetComponent<MPNpcSoulActor>();
        if (npc != null)
        {
            _npcInstanceCounter++;
            npc.SetUniqueId($"NPC-{_npcInstanceCounter}");
            npc.Init(this, _localPlayer);
            npc.ApplyAttributes(attrs);
            npc.SetPoolKey(poolKey);

            var agent = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(position);
                agent.enabled = true;
            }

            npc.transform.position = position;
            OnEnemySpawned(npc);
            RegisterActor(npc);
            RegisterNpc(npc);
            return npc;
        }
        else
        {
            Debug.LogWarning("[MPRoomManager] Spawned enemy missing MPNpcSoulActor component.");
        }

        return null;
    }

    private void RegisterNpc(MPNpcSoulActor npc)
    {
        if (npc == null || _npcs.Contains(npc))
        {
            return;
        }

        _npcs.Add(npc);
    }

    private void UnregisterNpc(MPNpcSoulActor npc)
    {
        if (npc == null)
        {
            return;
        }

        _npcs.Remove(npc);
    }

    private GameObject ResolveNpcPrefab(NPCSpawnData wave)
    {
        if (wave == null)
        {
            return _dummyEnemyPrefab;
        }

        return ResolveNpcPrefab(wave.NpcId);
    }

    private GameObject ResolveNpcPrefab(int npcId)
    {
        if (_npcPrefabLookup.TryGetValue(npcId, out var prefab) && prefab != null)
        {
            return prefab;
        }

        return _dummyEnemyPrefab;
    }

    private string ResolvePoolKey(NPCSpawnData wave)
    {
        if (wave == null)
        {
            return _enemyPoolKey;
        }

        return GetPoolKeyForNpc(wave.NpcId);
    }

    private string GetPoolKeyForNpc(int npcId)
    {
        if (_npcPoolKeyLookup.TryGetValue(npcId, out var key))
        {
            return key;
        }

        var resolved = string.IsNullOrEmpty(_enemyPoolKey) ? $"Enemy_{npcId}" : $"{_enemyPoolKey}_{npcId}";
        _npcPoolKeyLookup[npcId] = resolved;
        return resolved;
    }

    private void SetNpcsPaused(bool paused)
    {
        foreach (var npc in _npcs)
        {
            if (npc != null)
            {
                npc.SetPaused(paused);
            }
        }
    }

    private void ClearNpcs()
    {
        var npcsSnapshot = new List<MPNpcSoulActor>(_npcs);
        foreach (var npc in npcsSnapshot)
        {
            if (npc != null)
            {
                ReleaseHud(npc);
                var poolKey = npc.GetPoolKey();
                PoolManager.DespawnItemToPool(string.IsNullOrEmpty(poolKey) ? _enemyPoolKey : poolKey, npc);
            }
        }
        _npcs.Clear();
    }

    private MPSoulActor SpawnFallbackPlayer()
    {
        if (_playerPrefab == null)
        {
            Debug.LogError("[MPRoomManager] No player prefab assigned for fallback spawn.");
            return null;
        }

        var parent = _runtimeActorsRoot != null ? _runtimeActorsRoot : transform;
        var obj = Instantiate(_playerPrefab, _playerSpawnPosition, _playerSpawnRotation, parent);
        var actor = obj.GetComponent<MPSoulActor>();
        if (actor == null)
        {
            Debug.LogError("[MPRoomManager] Fallback player prefab missing MPSoulActor component.");
            return null;
        }

        return actor;
    }

    private void AttachHud(MPCharacterSoulActorBase actor)
    {
        if (actor == null || _actorHuds.ContainsKey(actor))
        {
            return;
        }

        PlayerHUD hud = null;
        var spawnPos = actor.transform.position;
        // HUDs must be under a Canvas, not the 3D root
        var parent = _hudCanvasRoot;

        if (parent == null)
        {
             // Fallback: try to find a canvas if none assigned
             var cv = FindObjectOfType<Canvas>();
             if (cv == null)
             {
                 var all = Resources.FindObjectsOfTypeAll<Canvas>();
                 if (all != null && all.Length > 0)
                 {
                     cv = all[0];
                 }
             }
             if (cv != null) parent = cv.transform;
        }

        if (PoolManager.Inst != null)
        {
            hud = PoolManager.SpawnItemFromPool<PlayerHUD>("UI_PlayerHUD", spawnPos, Quaternion.identity);
            if (hud != null && parent != null && hud.transform.parent != parent)
            {
                hud.transform.SetParent(parent, false); // false = keep local scale, important for UI
            }
        }
        else if (_playerHudPrefab != null)
        {
            hud = Instantiate(_playerHudPrefab, spawnPos, Quaternion.identity, parent).GetComponent<PlayerHUD>();
        }

        if (hud != null)
        {
            var cam = _camManager != null ? _camManager.MainCamera : Camera.main;
            hud.Bind(actor, actor.HudAnchor, cam);
            // Label: show unique ID for NPCs, empty for players.
            if (actor is MPNpcSoulActor npc)
            {
                hud.SetLabel(npc.GetUniqueId());
            }
            else
            {
                hud.SetLabel(string.Empty);
            }
            _actorHuds[actor] = hud;
        }
        else
        {
            Debug.LogWarning("[MPRoomManager] Failed to obtain PlayerHUD instance. Ensure canvas and pool are set.");
        }
    }

    private void ReleaseHud(MPCharacterSoulActorBase actor)
    {
        if (actor == null)
        {
            return;
        }

        if (_actorHuds.TryGetValue(actor, out var hud))
        {
            _actorHuds.Remove(actor);
            if (hud != null)
            {
                if (PoolManager.Inst != null)
                {
                    PoolManager.DespawnItemToPool("UI_PlayerHUD", hud);
                }
                else
                {
                    Destroy(hud.gameObject);
                }
            }
        }
    }

    private void ReleaseAllHuds()
    {
        var snapshot = new List<MPCharacterSoulActorBase>(_actorHuds.Keys);
        foreach (var actor in snapshot)
        {
            ReleaseHud(actor);
        }
        _actorHuds.Clear();
    }

    private MPCamManager SpawnFallbackCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }

        var mgr = cam.GetComponent<MPCamManager>();
        if (mgr == null)
        {
            mgr = cam.gameObject.AddComponent<MPCamManager>();
        }

        return mgr;
    }
    #endregion
}
