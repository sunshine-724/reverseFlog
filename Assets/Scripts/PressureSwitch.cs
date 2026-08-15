using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 箱など重い物体が乗ったら作動する感圧スイッチ。
/// OnActivated / OnDeactivated を UnityEvent で公開し、
/// Inspector 上で Door 等を接続する。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PressureSwitch : MonoBehaviour
{
    [Header("イベント")]
    public UnityEvent OnActivated;
    public UnityEvent OnDeactivated;

    [Header("見た目")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.red;

    private SpriteRenderer spriteRenderer;
    private int overlappingCount;
    private bool isActive;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        UpdateVisual();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 箱（FallingBox）またはプレイヤーが乗ったら作動
        if (other.GetComponent<FallingBox>() != null ||
            other.GetComponent<PlayerController>() != null)
        {
            overlappingCount++;
            if (!isActive)
            {
                isActive = true;
                OnActivated?.Invoke();
                UpdateVisual();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<FallingBox>() != null ||
            other.GetComponent<PlayerController>() != null)
        {
            overlappingCount = Mathf.Max(0, overlappingCount - 1);
            if (overlappingCount == 0 && isActive)
            {
                isActive = false;
                OnDeactivated?.Invoke();
                UpdateVisual();
            }
        }
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = isActive ? activeColor : inactiveColor;
    }

    public void ResetState()
    {
        overlappingCount = 0;
        isActive = false;
        UpdateVisual();
    }
}
