using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class AirVisiblePlatform : MonoBehaviour
{
    [Tooltip("如果不手动指定玩家，会自动在场景里找第一个 PlayerPlatformer")]
    public PlayerPlatformer player;

    SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        // 确保平台一直有碰撞（不可见时也能站上去）
        var col = GetComponent<Collider2D>();
        col.isTrigger = false;

        // 如果没在 Inspector 手动拖 player，就自动找一次
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerPlatformer>();
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 只要玩家不在地面”，平台就可见
        bool shouldBeVisible = !player.IsOnGround;

        if (_sr.enabled != shouldBeVisible)
        {
            _sr.enabled = shouldBeVisible;
        }

    }
}
