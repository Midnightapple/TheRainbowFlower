using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    [Header("Base")]
    public int health = 1;
    public float hitCooldown = 0.25f;

    [Header("Knockback (shield)")]
    public float knockStep = 0.9f;
    public float skin = 0.06f;
    public LayerMask solidMask;
    
    [Header("Gravity")]
    public bool useGravity = true;          
    public float gravityScale = 3f;        
    
    float lastHitTime = -999f;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = useGravity ? gravityScale : 0f; 
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void OnTriggerEnter2D(Collider2D other) { HandlePlayerContact(other); }
    void OnTriggerStay2D(Collider2D other)  { HandlePlayerContact(other); }

    void HandlePlayerContact(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        // 移除CD检查，让冲刺能立即杀死敌人
        // 只在非冲刺情况下才检查CD
        
        var life   = other.GetComponent<PlayerLife>();
        var skills = other.GetComponent<PlayerSkills>();
        bool isDashing = false;
        var ctrl = other.GetComponent<PlayerPlatformer>();
        if (ctrl != null)
        {
            isDashing = ctrl.isDashing;
        }

        // ① 冲刺命中：敌人直接死亡（无CD限制）
        if (isDashing)
        {
            Die();
            return;
        }

        // 只有非冲刺时才检查CD
        if (Time.time < lastHitTime + hitCooldown) return;

        // ② 非冲刺，尝试用护盾抵消
        if (skills != null && skills.TryConsumeShield())
        {
            KnockbackOneStep(other.bounds.center);
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

    void KnockbackOneStep(Vector2 fromPlayerPos)
    {
        Vector2 dir = (Vector2)transform.position - fromPlayerPos;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        // 限制击退方向只能是水平的，防止敌人被打进地里
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;  // 如果纯垂直碰撞，默认右击退
        dir.Normalize();

        Vector2 start  = rb.position;
        float   dist   = knockStep + skin;
        Vector2 target = start + dir * knockStep;

        // 预判是否会撞到地形
        RaycastHit2D hit = Physics2D.Raycast(start, dir, dist, solidMask);
        if (hit.collider)
        {
            target = hit.point - dir * skin;
        }

        rb.MovePosition(target);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public virtual void Die()
    {
        // TODO: 掉落/特效/音效
        gameObject.SetActive(false);
    }
}