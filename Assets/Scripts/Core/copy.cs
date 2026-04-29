/*  
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
    public float horizontalDashGravity = 0f;  // 水平冲刺时的重力（0=不下落）
    public float verticalDashGravity = 4f;    // 垂直/斜向冲刺时的重力
    public float upwardDashSpeedMultiplier = 0.7f;

    [Header("Dash Hit Enemy")]
    [Tooltip("冲刺时可以被击杀的敌人所在 Layer")]
    public LayerMask dashEnemyMask;

    [Header("Dash Kill")]
    [Tooltip("是否允许冲刺击杀敌人")]
    public bool dashKillEnabled = true;

    [Header("Dash Aim Feedback")]
    [Tooltip("是否开启‘前方有可冲刺敌人’的可视化提示")]
    public bool enableDashAimFeedback = true;

    [Tooltip("检测前方敌人时的射线距离")]
    public float dashDetectDistance = 3f;

    [Tooltip("检测前方敌人用的圆形半径")]
    public float dashAimRadius = 0.4f;

    [Tooltip("没有目标时角色颜色")]
    public Color normalColor = Color.white;

    [Tooltip("前方有可冲刺目标时角色颜色")]
    public Color dashAimColor = Color.cyan;

    [HideInInspector]
    public bool canControl = true;
    float defaultGravity;

    // 组件
    Animator anim;
    Rigidbody2D rb;
    SpriteRenderer sr;

    // 状态
    bool isOnGround;
    public bool IsOnGround => isOnGround;
    bool touchingWallL, touchingWallR;
    bool isWallSliding;
    bool wallJumpLock;
    public bool isDashing;
    bool dashInterrupted;
    Coroutine dashRoutine;
    bool wasOnGround;

    // 空中冲刺是否已用掉（只在落地重置）
    bool airDashUsed;

    // 计时器
    float lastDashTime;
    float lastPressJumpTime;
    float lastOnGroundTime;

    // 方向记忆（无输入时用）
    Vector2 lastMoveDir = Vector2.right;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 3.2f;
        defaultGravity = rb.gravityScale;

        if (sr != null)
        {
            sr.color = normalColor;
        }
    }

    void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        anim.SetBool("isWalking", Mathf.Abs(moveInput) > 0.01f);
        
        if (!canControl)
        {
            return;
        }

        // 输入
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector2 rawDir = new Vector2(x, y);
        if (Mathf.Abs(x) > 0.01f) 
            lastMoveDir = new Vector2(Mathf.Sign(x), 0f);

        // 计时器
        lastPressJumpTime -= Time.deltaTime;
        lastOnGroundTime  -= Time.deltaTime;

        // 探测
        wasOnGround = isOnGround;
        isOnGround    = Physics2D.OverlapCircle(groundCheck.position,  groundRadius, groundMask);
        touchingWallL = Physics2D.OverlapCircle(wallCheckLeft.position, wallRadius, groundMask);
        touchingWallR = Physics2D.OverlapCircle(wallCheckRight.position,wallRadius, groundMask);
        bool touchingWall = (touchingWallL || touchingWallR) && !isOnGround;

        // 落地刷新：土狼 + 空中冲刺次数
        if (isOnGround)
        {
            lastOnGroundTime = coyoteTime;
            airDashUsed = false;
        }

        // 跳跃缓冲
        if (Input.GetButtonDown("Jump")) 
            lastPressJumpTime = jumpBuffer;

        // —— 冲刺命中范围提示（不在冲刺中时才检测）——
        if (enableDashAimFeedback && sr != null && !isDashing)
        {
            bool hasTarget = CheckDashTargetAhead(rawDir);
            sr.color = hasTarget ? dashAimColor : normalColor;
        }

        // —— 冲刺触发：八向 + 空中仅一次 —— //
        if (!isDashing && Time.time >= lastDashTime + dashCooldown && Input.GetKeyDown(dashKey))
        {
            if (!airDashUsed)  // 只检查是否已使用
            {
                Vector2 chosen = rawDir.sqrMagnitude > 0.01f ? rawDir
                                : (lastMoveDir.sqrMagnitude > 0 ? lastMoveDir : Vector2.right);

                Vector2 dir = QuantizeToEightDir(chosen);

                // 启动 dash 协程前，清理旧的
                if (dashRoutine != null)
                {
                    StopCoroutine(dashRoutine);
                }
                dashRoutine = StartCoroutine(CoDash(dir));

                airDashUsed = true;  // 无论哪里冲刺都标记
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
        if (isWallSliding) 
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, wallSlideSpeed);

        // 朝向翻转
        if (sr && Mathf.Abs(x) > 0.01f) 
            sr.flipX = x < 0;
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
        dashInterrupted = false;
        lastDashTime = Time.time;

        float originalGravity = rb.gravityScale;
        
        // 判断冲刺方向
        bool isHorizontalDash = Mathf.Abs(dir.y) < 0.1f;
        bool isUpwardDash = dir.y > 0.1f;
        bool isDownwardDash = dir.y < -0.1f;
        
        // 设置重力
        if (isHorizontalDash)
        {
            rb.gravityScale = horizontalDashGravity; // 水平：0
        }
        else if (isUpwardDash)
        {
            rb.gravityScale = verticalDashGravity;   // 向上：强重力
        }
        else if (isDownwardDash)
        {
            rb.gravityScale = 0f;                   // 向下：无重力（加速下落）
        }
        else
        {
            rb.gravityScale = verticalDashGravity;   // 斜向：正常
        }
        
        // 向上冲刺降低初始速度
        float actualSpeed = isUpwardDash ? (dashSpeed * upwardDashSpeedMultiplier) : dashSpeed;
        rb.linearVelocity = dir.normalized * actualSpeed;

        float elapsed = 0f;
        while (elapsed < dashTime && !dashInterrupted)
        {
            elapsed += Time.deltaTime;
            
            if (isHorizontalDash)
            {
                // 水平冲刺：完全锁定速度，不受重力影响
                rb.linearVelocity = dir.normalized * dashSpeed;
            }
            else
            {
                // 垂直/斜向冲刺：不再每帧设置速度，完全交给物理引擎处理
            }
            
            yield return null;
        }

        rb.gravityScale = originalGravity;
        isDashing = false;
        dashRoutine = null;
        dashInterrupted = false;

        // 结束 dash 时，如果启用了提示，恢复正常颜色
        if (enableDashAimFeedback && sr != null)
        {
            sr.color = normalColor;
        }
    }

    // 八向量化
    Vector2 QuantizeToEightDir(Vector2 input)
    {
        if (input.sqrMagnitude < 1e-6f) return Vector2.right;
        Vector2 n = input.normalized;

        Vector2[] dirs = new Vector2[]
        {
            Vector2.right,                    // →
            new Vector2(1,1).normalized,      // ↗
            Vector2.up,                       // ↑
            new Vector2(-1,1).normalized,     // ↖
            Vector2.left,                     // ←
            new Vector2(-1,-1).normalized,    // ↙
            Vector2.down,                     // ↓
            new Vector2(1,-1).normalized      // ↘
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

        // 在场景视图里画出 dash 检测范围，方便你调试
        Gizmos.color = Color.magenta;
        Vector2 dir = Application.isPlaying 
            ? (lastMoveDir.sqrMagnitude > 0.01f ? lastMoveDir.normalized : Vector2.right)
            : Vector2.right;
        Vector3 start = groundCheck ? groundCheck.position : transform.position;
        Gizmos.DrawWireSphere(start, dashAimRadius);
        Gizmos.DrawLine(start, start + (Vector3)(dir * dashDetectDistance));
    }

    public void ResetDashState()
    {
        // dash 状态清零
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        isDashing = false;
        airDashUsed = false;
        dashInterrupted = false;

        // 让冷却计时也清零
        lastDashTime = Time.time - dashCooldown;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = defaultGravity;

        // 颜色也重置
        if (enableDashAimFeedback && sr != null)
        {
            sr.color = normalColor;
        }
    }

    // 对外接口：开启/关闭玩家操作
    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;

        if (!enabled)
        {
            // 禁用时：停掉所有协程、速度清零、关重力
            StopAllCoroutines();
            dashRoutine = null;
            isDashing = false;
            isWallSliding = false;
            wallJumpLock = false;
            dashInterrupted = false;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
        }
        else
        {
            // 恢复操作：把重力恢复
            rb.gravityScale = defaultGravity;
        }

        // 切换控制时也顺手恢复颜色
        if (enableDashAimFeedback && sr != null)
        {
            sr.color = normalColor;
        }
    }

    public void SetDashKillEnabled(bool enabled)
    {
        dashKillEnabled = enabled;
    }

    // 冲刺中撞到敌人：停在敌人原本位置
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing) return;

        if (!dashKillEnabled) return;

        if (!IsInLayerMask(collision.collider.gameObject, dashEnemyMask))
            return;

        // 敌人当前位置（中心点）
        Vector3 enemyPos = collision.collider.transform.position;

        // 通知 dash 协程结束
        dashInterrupted = true;

        // 停止刚体运动，恢复重力
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = defaultGravity;

        // 玩家移动到敌人的位置（保留原来的 z）
        transform.position = new Vector3(enemyPos.x, enemyPos.y, transform.position.z);

        isDashing = false;

        // 碰到敌人的瞬间也恢复颜色
        if (enableDashAimFeedback && sr != null)
        {
            sr.color = normalColor;
        }
    }

    bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }

    // 检测前方是否有可被 dash 命中的敌人，用于可视化提示
    bool CheckDashTargetAhead(Vector2 rawInput)
    {
        if (dashEnemyMask.value == 0)
            return false;

        // 选择一个检测方向：优先当前输入，其次 lastMoveDir
        Vector2 dir;
        if (rawInput.sqrMagnitude > 0.01f)
        {
            dir = rawInput.normalized;
        }
        else if (lastMoveDir.sqrMagnitude > 0.01f)
        {
            dir = lastMoveDir.normalized;
        }
        else
        {
            dir = Vector2.right;
        }

        Vector3 start = groundCheck ? groundCheck.position : transform.position;

        RaycastHit2D hit = Physics2D.CircleCast(
            start,
            dashAimRadius,
            dir,
            dashDetectDistance,
            dashEnemyMask
        );

        return hit.collider != null;
    }
}

*/