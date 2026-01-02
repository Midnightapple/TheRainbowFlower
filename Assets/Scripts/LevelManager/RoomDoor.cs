using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [Tooltip("传送到哪一个点（同一大场景里的小关入口）")]
    public Transform targetSpawnPoint;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (targetSpawnPoint == null)
        {
            Debug.LogWarning("RoomDoor 没有指定 targetSpawnPoint！");
            return;
        }

        // 把玩家移动到目标小关入口
        other.transform.position = targetSpawnPoint.position;

        // 清一下 dash 状态
        var player = other.GetComponent<PlayerPlatformer>();
        if (player != null)
        {
            player.ResetDashState();
        }
    }
}
