using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [Tooltip("后期指定出生点")]
    public Transform spawnPoint;

    Vector3 _spawnPos;
    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
       _spawnPos = spawnPoint ? spawnPoint.position : transform.position; 
    }

    public void Respawn()
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        transform.position = _spawnPos;
    }

    void onDrawGizmosSelected()
    {
        Vector3 p = (spawnPoint ? spawnPoint.position : transform.position);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p,0.25f);
    }
}
