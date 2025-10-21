using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerPlatformer : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;
    public float airControl = 0.6f;
    public float accel = 50f;
    public float decel = 60f;
    public float maxFallSpeed = -18f;

    [Header("Jump")]
    public float jumpForce = 13f;
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.12f;
    public float jumpCutMultiplier = 0.5f;

    [Header("Ground / Wall Check")]
    public Transform groundCheck;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public float groundRadius = 0.2f;
    public float wallRadius = 0.2f;
    public LayerMask groundMask;

    [Header("Wall Slide & Wall Jump")]
    public float wallSlideSpeed = -3.5f;
    public Vector2 wallJumpImpulse = new Vector2(12f, 14f);
    public float wallJumpLockTime = 0.12f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashTime = 0.18f;
    public float dashCooldown = 0.35f;
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

    // ★ 空中冲刺是否已用掉（只在落地重置）
    bool airDashUsed;

    // 计时器
    float lastDashTime;
    float lastPressJumpTime;
    float lastOnGroundTime;

    // 方向记忆（无输入时用）
    Vector2 lastMoveDir = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 3.2f;
    }

    void Update()
    {
        // 输入
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector2 rawDir = new Vector2(x, y);
        if (Mathf.Abs(x) > 0.01f) lastMoveDir = new Vector2(Mathf.Sign(x), 0f);

        // 计时器
        lastPressJumpTime -= Time.deltaTime;
        lastOnGroundTime  -= Time.deltaTime;

        // 探测
        isOnGround    = Physics2D.OverlapCircle(groundCheck.position,  groundRadius, groundMask);
        touchingWallL = Physics2D.OverlapCircle(wallCheckLeft.position, wallRadius, groundMask);
        touchingWallR = Physics2D.OverlapCircle(wallCheckRight.position,wallRadius, groundMask);
        bool touchingWall = (touchingWallL || touchingWallR) && !isOnGround;

        // 落地刷新：土狼 + 空中冲刺次数
        if (isOnGround)
        {
            lastOnGroundTime = coyoteTime;
            airDashUsed = false; // ★ 只在落地时重置
        }

        // 跳跃缓冲
        if (Input.GetButtonDown("Jump")) lastPressJumpTime = jumpBuffer;

        // —— 冲刺触发：八向 + 空中仅一次 —— //
        if (!isDashing && Time.time >= lastDashTime + dashCooldown && Input.GetKeyDown(dashKey))
        {
            bool canDash = isOnGround || !airDashUsed; // 地面无限，空中只能一次
            if (canDash)
            {
                Vector2 chosen = rawDir.sqrMagnitude > 0.01f ? rawDir
                                : (lastMoveDir.sqrMagnitude > 0 ? lastMoveDir : Vector2.right);

                Vector2 dir = QuantizeToEightDir(chosen); // ★ 八向量化
                StartCoroutine(CoDash(dir));

                if (!isOnGround) airDashUsed = true; // ★ 空中消耗
            }
        }

        // 起跳优先普通跳，其次墙跳
        if (lastPressJumpTime > 0f)
        {
            if (lastOnGroundTime > 0f && !isDashing)
            {
                JumpUp();
            }
            else if (touchingWall && !isDashing)
            {
                WallJump();
            }
        }

        // 可变跳
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f && !isDashing)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        // 水平移动（非冲刺中）
        if (!isDashing)
        {
            float inputX = x;
            if (wallJumpLock)
                inputX = Mathf.Clamp(inputX, 0f, 1f) * (touchingWallL ? 1 : (touchingWallR ? -1 : Mathf.Sign(inputX)));

            float target = inputX * moveSpeed;
            float factor = isOnGround ? 1f : airControl;
            float a = Mathf.Abs(target) > 0.01f ? accel : decel;
            float vx = Mathf.MoveTowards(rb.linearVelocity.x, target, a * factor * Time.deltaTime);
            rb.linearVelocity = new Vector2(vx, Mathf.Max(rb.linearVelocity.y, maxFallSpeed));
        }

        // 墙滑
        isWallSliding = (touchingWallL || touchingWallR) && rb.linearVelocity.y < wallSlideSpeed && !isDashing;
        if (isWallSliding) rb.linearVelocity = new Vector2(rb.linearVelocity.x, wallSlideSpeed);

        // 朝向翻转
        if (sr && Mathf.Abs(x) > 0.01f) sr.flipX = x < 0;
    }

    void JumpUp()
    {
        lastPressJumpTime = 0f;
        lastOnGroundTime  = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void WallJump()
    {
        lastPressJumpTime = 0f;
        int dir = touchingWallL ? 1 : -1;
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
            rb.linearVelocity = dir * dashSpeed; // 维持冲刺方向与速度
            yield return null;
        }

        rb.gravityScale = originalGravity;
        isDashing = false;
    }

    // ★ 八向量化
    Vector2 QuantizeToEightDir(Vector2 input)
    {
        if (input.sqrMagnitude < 1e-6f) return Vector2.right;
        Vector2 n = input.normalized;

        Vector2[] dirs = new Vector2[]
        {
            new Vector2(1,0),   // →
            new Vector2(1,1).normalized,   // 
            new Vector2(0,1),   // ↑
            new Vector2(-1,1).normalized,  // 
            new Vector2(-1,0),  // ←
            new Vector2(-1,-1).normalized, // 
            new Vector2(0,-1),  // ↓
            new Vector2(1,-1).normalized   // 
        };

        int best = 0;
        float bestDot = -999f;
        for (int i = 0; i < dirs.Length; i++)
        {
            float d = Vector2.Dot(n, dirs[i]);
            if (d > bestDot) { bestDot = d; best = i; }
        }
        return dirs[best];
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