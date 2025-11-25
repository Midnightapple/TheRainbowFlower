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
        if (!other.CompareTag("Player")) return;
        var life = other.GetComponent<PlayerLife>();
        if (life != null) life.Die(KillReason.Fall);
    }
}
