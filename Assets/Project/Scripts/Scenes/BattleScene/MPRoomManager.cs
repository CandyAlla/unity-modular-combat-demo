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
    #endregion

    #region Fields
    private RoomState _state = RoomState.NotStarted;
    private MainChapterInfo _chapterInfo;
    private readonly Dictionary<int, List<NPCSpawnData>> _secondToWaves = new Dictionary<int, List<NPCSpawnData>>();
    private float _elapsedTime;
    private int _lastProcessedSecond = -1;
    private int _durationSeconds;
    #endregion

    #region Properties
    public RoomState State => _state;
    public float ElapsedTime => _elapsedTime;
    public int DurationSeconds => _durationSeconds;
    #endregion

    #region Public Methods
    public void InitializeStage(int stageId)
    {
        _stageId = stageId;
        _chapterInfo = DataCtrl.Instance.GetStageInfo(_stageId);

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
                Debug.Log($"[MPRoomManager] Second {second}: spawn {wave.NpcCount}x NPC {wave.NpcId} (SpawnPointIndex {wave.SpawnPointIndex}, Pos {wave.SpawnPosition}).");
            }
        }
    }
    #endregion
}
