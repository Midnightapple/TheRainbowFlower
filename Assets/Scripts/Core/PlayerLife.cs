using UnityEngine;
public enum KillReason {Fall, Hazard}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerLife : MonoBehaviour
{
    [Header("Shield")]
    public int shieldCount = 0;
    
    bool isDead = false;
    PlayerRespawn respawn;

    void Awake()
    {
        respawn = GetComponent<PlayerRespawn>();
    }

    public void AddShields(int amount = 1)
    {
        if (amount <= 0) return;
        shieldCount += amount;
    }

    bool TryConsumeShield()
    {
        if (shieldCount > 0)
        {
            shieldCount--;
            return true;
        }
        return false;
    }

    public void Die(KillReason reason)
    {
        if (reason != KillReason.Fall && TryConsumeShield()) 
            return;
        
        if (isDead) return;
        isDead = true;
        respawn.Respawn();
        isDead = false;
    }
}
