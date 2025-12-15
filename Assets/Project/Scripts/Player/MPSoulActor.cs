using System;
using UnityEngine;
using UnityEngine.InputSystem;

// MPSoulActor represents the local player actor with basic health and input-driven movement.
// It derives from MPCharacterSoulActorBase to share damage/death flow with NPCs.
public class MPSoulActor : MPCharacterSoulActorBase
{
    #region Inspector
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private MPRoomManager _roomManager;
    [SerializeField] private MPSkillActorLite _skillActor;
    #endregion

    #region Fields
    private GameInput _input;
    private Vector2 _moveInput;
    private bool _canControl = true;
    private MPCamManager _camManager;
    private float _attackCooldownTimer;
    #endregion

    #region Constants
    private const float ATTACK_COOLDOWN = 0.5f;
    private const float ATTACK_RANGE = 10f;
    private const int ATTACK_DAMAGE = 10;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        if (_roomManager == null)
        {
            _roomManager = FindObjectOfType<MPRoomManager>();
        }

        _input = new GameInput();
        _input.Player.SetCallbacks(new PlayerActions(this));
    }

    private void OnEnable()
    {
        base.OnEnable();
        _input?.Enable();
    }

    private void OnDisable()
    {
        _input?.Disable();
    }

    private void Update()
    {
        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -= Time.deltaTime;
        }

        // DEBUG: Press K to add Speed Buff
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("[MPSoulActor] Debug Input: Adding MoveSpeedUp Buff");
            TryAddBuffStack(BuffType.MoveSpeedUp);
        }

        _skillActor?.Tick(Time.deltaTime);
    }

    protected override void OnDestroy()
    {
        _input?.Dispose();
    }
    #endregion

    #region Private Methods
    protected override void OnInitActor()
    {
        _isPlayer = true;
        MaxHp = MaxHp <= 0 ? 100 : MaxHp;
        if (_attributeComponent != null)
        {
            _attributeComponent.SetBaseValue(AttributeType.MoveSpeed, _moveSpeed);
        }

        if (_skillActor == null)
        {
            _skillActor = GetComponent<MPSkillActorLite>();
        }

        if (_skillActor != null)
        {
            _skillActor.Initialize(gameObject);
        }
    }

    protected override void OnUpdatePlayerMovement(float deltaTime)
    {
        if (!_canControl)
        {
            return;
        }

        if (MPRoomManager.Inst != null && MPRoomManager.Inst.IsPaused)
        {
            return;
        }

        var inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

        if (inputDir.sqrMagnitude > 0.001f)
        {
            var worldDir = inputDir.normalized;

            var currentSpeed = _attributeComponent != null ? _attributeComponent.GetValue(AttributeType.MoveSpeed) : _moveSpeed;
            transform.position += worldDir * (currentSpeed * deltaTime);
            transform.rotation = Quaternion.LookRotation(worldDir, Vector3.up);
        }
    }

    protected override void OnBeforeDeath()
    {
        // Base class raises EventBus event
        _canControl = false;
        base.OnBeforeDeath();
    }

    protected override void OnAfterDeath()
    {
        gameObject.SetActive(false);
    }

    public void OnSetMPCamMgr(MPCamManager camMgr)
    {
        _camManager = camMgr;
    }

    public Camera GetMainCamera()
    {
        return _camManager != null ? _camManager.MainCamera : Camera.main;
    }

    public MPCamManager GetMPCamManager()
    {
        return _camManager;
    }

    public void ResetForRestart()
    {
        _canControl = true;
        ResetActorState();
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (_skillActor == null)
        {
            _skillActor = GetComponent<MPSkillActorLite>();
        }

        if (_skillActor != null)
        {
            _skillActor.Initialize(gameObject);
        }
    }

    public void SetCanControl(bool canControl, float recoverAfterSeconds = 0f)
    {
        _canControl = canControl;
        if (canControl || recoverAfterSeconds <= 0f)
        {
            return;
        }

        StartCoroutine(RestoreControl(recoverAfterSeconds));
    }

    private System.Collections.IEnumerator RestoreControl(float delay)
    {
        yield return new WaitForSeconds(delay);
        _canControl = true;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!_canControl || _attackCooldownTimer > 0f)
        {
            return;
        }

        PerformAttack();
    }

    private void PerformAttack()
    {
        _attackCooldownTimer = ATTACK_COOLDOWN;
        Debug.Log("[MPSoulActor] Attack performed!");

        var attackValue = ATTACK_DAMAGE;
        if (_attributeComponent != null)
        {
            attackValue = Mathf.RoundToInt(_attributeComponent.GetValue(AttributeType.AttackPower));
        }

        // Simple forward detection
        var center = transform.position + transform.forward * 1.0f;
        var hitColliders = Physics.OverlapSphere(center, ATTACK_RANGE);
        
        foreach (var hit in hitColliders)
        {
            var target = hit.GetComponent<MPCharacterSoulActorBase>();
            if (target != null && target != this && !target.IsDead)
            {
                target.TakeDamage(attackValue);
                Debug.Log($"[MPSoulActor] Hit target: {target.name} for {attackValue}");
            }
        }
    }

    private void OnCastSkill(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TryCastActiveSkillFromUI();
        }
    }

    private MPNpcSoulActor FindNearestNpc()
    {
        var npcs = FindObjectsOfType<MPNpcSoulActor>();
        MPNpcSoulActor nearest = null;
        float best = float.MaxValue;
        var myPos = transform.position;

        foreach (var npc in npcs)
        {
            if (npc == null || npc.IsDead)
            {
                continue;
            }

            var toNpc = npc.transform.position - myPos;
            var dist = toNpc.sqrMagnitude;
            if (dist < best)
            {
                best = dist;
                nearest = npc;
            }
        }

        return nearest;
    }

    public bool TryCastActiveSkillFromUI()
    {
        if (!_canControl || _skillActor == null || (MPRoomManager.Inst != null && MPRoomManager.Inst.IsPaused))
        {
            return false;
        }

        // Aim toward nearest NPC; fallback to forward.
        var dir = transform.forward;
        var targetPos = transform.position + dir * 2f;

        var nearest = FindNearestNpc();
        if (nearest != null)
        {
            var toNpc = nearest.transform.position - transform.position;
            toNpc.y = 0f;
            if (toNpc.sqrMagnitude > 0.001f)
            {
                dir = toNpc.normalized;
                targetPos = nearest.transform.position;
            }
        }

        var casted = _skillActor.OnActiveSkillInput(targetPos, dir);
        if (casted)
        {
            Debug.Log("[MPSoulActor] Casting skill...");
        }
        else
        {
            Debug.Log("[MPSoulActor] Active skill not ready or missing config.");
        }

        return casted;
    }

    public MPSkillActorLite GetSkillActor() => _skillActor;
    #endregion

    #region Input Wrapper
    private class PlayerActions : GameInput.IPlayerActions
    {
        private readonly MPSoulActor _owner;

        public PlayerActions(MPSoulActor owner)
        {
            _owner = owner;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _owner.OnMove(context);
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                _owner.OnAttack(context);
            }
        }

        public void OnCastSkill(InputAction.CallbackContext context)
        {
            _owner.OnCastSkill(context);
        }
    }
    #endregion

    #region Editor

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var center = transform.position + transform.forward * 1.0f;
        Gizmos.DrawWireSphere(center, ATTACK_RANGE);
    }
    
    #endregion
}
