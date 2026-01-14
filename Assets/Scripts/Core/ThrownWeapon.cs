using UnityEngine;

public class ThrownWeapon : MonoBehaviour
{
    public enum WeaponState { Idle, Flying, FallingOrStuck, Returning }

    [Header("碰撞设置")]
    public LayerMask enemyMask;        // 敌人 Layer（带 EnemyBase）
    public LayerMask environmentMask;  // 墙、地面、天花板 Layer
    public float returnSpeed = 20f;    // 召回 / 自动返回速度
    public float gravityWhenFlying = 0f;   // 飞行时重力（0 = 完全直线）
    public float gravityWhenFalling = 5f;  // 打到敌人后开始下落的重力

    PlayerThrowWeapon ownerWeapon;
    PlayerPlatformer ownerPlayer;
    Rigidbody2D rb;
    Collider2D col;

    Vector3 startPos;
    Vector2 flyDir;
    float flySpeed;
    float maxDistance;

    WeaponState state = WeaponState.Idle;

    public void Init(
        PlayerThrowWeapon ownerWeapon,
        PlayerPlatformer ownerPlayer,
        Vector2 dir,
        float speed,
        float maxDistance)
    {
        this.ownerWeapon = ownerWeapon;
        this.ownerPlayer = ownerPlayer;
        this.flyDir      = dir.normalized;
        this.flySpeed    = speed;
        this.maxDistance = maxDistance;

        startPos = transform.position;
        state    = WeaponState.Flying;

        // 让武器细长边朝飞行方向
        if (flyDir.sqrMagnitude > 0.0001f)
        {
            // 如果贴图是横向（朝右）
            transform.right = flyDir;
            // 如果贴图是纵向（朝上），就改成：
            // transform.up = flyDir;
        }

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<Collider2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityWhenFlying;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = flyDir * flySpeed;
    }

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    void Update()
    {
        if (ownerWeapon == null || ownerPlayer == null) return;

        switch (state)
        {
            case WeaponState.Flying:
                CheckMaxDistance();
                break;

            case WeaponState.Returning:
                ReturnUpdate();
                break;
        }
    }

    void CheckMaxDistance()
    {
        float distSqr = (transform.position - startPos).sqrMagnitude;
        if (distSqr > maxDistance * maxDistance)
        {
            BeginReturn();
        }
    }

    void ReturnUpdate()
    {
        if (ownerPlayer == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 target = ownerPlayer.transform.position;
        Vector3 newPos = Vector3.MoveTowards(transform.position, target, returnSpeed * Time.deltaTime);

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;  
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        transform.position = newPos;

        if ((transform.position - target).sqrMagnitude < 0.05f * 0.05f)
        {
            ownerWeapon.OnWeaponReturned();
            Destroy(gameObject);
        }
    }

    public void BeginReturn()
    {
        if (state == WeaponState.Returning) return;
        state = WeaponState.Returning;
    }

    // 碰撞

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    void HandleHit(Collider2D other)
    {
        if (ownerWeapon == null || ownerPlayer == null) return;

        GameObject obj = other.gameObject;
        int otherLayer = obj.layer;

        // 玩家拾取 / 空中抓 / 挂在武器上
        if (obj.CompareTag("Player"))
        {
            bool inAir = !ownerPlayer.IsOnGround;

            // 武器已经卡在墙 / 天花板上，同时玩家在空中
            if (state == WeaponState.FallingOrStuck && inAir)
            {
                // 如果此时按着投掷键（J），则挂在武器上，不立即取下
                if (Input.GetKey(ownerWeapon.throwKey))
                {
                    ownerWeapon.StartHookFromWeapon(this);
                    // 不销毁武器，让它继续挂在墙上
                    return;
                }
                else
                {
                    // 没按 J，按老逻辑空中接住武器，重置 dash
                    ownerWeapon.OnWeaponCaughtInAir();
                    Destroy(gameObject);
                    return;
                }
            }

            // 其它情况（比如武器返回途中撞到玩家，或地上走过去捡）
            ownerWeapon.OnWeaponReturned();
            Destroy(gameObject);
            return;
        }

        // 打到敌人
        if (IsInLayerMask(otherLayer, enemyMask))
        {
            EnemyBase enemy = obj.GetComponentInParent<EnemyBase>();
            if (enemy != null) enemy.Die();

            state = WeaponState.FallingOrStuck;

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;   
                rb.gravityScale = gravityWhenFalling;
                rb.linearVelocity = rb.linearVelocity;    // 保留一点水平速度
            }
            return;
        }

        // 撞到环境卡住
        if (IsInLayerMask(otherLayer, environmentMask))
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic; 
                rb.gravityScale = 0f;
            }

            state = WeaponState.FallingOrStuck;
            return;
        }
    }

    bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    public void ForceDestroy()
    {
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}
