using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerPlatformer : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;         // 地面目标速度
    public float airControl = 0.6f;      // 空中转向比例
    public float accel = 50f;            // 加速
    public float decel = 60f;            // 减速
    public float maxFallSpeed = -18f;    // 最大下落速度（向下为负）

    [Header("Jump")]
    public float jumpForce = 13f;        // 起跳冲量
    public float coyoteTime = 0.12f;     // ✅ 土狼时间：离地后这段时间仍可起跳
    public float jumpBuffer = 0.12f;     // 跳跃缓冲：提前按跳
    public float jumpCutMultiplier = 0.5f;// 可变跳：松开跳剪切上升

    [Header("Ground / Wall Check")]
    public Transform groundCheck;        // 脚底探测点（放在碰撞体底部外一点）
    public Transform wallCheckLeft;      // 左墙探测点（身体左侧外一点）
    public Transform wallCheckRight;     // 右墙探测点（身体右侧外一点）
    public float groundRadius = 0.2f;
    public float wallRadius = 0.2f;
    public LayerMask groundMask;         // 勾选 Ground 层

    [Header("Wall Slide & Wall Jump")]
    public float wallSlideSpeed = -3.5f; // 墙滑最大下落速度
    public Vector2 wallJumpImpulse = new Vector2(12f, 14f); // 墙跳冲量（更远）
    public float wallJumpLockTime = 0.12f; // 墙跳后短暂无反向吸墙

    [Header("Dash（地面与空中都可用）")]
    public float dashSpeed = 20f;        // 冲刺速度
    public float dashTime = 0.18f;       // 冲刺持续
    public float dashCooldown = 0.35f;   // 冷却
    public KeyCode dashKey = KeyCode.LeftShift;

    // 组件
    Rigidbody2D rb;
    SpriteRenderer sr;

    // 状态
    bool isOnGround;
    bool touchingWallL, touchingWallR;
    bool isWallSliding;
    bool wallJumpLock;
    bool isDashing;

    // 计时器
    float lastDashTime;
    float lastPressJumpTime;
    float lastOnGroundTime; // 用于土狼时间

    // 方向记忆（无输入时冲刺用）
    Vector2 lastMoveDir = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 3.2f;
    }

    void Update()
    {
        // —— 输入 —— //
        float x = Input.GetAxisRaw("Horizontal");
        Vector2 rawDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Mathf.Abs(x) > 0.01f) lastMoveDir = new Vector2(Mathf.Sign(x), 0f);

        // —— 计时器衰减 —— //
        lastPressJumpTime -= Time.deltaTime;
        lastOnGroundTime  -= Time.deltaTime;

        // —— 探测 —— //
        isOnGround    = Physics2D.OverlapCircle(groundCheck.position,  groundRadius, groundMask);
        touchingWallL = Physics2D.OverlapCircle(wallCheckLeft.position, wallRadius, groundMask);
        touchingWallR = Physics2D.OverlapCircle(wallCheckRight.position,wallRadius, groundMask);
        bool touchingWall = (touchingWallL || touchingWallR) && !isOnGround;

        // 落地时刷新土狼时间
        if (isOnGround) lastOnGroundTime = coyoteTime;

        // 跳跃缓冲：记录“最近按过跳”
        if (Input.GetButtonDown("Jump")) lastPressJumpTime = jumpBuffer;

        // —— 冲刺（地面/空中均可触发）—— //
        if (!isDashing && Time.time >= lastDashTime + dashCooldown && Input.GetKeyDown(dashKey))
        {
            Vector2 dir = rawDir.sqrMagnitude > 0.01f ? rawDir.normalized
                                                      : (lastMoveDir.sqrMagnitude > 0 ? lastMoveDir : Vector2.right);
            StartCoroutine(CoDash(dir));
        }

        // —— 起跳逻辑：优先普通跳，其次墙跳 —— //
        if (lastPressJumpTime > 0f)
        {
            // 允许在“离地后的土狼窗口”内起跳
            if (lastOnGroundTime > 0f && !isDashing)
            {
                JumpUp();
            }
            else if (touchingWall && !isDashing)
            {
                WallJump();
            }
        }

        // —— 可变跳 —— //
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f && !isDashing)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        // —— 水平移动（非冲刺中）—— //
        if (!isDashing)
        {
            float inputX = x;
            if (wallJumpLock) // 墙跳后短暂无反向吸墙
                inputX = Mathf.Clamp(inputX, 0f, 1f) * (touchingWallL ? 1 : (touchingWallR ? -1 : Mathf.Sign(inputX)));

            float target = inputX * moveSpeed;
            float factor = isOnGround ? 1f : airControl;
            float a = Mathf.Abs(target) > 0.01f ? accel : decel;
            float vx = Mathf.MoveTowards(rb.linearVelocity.x, target, a * factor * Time.deltaTime);
            rb.linearVelocity = new Vector2(vx, Mathf.Max(rb.linearVelocity.y, maxFallSpeed));
        }

        // —— 墙滑 —— //
        isWallSliding = (touchingWallL || touchingWallR) && rb.linearVelocity.y < wallSlideSpeed && !isDashing;
        if (isWallSliding) rb.linearVelocity = new Vector2(rb.linearVelocity.x, wallSlideSpeed);

        // —— 朝向翻转 —— //
        if (sr && Mathf.Abs(x) > 0.01f) sr.flipX = x < 0;
    }

    void JumpUp()
    {
        lastPressJumpTime = 0f;
        lastOnGroundTime  = 0f; // 用掉土狼机会
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void WallJump()
    {
        lastPressJumpTime = 0f;
        int dir = touchingWallL ? 1 : -1; // 左墙→向右；右墙→向左
        Vector2 v = new Vector2(dir * wallJumpImpulse.x, wallJumpImpulse.y);
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(v, ForceMode2D.Impulse);
        StartCoroutine(CoWallJumpLock());
    }

    IEnumerator CoWallJumpLock()
    {
        wallJumpLock = true;
        yield return new WaitForSeconds(wallJumpLockTime);
        wallJumpLock = false;
    }

    IEnumerator CoDash(Vector2 dir)
    {
        isDashing = true;
        lastDashTime = Time.time;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = dir.normalized * dashSpeed;

        float t = dashTime;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            rb.linearVelocity = dir * dashSpeed; // 期间保持冲刺速度
            yield return null;
        }

        rb.gravityScale = originalGravity;
        isDashing = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
        if (wallCheckLeft)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheckLeft.position, wallRadius);
        }
        if (wallCheckRight)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheckRight.position, wallRadius);
        }
    }
}