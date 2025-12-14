using UnityEngine;

// SkillRuntimeController is a lightweight runtime driver for a single skill,
// replacing the original RuntimeTreeMgr with a simple phase machine + blackboard.
public class SkillRuntimeController
{
    #region Types
    public enum SkillPhase
    {
        Idle = 0,
        Casting = 1,
        Active = 2,
        Recovery = 3
    }

    public class SkillBlackboard
    {
        public GameObject Owner;
        public Vector3 TargetPosition;
        public Vector3 Direction;
        public bool HasHit;
        public bool IsCasting;
    }
    #endregion

    #region Fields
    private SkillConfig _config;
    private SkillPhase _phase = SkillPhase.Idle;
    private float _cooldownRemaining;
    private float _phaseTimer;
    private readonly SkillBlackboard _blackboard = new SkillBlackboard();
    #endregion

    #region Properties
    public SkillConfig Config => _config;
    public SkillPhase Phase => _phase;
    public float CooldownRemaining => _cooldownRemaining;
    public SkillBlackboard Blackboard => _blackboard;
    public bool IsReady => _phase == SkillPhase.Idle && _cooldownRemaining <= 0f;
    #endregion

    #region Public Methods
    /// <summary>
    /// Initialize the controller with a skill config and its owner.
    /// </summary>
    public void Initialize(SkillConfig config, GameObject owner)
    {
        _config = config;
        ResetState();
        _blackboard.Owner = owner;
    }

    /// <summary>
    /// Attempt to start casting the skill with context (direction/target).
    /// Performs simple cooldown/phase checks and moves to Casting if allowed.
    /// </summary>
    public bool TryStartCast(Vector3 targetPosition, Vector3 direction)
    {
        if (_config == null)
        {
            Debug.LogWarning("[SkillRuntimeController] No config assigned.");
            return false;
        }

        if (!IsReady)
        {
            return false;
        }

        _blackboard.TargetPosition = targetPosition;
        _blackboard.Direction = direction.normalized;
        _blackboard.HasHit = false;
        _blackboard.IsCasting = true;

        _phase = SkillPhase.Casting;
        _phaseTimer = _config.CastTime;
        return true;
    }

    /// <summary>
    /// Tick updates cooldowns and advances phases (Casting -> Active -> Recovery -> Idle).
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);
        }

        switch (_phase)
        {
            case SkillPhase.Idle:
                return;
            case SkillPhase.Casting:
                AdvancePhase(deltaTime, SkillPhase.Active, _config.CastTime);
                break;
            case SkillPhase.Active:
                // For now, active phase is instantaneous; mark hit flag and advance.
                _blackboard.HasHit = true;
                _phaseTimer = 0f;
                _phase = SkillPhase.Recovery;
                break;
            case SkillPhase.Recovery:
                AdvancePhase(deltaTime, SkillPhase.Idle, _config.Cooldown);
                break;
        }
    }

    /// <summary>
    /// Reset the controller (e.g., on death/interrupt/level restart).
    /// </summary>
    public void ResetState()
    {
        _phase = SkillPhase.Idle;
        _cooldownRemaining = 0f;
        _phaseTimer = 0f;
        _blackboard.HasHit = false;
        _blackboard.IsCasting = false;
    }
    #endregion

    #region Private Methods
    private void AdvancePhase(float deltaTime, SkillPhase nextPhase, float duration)
    {
        _phaseTimer -= deltaTime;
        if (_phaseTimer <= 0f)
        {
            if (_phase == SkillPhase.Recovery)
            {
                _cooldownRemaining = _config.Cooldown;
            }
            _phase = nextPhase;
            if (_phase == SkillPhase.Recovery)
            {
                _phaseTimer = duration;
            }
        }
    }
    #endregion
}
