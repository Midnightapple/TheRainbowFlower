using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomSpawnSetter : MonoBehaviour
{
    [Tooltip("这个小关的出生点")]
    public Transform roomSpawnPoint;

    void Reset()
    {
        // 自动变成 trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerRespawn pr = other.GetComponent<PlayerRespawn>();
        if (pr != null && roomSpawnPoint != null)
        {
            pr.SetSpawnPoint(roomSpawnPoint);
            // Debug.Log("更新复活点为：" + roomSpawnPoint.name);
        }
    }
}
