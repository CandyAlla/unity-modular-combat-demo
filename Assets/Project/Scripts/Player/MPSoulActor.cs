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
            transform.position += worldDir * (_moveSpeed * deltaTime);
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

        // Simple forward detection
        var center = transform.position + transform.forward * 1.0f;
        var hitColliders = Physics.OverlapSphere(center, ATTACK_RANGE);
        
        foreach (var hit in hitColliders)
        {
            var target = hit.GetComponent<MPCharacterSoulActorBase>();
            if (target != null && target != this && !target.IsDead)
            {
                target.TakeDamage(ATTACK_DAMAGE);
                Debug.Log($"[MPSoulActor] Hit target: {target.name}");
            }
        }
    }
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
