using UnityEngine;

public class PlayerThrowWeapon : MonoBehaviour
{
    [Header("武器设置")]
    public GameObject weaponPrefab;          // 丢出去的武器预制体
    public float throwSpeed = 18f;           // 飞行速度
    public float maxDistance = 8f;           // 超过这个距离没打到就自动返回

    [Header("按键设置")]
    [Tooltip("按住瞄准，松开丢武器")]
    public KeyCode throwKey  = KeyCode.J;    // ← 长按瞄准 / 松开丢出
    public KeyCode recallKey = KeyCode.F;    // 召回武器

    [Header("八向输入")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis   = "Vertical";

    [Header("投掷偏移")]
    [Tooltip("从角色中心沿投掷方向偏移多少距离生成武器")]
    public float throwOffset = 0.7f;

    [Header("斜向角度调节")]
    [Range(0f, 1f)]
    [Tooltip("斜向的竖直分量：越小越平，0.5 比较适中，1 是 45°")]
    public float diagonalYFactor = 0.5f;

    [Header("挂在武器上")]
    public float maxHangTime = 2.5f;                           // 挂多久后开始力竭变色
    public Color tiredColor = new Color(1f, 0.7f, 0.7f);       // 力竭时的颜色

    bool isHooked = false;        // 当前是否正挂在武器上
    ThrownWeapon hookedWeapon;    // 正在挂着的那把武器
    float currentHangTime = 0f;   // 当前这次挂住已经持续了多久
    Color originalColor;          // 角色原始颜色（用于恢复）

    [HideInInspector]
    public bool hasWeapon = true;            // 角色当前是否“手里有武器”

    PlayerPlatformer platformer;             // 用于重置 dash & 锁定操作
    SpriteRenderer sr;

    Vector2 lastMoveDir = Vector2.right;     // 最后一次水平移动方向（用于无输入时的默认方向）
    ThrownWeapon currentWeapon;

    // 瞄准状态
    bool   isAiming = false;
    Vector2 aimRawDir = Vector2.zero;        // 当前瞄准输入方向
    Vector2 aimLastDir = Vector2.right;      // 最近一次有效瞄准方向

    void Awake()
    {
        platformer = GetComponent<PlayerPlatformer>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalColor = sr.color;
        }
    }

    void Update()
    {
        if (isHooked)
        {
            UpdateHook();
            return;
        }

        // 读取输入
        float x = Input.GetAxisRaw(horizontalAxis);
        float y = Input.GetAxisRaw(verticalAxis);
        Vector2 rawDir = new Vector2(x, y);

        // 只在非瞄准状态下更新 lastMoveDir（因为瞄准时角色不会移动）
        if (!isAiming && Mathf.Abs(x) > 0.01f)
        {
            lastMoveDir = new Vector2(Mathf.Sign(x), 0f);
        }

        // —— 按下 J：进入瞄准模式 —— //
        if (Input.GetKeyDown(throwKey))
        {
            StartAim();
        }

        // —— 瞄准中：更新瞄准方向（只改方向，不移动角色）—— //
        if (isAiming)
        {
            UpdateAim(rawDir);
        }

        // —— 松开 J：结束瞄准并丢出武器 —— //
        if (Input.GetKeyUp(throwKey))
        {
            ReleaseAimAndThrow();
        }

        // 召回武器（不受是否瞄准影响）
        if (Input.GetKeyDown(recallKey))
        {
            RecallWeapon();
        }
    }

    // 进入瞄准模式：角色静止（用 PlayerPlatformer.SetControlEnabled(false)）
    void StartAim()
    {
        if (isAiming) return;

        // 手里没武器就没必要瞄准，但可以只做早退
        if (!hasWeapon) return;

        isAiming   = true;
        aimRawDir  = Vector2.zero;
        aimLastDir = (lastMoveDir.sqrMagnitude > 0.01f) ? lastMoveDir : Vector2.right;

        // 锁定角色：不再移动 / 跳跃 / 冲刺（包括在空中会停在原地）
        if (platformer != null)
        {
            platformer.SetControlEnabled(false);
        }
    }

    // 瞄准时更新方向（靠方向键 / 摇杆控制）
    void UpdateAim(Vector2 rawDir)
    {
        // 有输入时，更新瞄准方向
        if (rawDir.sqrMagnitude > 0.01f)
        {
            aimRawDir  = rawDir;
            aimLastDir = rawDir;
        }

        // 你之后如果想画瞄准线 / 箭头，可以在这里根据 aimLastDir 画 Gizmos 或 UI
    }

    // 松开 J：根据瞄准方向丢出武器，并恢复角色控制
    void ReleaseAimAndThrow()
    {
        if (!isAiming)
            return;

        isAiming = false;

        // 恢复角色控制
        if (platformer != null)
        {
            platformer.SetControlEnabled(true);
        }

        // 没武器就不扔（比如已经在别的地方被销毁）
        if (!hasWeapon || weaponPrefab == null)
            return;

        // 决定最终投掷方向：
        //   优先使用瞄准时的输入，其次用角色最后面向
        Vector2 chosen =
            aimRawDir.sqrMagnitude > 0.01f ? aimRawDir :
            (aimLastDir.sqrMagnitude > 0.01f ? aimLastDir :
             (lastMoveDir.sqrMagnitude > 0.01f ? lastMoveDir : Vector2.right));

        Vector2 dir = QuantizeToEightDir(chosen);

        // 从角色中心沿方向偏移一定距离生成武器
        Vector3 spawnPos = transform.position + (Vector3)(dir.normalized * throwOffset);

        GameObject go = Instantiate(weaponPrefab, spawnPos, Quaternion.identity);

        ThrownWeapon weapon = go.GetComponent<ThrownWeapon>();
        if (weapon != null)
        {
            weapon.Init(this, platformer, dir, throwSpeed, maxDistance);
            currentWeapon = weapon;
        }

        hasWeapon = false; // 武器离手

        if (platformer != null)
        {
            platformer.SetDashKillEnabled(false);
        }
    }

    void RecallWeapon()
    {
        if (currentWeapon == null) return;
        currentWeapon.BeginReturn();
    }

    /// 被武器通知“已经回到玩家身上”
    public void OnWeaponReturned()
    {
        hasWeapon = true;
        currentWeapon = null;

        if (platformer != null)
        {
            platformer.SetDashKillEnabled(true);
        }
    }

    /// 被武器通知“玩家在空中抓到武器，需要重置 dash”
    public void OnWeaponCaughtInAir()
    {
        hasWeapon = true;
        currentWeapon = null;

        if (platformer != null)
        {
            platformer.ResetDashState();
            platformer.SetDashKillEnabled(true);
        }
    }

    // 八向量化，斜向可以调得更平
    Vector2 QuantizeToEightDir(Vector2 input)
    {
        if (input.sqrMagnitude < 1e-6f) return Vector2.right;
        Vector2 n = input.normalized;

        float y = diagonalYFactor; // 越小越平

        Vector2[] dirs = new Vector2[]
        {
            Vector2.right,                    // →
            new Vector2( 1f,  y).normalized,  // ↗（更平）
            Vector2.up,                       // ↑
            new Vector2(-1f,  y).normalized,  // ↖
            Vector2.left,                     // ←
            new Vector2(-1f, -y).normalized,  // ↙
            Vector2.down,                     // ↓
            new Vector2( 1f, -y).normalized   // ↘
        };

        int   best    = 0;
        float bestDot = -999f;
        for (int i = 0; i < dirs.Length; i++)
        {
            float d = Vector2.Dot(n, dirs[i]);
            if (d > bestDot)
            {
                bestDot = d;
                best    = i;
            }
        }
        return dirs[best];
    }

    // 死亡时重置武器
    public void ResetWeaponOnDeath()
    {
        if (currentWeapon != null)
        {
            currentWeapon.ForceDestroy();
            currentWeapon = null;
        }

        hasWeapon = true;
        isAiming = false;

        // 确保死亡时不会卡在瞄准锁定状态
        if (platformer != null)
        {
            platformer.SetControlEnabled(true);
            platformer.SetDashKillEnabled(true);
        }
    }

        // 从武器那边发起的“开始挂住”
    public void StartHookFromWeapon(ThrownWeapon weapon)
    {
        if (weapon == null) return;
        if (platformer == null) return;
        if (isHooked) return; // 已经挂着了就不用再来一次

        isHooked = true;
        hookedWeapon = weapon;
        currentHangTime = 0f;

        // 锁定玩家操作（包括重力），防止自己乱动
        platformer.SetControlEnabled(false);

        // 恢复成原本颜色（以免上一次残留）
        if (sr != null)
        {
            sr.color = originalColor;
        }
    }

    // —— 挂住过程：每帧调用 —— //
    void UpdateHook()
    {
        // 万一武器被删了，就自动掉下去
        if (hookedWeapon == null)
        {
            StopHook(false);
            return;
        }

        // 把玩家位置锁在武器位置（你可以根据需要加一点偏移）
        Vector3 wp = hookedWeapon.transform.position;
        transform.position = new Vector3(wp.x, wp.y, transform.position.z);

        // 累计挂住时间
        currentHangTime += Time.deltaTime;

        // 挂太久角色变色表示力竭
        if (sr != null)
        {
            sr.color = (currentHangTime >= maxHangTime) ? tiredColor : originalColor;
        }

        // 规则：必须一直按着 J 才能继续挂
        // 一旦松开 J，就离开武器，并把武器取下（回到手上）
        if (!Input.GetKey(throwKey))
        {
            StopHook(true);
        }
    }

    // —— 停止挂住 —— //
    void StopHook(bool takeWeapon)
    {
        isHooked = false;
        currentHangTime = 0f;

        // 恢复颜色
        if (sr != null)
        {
            sr.color = originalColor;
        }

        // 恢复玩家操作
        if (platformer != null)
        {
            platformer.SetControlEnabled(true);
        }

        if (takeWeapon && hookedWeapon != null)
        {
            // 相当于在空中接住武器：销毁场景里的武器，让武器回到手上，并重置 dash
            if (currentWeapon == hookedWeapon)
            {
                hookedWeapon.ForceDestroy();
                currentWeapon = null;
            }

            hasWeapon = true;

            if (platformer != null)
            {
                platformer.ResetDashState();
                platformer.SetDashKillEnabled(true);
            }
        }

        hookedWeapon = null;
    }

}
