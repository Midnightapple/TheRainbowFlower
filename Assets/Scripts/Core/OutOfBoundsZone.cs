using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class OutOfBoundsZone : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(!other.CompareTag("Player")) return;
        var respawn = other.GetComponent<PlayerRespawn>();
        if (respawn != null) respawn.Respawn();
    }
}
