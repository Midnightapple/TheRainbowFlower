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
}
