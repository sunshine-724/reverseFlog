using UnityEngine;

/// <summary>
/// ゴール判定。プレイヤーが触れたらクリア処理を呼ぶ。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Goal : MonoBehaviour
{
    [Header("見た目")]
    [SerializeField] private Color goalColor = Color.yellow;

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = goalColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StageClear();
            }
        }
    }
}
