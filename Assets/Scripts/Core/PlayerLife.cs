using UnityEngine;
public enum KillReason {Fall, Hazard}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerLife : MonoBehaviour
{
    bool isDead = false;
    PlayerRespawn respawn;

    void Awake()
    {
        respawn = GetComponent<PlayerRespawn>();
    }

    public void Die(KillReason reason)
    {
        if (isDead) return;
        isDead = true;
        respawn.Respawn();
        isDead = false;
    }
}
