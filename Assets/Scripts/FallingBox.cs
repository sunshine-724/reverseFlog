using UnityEngine;

/// <summary>
/// 重力反転に従い落下する箱。
/// Rigidbody2D が付いていれば Physics2D.gravity に従うので、
/// このスクリプトは初期設定とリセットのみ担当する。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class FallingBox : MonoBehaviour
{
    private Vector3 initialPosition;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialPosition = transform.position;

        // 箱は重力に従って動く（mass を少し重くして安定させる）
        rb.mass = 2f;
        rb.freezeRotation = true;
    }

    /// <summary>
    /// リトライ時に初期位置に戻す。
    /// </summary>
    public void ResetState()
    {
        transform.position = initialPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
}
