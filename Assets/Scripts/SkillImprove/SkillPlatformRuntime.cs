using System.Collections.Generic;
using UnityEngine;

public class SkillPlatformRuntime : MonoBehaviour
{
    // 记录所有“技能生成的平台”实例
    static List<SkillPlatformRuntime> all = new List<SkillPlatformRuntime>();

    void OnEnable()
    {
        if (!all.Contains(this))
            all.Add(this);
    }

    void OnDisable()
    {
        all.Remove(this);
    }

    // 玩家死亡时调用：清除所有技能平台
    public static void ResetAll()
    {
        // 倒序销毁，避免修改列表时索引乱掉
        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (all[i] != null)
                Object.Destroy(all[i].gameObject);
        }
        all.Clear();
    }
}
