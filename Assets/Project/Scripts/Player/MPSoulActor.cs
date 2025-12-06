using UnityEngine;

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
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_roomManager == null)
        {
            _roomManager = FindObjectOfType<MPRoomManager>();
        }

        CurrentHp = MaxHp;
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
        var inputX = Input.GetAxis("Horizontal");
        var inputZ = Input.GetAxis("Vertical");

        var inputDir = new Vector3(inputX, 0f, inputZ);

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
    #endregion
}
