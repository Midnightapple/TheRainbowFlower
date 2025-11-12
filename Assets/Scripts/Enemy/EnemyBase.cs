using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    [Header("Base")]
    public int health = 1;
    public float hitCooldown = 0.25f;

    [Header("Knockback (shield)")]
    public float knockStep = 0.9f;          // 护盾命中时后退的一小步
    public float skin = 0.06f;              // 贴墙留一点皮肤，避免嵌入
    public LayerMask solidMask;             // 指向 Ground 等“固体层”

    float lastHitTime = -999f;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 基础刚体设置：与地形碰撞，不受重力
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    // —— 用“触发器”处理玩家交互，确保冲刺不会被实体挡住 ——
    void OnTriggerEnter2D(Collider2D other) { HandlePlayerContact(other); }
    void OnTriggerStay2D(Collider2D other)  { HandlePlayerContact(other); }

    void HandlePlayerContact(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < lastHitTime + hitCooldown) return;

        // 读取玩家组件
        var life   = other.GetComponent<PlayerLife>();
        var skills = other.GetComponent<PlayerSkills>();
        
        bool isDashing = false;
        var ctrl = other.GetComponent<PlayerPlatformer>();
        if (ctrl != null)
        {    
            isDashing = ctrl.isDashing;
        }

        // ① 冲刺命中：敌人直接死亡（玩家不被阻挡）
        if (isDashing)
        {
            Die();
            lastHitTime = Time.time;
            return;
        }

        // ② 非冲刺，尝试用护盾抵消，并让敌人只后退“一小步并停住”
        if (skills != null && skills.TryConsumeShield())
        {
            Vector2 delta = (Vector2)(transform.position - other.bounds.center);
            float sx = Mathf.Sign(delta.x != 0 ? delta.x : 1f);
            Vector2 dir = new Vector2(sx, 0f); // 纯水平

            Vector2 start = rb.position;
            float step = knockStep; // 0.8~1.0
            RaycastHit2D hit = Physics2D.Raycast(start, dir, knockStep + skin, solidMask); // solidMask 只勾 Ground
            if (hit.collider) step = Mathf.Max(0f, hit.distance - skin);

            rb.MovePosition(start + dir * step);
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            lastHitTime = Time.time;
            return;
        }


        // ③ 没护盾：玩家死亡
        if (life != null)
        {
            life.Die(KillReason.Enemy);
            lastHitTime = Time.time;
        }
    }

    // 护盾命中 → 从玩家反方向后退一小步，立即停止；含“固体”预判，不会穿墙/地面
    void KnockbackOneStep(Vector2 fromPlayerPos)
    {
        Vector2 dir = (Vector2)transform.position - fromPlayerPos;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        Vector2 start  = rb.position;
        float   dist   = knockStep + skin;
        Vector2 target = start + dir * knockStep;

        // 预判是否会撞到地形（Ground 等）
        RaycastHit2D hit = Physics2D.Raycast(start, dir, dist, solidMask);
        if (hit.collider) target = hit.point - dir * skin;

        rb.MovePosition(target);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public virtual void Die()
    {
        // TODO: 掉落/特效/音效
        Destroy(gameObject);
    }
}

