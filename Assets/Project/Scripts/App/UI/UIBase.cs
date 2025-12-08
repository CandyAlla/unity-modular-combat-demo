using UnityEngine;
using UnityEngine.Events;
using System.Threading;
using Cysharp.Threading.Tasks;

// UIBase provides a unified open/close flow, simple animations, and lifecycle hooks.
// It is a simplified version aligned with the original project's structure, without sub-UI dependencies.
public class UIBase : MonoBehaviour
{
    #region Inspector
    [Header("Animation")]
    [SerializeField] private AnimationCurve _openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve _closeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private float _openDuration = 0.15f;
    [SerializeField] private float _closeDuration = 0.15f;
    [SerializeField] private Vector3 _openScale = Vector3.one;
    [SerializeField] private Vector3 _closedScale = Vector3.one * 0.95f;

    [Header("Events")]
    [SerializeField] private UnityEvent _onOpenCompleted;
    [SerializeField] private UnityEvent _onCloseCompleted;
    #endregion

    #region Fields
    private CanvasGroup _canvasGroup;
    private CancellationTokenSource _animCts;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    protected virtual void OnDestroy()
    {
        _animCts?.Cancel();
        _animCts?.Dispose();
        _animCts = null;
    }
    #endregion

    #region Public Methods
    public virtual void Open()
    {
        gameObject.SetActive(true);
        _animCts?.Cancel();
        _animCts?.Dispose();
        _animCts = new CancellationTokenSource();
        AnimateOpenAsync(_animCts.Token).Forget();
    }

    public virtual void Close()
    {
        _animCts?.Cancel();
        _animCts?.Dispose();
        _animCts = new CancellationTokenSource();
        AnimateCloseAsync(_animCts.Token).Forget();
    }
    #endregion

    #region Protected Hooks
    protected virtual void OnOpenUI() { }
    protected virtual void OnCloseUI() { }
    #endregion

    #region Private Methods
    private async UniTaskVoid AnimateOpenAsync(CancellationToken token)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        OnOpenUI();

        var duration = Mathf.Max(0.01f, _openDuration);
        var time = 0f;
        while (time < duration && !token.IsCancellationRequested)
        {
            time += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(time / duration);
            var alpha = _openCurve.Evaluate(t);
            _canvasGroup.alpha = alpha;
            transform.localScale = Vector3.Lerp(_closedScale, _openScale, alpha);
            await UniTask.Yield(PlayerLoopTiming.TimeUpdate, token);
        }

        if (!token.IsCancellationRequested)
        {
            _canvasGroup.alpha = 1f;
            transform.localScale = _openScale;
            _onOpenCompleted?.Invoke();
        }
    }

    private async UniTaskVoid AnimateCloseAsync(CancellationToken token)
    {
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        OnCloseUI();

        var duration = Mathf.Max(0.01f, _closeDuration);
        var time = 0f;
        while (time < duration && !token.IsCancellationRequested)
        {
            time += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(time / duration);
            var alpha = _closeCurve.Evaluate(t);
            _canvasGroup.alpha = alpha;
            transform.localScale = Vector3.Lerp(_openScale, _closedScale, t);
            await UniTask.Yield(PlayerLoopTiming.TimeUpdate, token);
        }

        if (!token.IsCancellationRequested)
        {
            _canvasGroup.alpha = 0f;
            transform.localScale = _closedScale;
            _onCloseCompleted?.Invoke();
            gameObject.SetActive(false);
        }
    }
    #endregion
}
