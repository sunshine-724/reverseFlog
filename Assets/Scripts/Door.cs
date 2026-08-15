using UnityEngine;
using System.Collections;

/// <summary>
/// スイッチ連動で開く扉。
/// PressureSwitch の UnityEvent から Open() / Close() を呼ぶ。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Door : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("開閉アニメーションの所要時間（秒）")]
    [SerializeField] private float openDuration = 0.3f;

    [Header("見た目")]
    [SerializeField] private Color closedColor = new Color(0.5f, 0.3f, 0.1f); // 茶色
    [SerializeField] private Color openColor = new Color(0.5f, 0.3f, 0.1f, 0.2f); // 半透明

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D col;
    private Vector3 closedScale;
    private bool isOpen;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        closedScale = transform.localScale;
        col.isTrigger = false; // 閉じている間は壁として機能
        UpdateVisual();
    }

    /// <summary>PressureSwitch.OnActivated から呼ぶ</summary>
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        col.enabled = false; // 通り抜け可能にする
        UpdateVisual();
    }

    /// <summary>PressureSwitch.OnDeactivated から呼ぶ</summary>
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        col.enabled = true;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isOpen ? openColor : closedColor;
        }

        // 開いたら小さくする（視覚的に消える演出）
        transform.localScale = isOpen
            ? new Vector3(closedScale.x, closedScale.y * 0.1f, closedScale.z)
            : closedScale;
    }

    public void ResetState()
    {
        isOpen = false;
        col.enabled = true;
        transform.localScale = closedScale;
        UpdateVisual();
    }
}
