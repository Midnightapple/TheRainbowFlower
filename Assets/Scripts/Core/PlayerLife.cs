using UnityEngine;
public enum KillReason {Fall, Hazard, Enemy}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerLife : MonoBehaviour
{
    bool isDead = false;
    PlayerRespawn respawn;
    PlayerSkills skills;

    void Awake()
    {
        respawn = GetComponent<PlayerRespawn>();
        skills = GetComponent<PlayerSkills>();
    }



    public void Die(KillReason reason)
    {
        // 1) 坠落：不消耗护盾，直接复位
        if (reason == KillReason.Fall)
        {
            if (isDead) return;
            isDead = true;
            respawn.Respawn();
            ResetAllEnemies();
            isDead = false;
            return;
        }

        // 2) 敌人/陷阱：若有护盾 → 抵消这次伤害（不死亡）
        if (skills != null && skills.TryConsumeShield())
            return;

        // 3) 没护盾 → 正常死亡并复位
        if (isDead) return;
        isDead = true;
        respawn.Respawn();
        ResetAllEnemies();
        isDead = false;
    }

    void ResetAllEnemies()
    {
        // 包括被 SetActive(false) 的敌人
        EnemyRespawn[] all =
            FindObjectsByType<EnemyRespawn>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (var e in all)
        {
            e.RespawnNow();
        }
    }

}
