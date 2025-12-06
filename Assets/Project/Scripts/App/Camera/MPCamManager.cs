using UnityEngine;

// MPCamManager handles a simple follow camera for the local player.
// It keeps a singleton reference and smoothly follows a target with a configurable offset.
public class MPCamManager : MonoBehaviour
{
    #region Inspector
    [SerializeField] private Camera _camera;
    [SerializeField] private Vector3 _followOffset = new Vector3(0f, 15f, -10f);
    [SerializeField] private float _followSmoothSpeed = 10f;
    #endregion

    #region Properties
    public static MPCamManager Inst { get; private set; }
    public Camera MainCamera { get; private set; }
    #endregion

    #region Fields
    private Transform _followTarget;
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
        MainCamera = _camera != null ? _camera : GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (_followTarget == null)
        {
            return;
        }

        var desiredPos = _followTarget.position + _followOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, _followSmoothSpeed * Time.deltaTime);
        transform.LookAt(_followTarget);
    }

    private void OnDestroy()
    {
        if (Inst == this)
        {
            Inst = null;
        }
    }
    #endregion

    #region Public Methods
    public void OnInitCam(MPSoulActor localPlayer)
    {
        if (localPlayer == null)
        {
            Debug.LogWarning("[MPCamManager] OnInitCam called with null player.");
            return;
        }

        _followTarget = localPlayer.transform;
    }
    #endregion
}
