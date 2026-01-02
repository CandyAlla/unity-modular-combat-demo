using  TMPro;
using UnityEngine;

public class SoulFloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshPro _tmpText;
    [SerializeField] private TextMeshProUGUI _tmpTextUI; // Support both World and UI space if needed
    
    private FloatTextInfo _info;
    private float _timer;
    private Vector3 _startPos;
    private PoolKey _poolKey = PoolKey.UI_FloatText;

    public void Init(FloatTextInfo info)
    {
        _info = info;
        _timer = 0f;
        _startPos = transform.position;

        SetText(info);
        SetColor(info);
    }

    private void Update()
    {
        if (_info == null) return;

        _timer += Time.deltaTime;

        // Simple upward movement
        float progress = _timer / _info.Duration;
        transform.position = _startPos + Vector3.up * (_info.MoveSpeed * progress);

        // Fade out logic could go here if using CanvasGroup or VertexColor

        if (_timer >= _info.Duration)
        {
            Recycle();
        }
        else
        {
            // Billboard effect: always face camera
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    private void SetText(FloatTextInfo info)
    {
        string text = info.Value.ToString();
        if (info.Type == FloatTextType.Miss) text = "MISS";
        if (info.Type == FloatTextType.Buff) text = !string.IsNullOrEmpty(info.CustomText) ? info.CustomText : "BUFF";

        if (_tmpText != null) _tmpText.text = text;
        if (_tmpTextUI != null) _tmpTextUI.text = text;
    }

    private void SetColor(FloatTextInfo info)
    {
        Color c = info.Color;

        if (_tmpText != null) _tmpText.color = c;
        if (_tmpTextUI != null) _tmpTextUI.color = c;
    }

    private void Recycle()
    {
        if (PoolManager.Inst != null)
        {
            PoolManager.DespawnItemToPool(_poolKey, this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
