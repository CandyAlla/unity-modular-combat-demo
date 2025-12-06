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

    protected override void Update()
    {
        if (_roomManager != null && _roomManager.State != MPRoomManager.RoomState.Running)
        {
            return;
        }

        base.Update();
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
        if (_roomManager != null)
        {
            _roomManager.OnPlayerDead();
        }

        _canControl = false;
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

    private void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
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
    }
    #endregion
}
