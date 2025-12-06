using System.Collections.Generic;
using UnityEngine;

// MPRoomManager drives the single-player battle timeline and wave triggers.
// It advances time while running, checks chapter config per second, and logs spawn intentions.
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
    #endregion

    #region Inspector
    [SerializeField] private int _stageId = 1;
    [SerializeField] private GameObject _dummyEnemyPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Transform _runtimeActorsRoot;
    [SerializeField] private string _enemyPoolKey = "Enemy_Dummy";
    [SerializeField] private MPCamManager _camManager;
    #endregion

    #region Fields
    private RoomState _state = RoomState.NotStarted;
    private MainChapterInfo _chapterInfo;
    private readonly Dictionary<int, List<NPCSpawnData>> _secondToWaves = new Dictionary<int, List<NPCSpawnData>>();
    private float _elapsedTime;
    private int _lastProcessedSecond = -1;
    private int _durationSeconds;
    private int _aliveEnemyCount;
    [SerializeField] private MPSoulActor _localPlayer;
    #endregion

    #region Properties
    public RoomState State => _state;
    public float ElapsedTime => _elapsedTime;
    public int DurationSeconds => _durationSeconds;
    public int AliveEnemyCount => _aliveEnemyCount;
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

        PoolManager.InitPoolItem<MPNpcSoulActor>(_enemyPoolKey, _dummyEnemyPrefab, 0);

        if (_chapterInfo == null)
        {
            Debug.LogError($"[MPRoomManager] No chapter info found for stage {_stageId}");
            return;
        }

        _durationSeconds = Mathf.Max(0, _chapterInfo.Duration);
        _secondToWaves.Clear();
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
            }
        }

        _state = RoomState.NotStarted;
        _elapsedTime = 0f;
        _lastProcessedSecond = -1;
        _aliveEnemyCount = 0;

        if (_localPlayer == null)
        {
            _localPlayer = FindObjectOfType<MPSoulActor>();
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

        Debug.Log($"[MPRoomManager] Initialized stage {_stageId} with duration {_durationSeconds}s and {_secondToWaves.Count} wave times.");
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
        _elapsedTime = 0f;
        _lastProcessedSecond = -1;
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
        Debug.Log("[MPRoomManager] Battle resumed.");
    }

    public void EndBattle()
    {
        if (_state == RoomState.Finished)
        {
            return;
        }

        _state = RoomState.Finished;
        Debug.Log($"[MPRoomManager] Battle finished at {_elapsedTime:F1}s.");
    }
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        if (_state != RoomState.Running)
        {
            return;
        }

        _elapsedTime += Time.deltaTime;
        var currentSecond = Mathf.FloorToInt(_elapsedTime);

        for (var sec = _lastProcessedSecond + 1; sec <= currentSecond; sec++)
        {
            if (sec > _durationSeconds)
            {
                EndBattle();
                break;
            }

            OnSecondTick(sec);
            _lastProcessedSecond = sec;
        }

        if (_elapsedTime >= _durationSeconds && _state == RoomState.Running)
        {
            EndBattle();
        }
    }
    #endregion

    #region Private Methods
    private void OnSecondTick(int second)
    {
        Debug.Log($"[MPRoomManager] Second {second} reached.");

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
        Debug.Log($"[MPRoomManager] Second {second}: spawn {wave.NpcCount}x NPC {wave.NpcId} (SpawnPointIndex {wave.SpawnPointIndex}, BasePos {basePos}). Prefab={_dummyEnemyPrefab?.name ?? "None"}");

        SpawnWave(wave, basePos);
    }

    private void SpawnWave(NPCSpawnData wave, Vector3 basePos)
    {
        if (_dummyEnemyPrefab == null || wave == null)
        {
            Debug.LogWarning("[MPRoomManager] SpawnWave skipped: missing prefab or wave data.");
            return;
        }

        var spawnCount = Mathf.Max(0, wave.NpcCount);

        for (var i = 0; i < spawnCount; i++)
        {
            var position = ResolveSpawnPositionForSpawn(basePos, wave, i);
            SpawnEnemy(position);
        }
    }

    private void RegisterSpawnedEnemy()
    {
        _aliveEnemyCount += 1;
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
        Debug.Log("[MPRoomManager] Enemy dead reported.");
    }

    public void OnPlayerDead()
    {
        if (_state != RoomState.Running)
        {
            return;
        }

        Debug.Log("[MPRoomManager] Player dead, ending battle (fail).");
        EndBattle();
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

        var radius = Random.Range(0.5f, 1f);
        var angle = Random.Range(0f, Mathf.PI * 2f);
        var offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

        return position + offset;
    }

    private void SpawnEnemy(Vector3 position)
    {
        GameObject enemy = null;
        if (PoolManager.Inst != null)
        {
            var parent = PoolManager.Inst.RuntimeActorsRoot != null ? PoolManager.Inst.RuntimeActorsRoot : _runtimeActorsRoot;
            enemy = PoolManager.SpawnItemFromPool<MPNpcSoulActor>(_enemyPoolKey, position, Quaternion.identity)?.gameObject;
            if (enemy != null && parent != null && enemy.transform.parent != parent)
            {
                enemy.transform.SetParent(parent, true);
            }
        }
        else
        {
            if (_dummyEnemyPrefab == null)
            {
                Debug.LogWarning("[MPRoomManager] No enemy prefab assigned and no PoolManager available.");
                return;
            }

            var parent = PoolManager.Inst != null ? PoolManager.Inst.RuntimeActorsRoot : _runtimeActorsRoot;
            enemy = Instantiate(_dummyEnemyPrefab, position, Quaternion.identity, parent);
        }

        if (enemy == null)
        {
            Debug.LogWarning("[MPRoomManager] Failed to spawn enemy.");
            return;
        }

        if (_runtimeActorsRoot != null && enemy.transform.parent != _runtimeActorsRoot)
        {
            enemy.transform.SetParent(_runtimeActorsRoot, true);
        }

        var npc = enemy.GetComponent<MPNpcSoulActor>();
        if (npc != null)
        {
            npc.Init(this, _localPlayer);

            var agent = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(position);
                agent.enabled = true;
            }

            npc.transform.position = position;
            OnEnemySpawned(npc);
        }
        else
        {
            Debug.LogWarning("[MPRoomManager] Spawned enemy missing MPNpcSoulActor component.");
        }
    }
    #endregion
}
