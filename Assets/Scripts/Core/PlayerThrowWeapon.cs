using UnityEngine;

public class PlayerThrowWeapon : MonoBehaviour
{
    [Header("武器设置")]
    public GameObject weaponPrefab;          // 丢出去的武器预制体
    public float throwSpeed = 18f;           // 飞行速度
    public float maxDistance = 8f;           // 超过这个距离没打到就自动返回
    public KeyCode throwKey = KeyCode.R;     // 扔武器
    public KeyCode recallKey = KeyCode.F;    // 召回武器

    [Header("八向输入")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis   = "Vertical";

    [Header("投掷偏移")]
    [Tooltip("从角色中心沿投掷方向偏移多少距离生成武器")]
    public float throwOffset = 0.7f;

    [HideInInspector]
    public bool hasWeapon = true;            // 角色当前是否“手里有武器”

    PlayerPlatformer platformer;             // 用于重置 dash
    SpriteRenderer sr;
    Vector2 lastMoveDir = Vector2.right;     // 记住最后一次水平方向
    ThrownWeapon currentWeapon;              // 场上那一把武器

    void Awake()
    {
        platformer = GetComponent<PlayerPlatformer>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 记录输入方向，用来确定八向
        float x = Input.GetAxisRaw(horizontalAxis);
        float y = Input.GetAxisRaw(verticalAxis);
        Vector2 rawDir = new Vector2(x, y);

        if (Mathf.Abs(x) > 0.01f)
        {
            lastMoveDir = new Vector2(Mathf.Sign(x), 0f);
        }

        // 扔武器
        if (Input.GetKeyDown(throwKey))
        {
            TryThrowWeapon(rawDir);
        }

        // 召回武器
        if (Input.GetKeyDown(recallKey))
        {
            RecallWeapon();
        }
    }

    void TryThrowWeapon(Vector2 rawDir)
    {
        if (!hasWeapon) return;
        if (weaponPrefab == null) return;

        // 选择方向：优先当前输入，其次最后的水平面向
        Vector2 chosen =
            rawDir.sqrMagnitude > 0.01f ? rawDir :
            (lastMoveDir.sqrMagnitude > 0 ? lastMoveDir : Vector2.right);

        Vector2 dir = QuantizeToEightDir(chosen);

        Vector3 spawnPos = transform.position + (Vector3)(dir.normalized * throwOffset);

        GameObject go = Instantiate(weaponPrefab, spawnPos, Quaternion.identity);

        ThrownWeapon weapon = go.GetComponent<ThrownWeapon>();
        if (weapon != null)
        {
            weapon.Init(this, platformer, dir, throwSpeed, maxDistance);
            currentWeapon = weapon;
        }

        hasWeapon = false; // 手里暂时没有武器
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
    }

    /// 被武器通知“玩家在空中抓到武器，需要重置 dash”
    public void OnWeaponCaughtInAir()
    {
        hasWeapon = true;
        currentWeapon = null;

        if (platformer != null)
        {
            platformer.ResetDashState();
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

    public void ResetWeaponOnDeath()
    {
        if (currentWeapon != null)
        {
            currentWeapon.ForceDestory();
            currentWeapon = null;
        }

        hasWeapon = true;
    }
}
