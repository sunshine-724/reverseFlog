using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// カエルのプレイヤー操作。
/// 左右移動・ジャンプ・壁/天井張りつき・反転入力を処理する。
/// Input System の PlayerInput (SendMessages) を使用。
/// Move → OnMove, Jump → OnJump で受け取る。
/// 反転(F) とリトライ(R) は Keyboard で直接読む。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("接地判定")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("壁張りつき")]
    [SerializeField] private Transform wallCheckFront;
    [SerializeField] private float wallCheckRadius = 0.15f;



    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private float moveInput;
    private bool jumpRequested;

    private bool isGrounded;
    private bool isTouchingWall;
    private bool isClinging;          // 壁に張りついている状態

    private int facingDirection = 1;  // 1 = 右, -1 = 左

    // ジャンプ後の張りつきクールダウン（上昇中に天井に張りつかないようにする）
    private float clingCooldownTimer;
    private const float CLING_COOLDOWN = 0.25f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
    }

    // ── Input System コールバック（SendMessages） ──────

    /// <summary>PlayerInput(SendMessages) から呼ばれる</summary>
    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>().x;
    }

    /// <summary>PlayerInput(SendMessages) から呼ばれる</summary>
    private void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpRequested = true;
    }

    // ── 物理更新 ────────────────────────────────────────

    private void FixedUpdate()
    {
        // 反転中は操作を受けない
        if (GravityManager.Instance != null && GravityManager.Instance.IsFlipping)
        {
            // 張りつき中なら速度ゼロを維持
            if (isClinging)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        CheckSurroundings();
        HandleMovement();
        HandleJump();
        HandleCling();
    }

    private void Update()
    {
        // 反転入力: F キーまたはマウス右クリック
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryFlip();
        }
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryFlip();
        }

        // 反転中はカメラが180度回転するので左右入力も反転
        float effectiveInput = moveInput * (GravityManager.Instance != null && GravityManager.Instance.IsGravityInverted ? -1f : 1f);

        // スプライト反転
        if (effectiveInput > 0.1f) facingDirection = 1;
        else if (effectiveInput < -0.1f) facingDirection = -1;

        if (spriteRenderer != null)
            spriteRenderer.flipX = facingDirection < 0;
    }

    private void TryFlip()
    {
        if (GravityManager.Instance != null && !GravityManager.Instance.IsFlipping)
        {
            GravityManager.Instance.Flip();
        }
    }

    // ── 周囲チェック ────────────────────────────────────

    private void CheckSurroundings()
    {
        float gravitySign = GravityManager.Instance != null
            ? GravityManager.Instance.GravitySign
            : -1f;

        // groundCheck の位置を重力方向に合わせて反転させる
        // 通常: (0, -0.45, 0)  反転時: (0, +0.45, 0)
        if (groundCheck != null)
        {
            Vector3 lp = groundCheck.localPosition;
            float absY = Mathf.Abs(lp.y);
            groundCheck.localPosition = new Vector3(lp.x, gravitySign * absY, lp.z);

            isGrounded = Physics2D.OverlapCircle(
                groundCheck.position, groundCheckRadius, groundLayer);
        }

        // 壁判定（進行方向の前面）
        if (wallCheckFront != null)
        {
            isTouchingWall = Physics2D.OverlapCircle(
                wallCheckFront.position, wallCheckRadius, groundLayer);
        }
    }

    // ── 移動 ────────────────────────────────────────────

    private void HandleMovement()
    {
        if (isClinging) return; // 張りつき中は横移動しない

        // 反転中は左右入力を反転
        float dir = GravityManager.Instance != null && GravityManager.Instance.IsGravityInverted ? -1f : 1f;
        float vx = moveInput * dir * moveSpeed;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    // ── ジャンプ ────────────────────────────────────────

    private void HandleJump()
    {
        if (!jumpRequested) return;
        jumpRequested = false;

        float gravitySign = GravityManager.Instance != null
            ? GravityManager.Instance.GravitySign
            : -1f;

        if (isGrounded || isClinging)
        {
            // 張りつき解除
            if (isClinging)
            {
                isClinging = false;
                rb.gravityScale = 1f;
            }

            // ジャンプ後に張りつきクールダウンを開始
            clingCooldownTimer = CLING_COOLDOWN;

            // 重力と逆方向にジャンプ
            float jumpDir = -gravitySign; // 通常時: +1 (上向き), 反転時: -1 (下向き)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpDir * jumpForce, ForceMode2D.Impulse);
        }
    }

    // ── 張りつき ────────────────────────────────────────

    private void HandleCling()
    {
        // クールダウン中は張りつかない（ジャンプ上昇中に天井に張りつくのを防ぐ）
        if (clingCooldownTimer > 0f)
        {
            clingCooldownTimer -= Time.fixedDeltaTime;
            return;
        }

        // 張りつき条件:
        // - 接地していない
        // - まだ張りついていない
        // - 壁の場合: 壁方向に移動入力がある
        // - 天井の場合: 上昇速度がほぼ 0（落下し始めている or 頂点付近）
        bool canClingWall = isTouchingWall && IsPushingTowardWall();

        if (!isGrounded && canClingWall && !isClinging)
        {
            isClinging = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        // 壁から離れたら解除
        if (isClinging && !isTouchingWall)
        {
            isClinging = false;
            rb.gravityScale = 1f;
        }

        // 張りつき中は速度ゼロ
        if (isClinging)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>壁方向に入力しているか</summary>
    private bool IsPushingTowardWall()
    {
        // wallCheckFront がプレイヤーの右側にあるなら右入力で張りつく
        if (wallCheckFront == null) return false;
        float wallDir = Mathf.Sign(wallCheckFront.localPosition.x);
        return moveInput * wallDir > 0.1f;
    }


    // ── Gizmo ───────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.blue;
        if (wallCheckFront != null)
            Gizmos.DrawWireSphere(wallCheckFront.position, wallCheckRadius);

    }
}
