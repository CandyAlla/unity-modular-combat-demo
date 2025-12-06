using UnityEngine;
using UnityEngine.InputSystem;

// MPSoulActor represents the local player actor with basic health and hooks for controls.
// Movement/rotation and combat are stubbed for now; focus on lifecycle and health API.
public class MPSoulActor : MonoBehaviour
{
    #region Inspector
    public int MaxHp = 100;
    public int CurrentHp = 100;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private MPRoomManager _roomManager;
    #endregion

    #region Fields
    private bool _isDead;
    private GameInput _input;
    private Vector2 _moveInput;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_roomManager == null)
        {
            _roomManager = FindObjectOfType<MPRoomManager>();
        }

        CurrentHp = MaxHp;

        _input = new GameInput();
        _input.Player.SetCallbacks(new PlayerActions(this));
    }

    private void OnEnable()
    {
        _input?.Enable();
    }

    private void OnDisable()
    {
        _input?.Disable();
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        if (_roomManager != null && _roomManager.State != MPRoomManager.RoomState.Running)
        {
            return;
        }

        HandleMovement();
    }

    private void OnDestroy()
    {
        _input?.Dispose();
    }
    #endregion

    #region Public Methods
    public void TakeDamage(int amount)
    {
        if (_isDead)
        {
            return;
        }

        CurrentHp = Mathf.Clamp(CurrentHp - Mathf.Max(0, amount), 0, MaxHp);
        if (CurrentHp <= 0)
        {
            Die();
        }
    }
    #endregion

    #region Private Methods
    private void HandleMovement()
    {
        var inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

        if (inputDir.sqrMagnitude > 0.001f)
        {
            var worldDir = inputDir.normalized;
            transform.position += worldDir * (_moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(worldDir, Vector3.up);
        }
    }

    private void Die()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        Debug.Log("[MPSoulActor] Player died");

        if (_roomManager != null)
        {
            _roomManager.OnPlayerDead();
        }

        gameObject.SetActive(false);
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
