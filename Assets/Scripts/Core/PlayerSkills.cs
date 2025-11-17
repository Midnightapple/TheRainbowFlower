using UnityEngine;

public class PlayerSkills : MonoBehaviour
{
    [Header("Shield")]
    [Tooltip("当前护盾层数（可无限）")]
    public int shieldCount = 0;

    /// 增加护盾层数（拾取/购买时调） 
    public void AddShields(int amount = 1)
    {
        if (amount <= 0) return;
        shieldCount += amount;
        // TODO: 可在这里触发 UI 事件或特效
    }

    /// 尝试消耗一层护盾；成功返回 true，失败(false)表示没有护盾 
    public bool TryConsumeShield()
    {
        if (shieldCount > 0)
        {
            shieldCount--;
            // TODO: 护盾破裂特效/音效
            return true;
        }
        return false;
    }

    /// 是否至少有一层护盾 
    public bool HasShield => shieldCount > 0;

    [Header("Smite Skill")]
    public KeyCode attackKey = KeyCode.E;     // 按哪个键释放技能
    public int attackCharges = 0;             // 当前可用次数/能量
    public int attackCost = 1;                // 每次消耗多少点
    public float attackRange = 5f;            // 技能半径
    public LayerMask enemyMask;               // 敌人所在 Layer（比如 EnemySolid）

    void Update()
    {
        // 监听主动技能按键
        if (Input.GetKeyDown(attackKey))
        {
            TryUseAttackSkill();
        }
    }

    // 外部（比如拾取/商店）可以调用这个加能量
    public void AddAttackCharges(int amount)
    {
        attackCharges += amount;
        if (attackCharges < 0) attackCharges = 0;
    }



    void TryUseAttackSkill()
    {
        // 1. 检查是否有足够能量
        if (attackCharges < attackCost)
        {
            // TODO: 可以在这里提示“能量不足”
            return;
        }

        // 2. 找到范围内所有敌人碰撞体
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            attackRange,
            enemyMask
        );

        if (hits.Length == 0)
        {
            // 范围内没有敌人，就不消耗能量
            return;
        }

        // 3. 在这些敌人里选一只（这里选最近的那一个）
        EnemyBase target = null;
        float bestDistSqr = float.MaxValue;

        foreach (var col in hits)
        {
            EnemyBase e = col.GetComponentInParent<EnemyBase>();
            if (e == null) continue;
            if (!e.gameObject.activeInHierarchy) continue; // 已经死了的忽略

            float d = (e.transform.position - transform.position).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                target = e;
            }
        }

        if (target == null)
        {
            // 没找到有效敌人，不消耗
            return;
        }

        // 4. 消耗能量并击杀目标
        attackCharges -= attackCost;
        target.Die();   // 调用 EnemyBase 里的 Die()
        // TODO: 这里可以播放一个范围技能特效/音效
    }

}
