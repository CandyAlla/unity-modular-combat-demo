using UnityEngine;
using UnityEngine.UI;
using TMPro;

// PlayerHUD binds to an actor, tracks HP changes, and billboards toward camera.
public class PlayerHUD : MonoBehaviour
{
    #region Inspector
    [Header("UI References")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _labelText;
    [Header("Canvas Override (optional)")]
    [SerializeField] private Canvas _overrideCanvas;
    [Header("Offsets")]
    [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector2 _screenOffset = Vector2.zero;
    #endregion

    #region Fields
    private MPCharacterSoulActorBase _actor;
    private Transform _anchor;
    private Camera _camera;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private RectTransform _canvasRectTransform;
    #endregion


    #region Public API
    public void Bind(MPCharacterSoulActorBase actor, Transform anchor, Camera cam)
    {
        Unsubscribe();

        _actor = actor;
        _anchor = anchor != null ? anchor : actor != null ? actor.transform : null;
        _camera = cam != null ? cam : Camera.main;
        
        // Force refresh canvas cache
        _canvas = _overrideCanvas;
        _canvasRectTransform = null;
        CacheCanvas();
        
        // Reset scale/anchor which might be messed up by pool parenting
        _rectTransform.localScale = Vector3.one;
        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Subscribe();
        RefreshNow();
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        CacheCanvas();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }
    #endregion

    #region Private Methods
    private void Subscribe()
    {
        if (_actor != null)
        {
            _actor.OnHpChanged += UpdateHpBar;
        }
        
    }

    private void Unsubscribe()
    {
        if (_actor != null)
        {
            _actor.OnHpChanged -= UpdateHpBar;
        }

        // if (MPCamManager.Inst != null)
        // {
        //     MPCamManager.Inst.OnCameraUpdated -= UpdatePosition;
        // }
    }

    private void UpdatePosition()
    {
        if (_actor == null || _anchor == null)
        {
            return;
        }

        var worldPos = _anchor.position;
        // Apply height offset derived from actor config
        if (_actor != null)
        {
            worldPos.y += _actor.HealthBarHeight;
        }
        worldPos += _worldOffset;

        var canvas = _canvas;
        if (canvas == null)
        {
            CacheCanvas();
            canvas = _canvas;
        }

        // Ensure we are parented to the canvas if one is found (avoids being left under a 3D root from pooling).
        if (canvas != null && _rectTransform != null && _rectTransform.transform.parent != canvas.transform)
        {
            _rectTransform.SetParent(canvas.transform, false);
            _canvasRectTransform = canvas.GetComponent<RectTransform>();
        }

        // If no canvas is available, fall back to world-space placement to avoid snapping to origin.
        if (canvas == null)
        {
            transform.position = worldPos;
            var camFallback = _camera != null ? _camera : Camera.main;
            if (camFallback != null)
            {
                var camForward = transform.position - camFallback.transform.position;
                if (camForward.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(camForward.normalized, Vector3.up);
                }
            }
            return;
        }

        // World-space canvas: place directly in world, face camera
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            transform.position = worldPos;
            var camToUse = _camera != null ? _camera : canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (camToUse != null)
            {
                var camForward = transform.position - camToUse.transform.position;
                if (camForward.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(camForward.normalized, Vector3.up);
                }
            }
            return;
        }

        // Screen-space canvas
        if (canvas != null && _rectTransform != null && _canvasRectTransform != null)
        {
            // Always try to use a camera for stable conversion, even for Overlay.
            var camToUse = _camera != null ? _camera : (canvas.worldCamera != null ? canvas.worldCamera : Camera.main);

            // If we still don't have a camera and render mode needs one, bail out.
            if (camToUse == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return;
            }

            var screenPos = RectTransformUtility.WorldToScreenPoint(camToUse, worldPos);

            // If behind camera in camera-mode, skip this frame to avoid jumps.
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && camToUse != null)
            {
                var vp = camToUse.WorldToViewportPoint(worldPos);
                if (vp.z <= 0f)
                {
                    return;
                }
            }
            
            // Crucial Fix: For Overlay, camera MUST be null in ScreenPointToLocalPointInRectangle
            Camera rectCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camToUse;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, screenPos, rectCam, out var localPoint))
            {
                _rectTransform.anchoredPosition = localPoint + _screenOffset;
                _rectTransform.rotation = Quaternion.identity;
            }
        }
    }

    private void RefreshNow()
    {
        if (_actor != null)
        {
            UpdateHpBar(_actor.CurrentHpValue, _actor.MaxHpValue);
        }
        else
        {
            UpdateHpBar(0, 1);
        }
        UpdatePosition();
    }

    private void UpdateHpBar(int current, int max)
    {
        if (_hpSlider != null)
        {
            float ratio = (float)current / Mathf.Max(1, max);
            _hpSlider.value = ratio;
        }

        if (_hpText != null)
        {
            _hpText.text = $"{current} / {max}";
        }
    }

    private void CacheCanvas()
    {
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                // Use only active canvases in scene to avoid hidden editor-time canvases causing wrong coordinates.
                foreach (var cv in Object.FindObjectsOfType<Canvas>())
                {
                    if (cv.isActiveAndEnabled)
                    {
                        _canvas = cv;
                        break;
                    }
                }
            }
        }

        if (_canvas != null)
        {
            _canvasRectTransform = _canvas.GetComponent<RectTransform>();
        }
    }

    public void SetLabel(string text)
    {
        if (_labelText == null)
        {
            return;
        }

        var hasText = !string.IsNullOrEmpty(text);
        _labelText.gameObject.SetActive(hasText);
        if (hasText)
        {
            _labelText.text = text;
        }
    }
    #endregion
}
