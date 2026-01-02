using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a queue of floating text requests to prevent visual clutter and performance spikes
/// by limiting the number of spawns per frame.
/// </summary>
public class SoulFloatTextManager : MonoBehaviour
{
    private struct FloatTextRequest
    {
        public int Value;
        public FloatTextType Type;
        public Vector3 Position;
        public string CustomText;
    }

    #region Fields

    public static SoulFloatTextManager Instance { get; private set; }

    [Header("Settings")] [SerializeField] private int _maxSpawnPerFrame = 5;

    private readonly Queue<FloatTextRequest> _requestQueue = new Queue<FloatTextRequest>();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        ProcessQueue();
    }

    #endregion

    #region Public API

    public void EnqueueRequest(int value, FloatTextType type, Vector3 position, string customText = null)
    {
        _requestQueue.Enqueue(new FloatTextRequest
        {
            Value = value,
            Type = type,
            Position = position,
            CustomText = customText
        });
    }

    #endregion

    #region Private Methods

    private void ProcessQueue()
    {
        if (_requestQueue.Count == 0) return;

        int count = 0;
        while (_requestQueue.Count > 0 && count < _maxSpawnPerFrame)
        {
            var req = _requestQueue.Dequeue();
            SpawnFloatingText(req);
            count++;
        }
    }

    private void SpawnFloatingText(FloatTextRequest req)
    {
        if (PoolManager.Inst == null) return;

        var textObj =
            PoolManager.SpawnItemFromPool<SoulFloatingText>(PoolKey.UI_FloatText, req.Position, Quaternion.identity);
        if (textObj != null)
        {
            var defaults = FloatTextConfigProvider.GetDefaults(req.Type);
            var info = new FloatTextInfo
            {
                Value = req.Value,
                Type = req.Type,
                CustomText = req.CustomText,
                Color = defaults.Color,
                Duration = defaults.Duration,
                MoveSpeed = defaults.MoveSpeed
            };
            textObj.Init(info);
        }
    }

    #endregion
}