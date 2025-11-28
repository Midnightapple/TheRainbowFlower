using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [Tooltip("当前重生点")]
    public Transform spawnPoint;

    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    // 在其他脚本里调用，动态更新重生点
    public void SetSpawnPoint(Transform newSpawn)
    {
        spawnPoint = newSpawn;
    }

    public void Respawn()
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("PlayerRespawn：spawnPoint 还没设置");
            return;
        }

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        transform.position = spawnPoint.position;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 p = (spawnPoint ? spawnPoint.position : transform.position);
        Gizmos.DrawWireSphere(p, 0.25f);
    }
}
