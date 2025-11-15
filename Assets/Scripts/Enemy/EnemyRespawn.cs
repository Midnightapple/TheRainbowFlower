using UnityEngine;

public class EnemyRespawn : MonoBehaviour
{
    Vector3 _startPos;
    Quaternion _startRot;

    Rigidbody2D _rb;

    public bool canRespawn = true;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 记录敌人一开始的 位置 + 旋转
        _startPos = transform.position;
        _startRot = transform.rotation;
    }

    // 在玩家死亡后调用：把敌人送回初始状态
    public void RespawnNow()
    {
        if (!canRespawn) return;
        
        // 先激活（如果被隐藏了）
        gameObject.SetActive(true);

        // 清空速度
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }

        // 回到起点
        transform.position = _startPos;
        transform.rotation = _startRot;
    }
}
