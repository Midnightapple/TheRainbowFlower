using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class SpikeTriangle : MonoBehaviour
{
    [Tooltip("勾选=触发器（不挡路）；不勾选=实体碰撞（会挡路）")]
    public bool useTrigger = true;

    [Tooltip("玩家的Tag")]
    public string playerTag = "Player";

    PolygonCollider2D poly;

    void Reset()  { Setup(); }
    void Awake()  { Setup(); }
#if UNITY_EDITOR
    void OnValidate() { if (!Application.isPlaying) Setup(); }
#endif

    void Setup()
    {
        poly = GetComponent<PolygonCollider2D>();
        poly.isTrigger = useTrigger;

        // 画一个“向上”的单位三角形；大小用 Transform 的 Scale 调整
        Vector2[] verts = new Vector2[]
        {
            new Vector2(-0.5f, 0f),
            new Vector2( 0.5f, 0f),
            new Vector2( 0.0f, 1f)
        };
        poly.pathCount = 1;
        poly.SetPath(0, verts);
    }

    // 触发器版
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (!other.CompareTag(playerTag)) return;
        Hit(other.gameObject);
    }

    // 实体碰撞版
    void OnCollisionEnter2D(Collision2D other)
    {
        if (useTrigger) return;
        if (!other.collider.CompareTag(playerTag)) return;
        Hit(other.collider.gameObject);
    }

    void Hit(GameObject player)
    {
        
        var life = player.GetComponent<PlayerLife>();
        if (life != null) { life.Die(KillReason.Hazard); return; }
    }
}