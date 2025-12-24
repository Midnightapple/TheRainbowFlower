using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class DisappearingPlatform : MonoBehaviour
{
    [Header("时间设置")]
    [Tooltip("玩家站在平台上多久后平台消失（秒）")]
    public float timeBeforeDisappear = 1.0f;

    [Tooltip("平台消失后，过多久重新出现（秒）")]
    public float respawnDelay = 2.0f;

    [Header("提示效果")]
    [Tooltip("开始要消失时的平台颜色变化")]
    public Color warningColor = Color.red;

    Color _originalColor;
    SpriteRenderer _sr;
    Collider2D _col;

    bool _isCrumbling = false;   // 正在计时准备消失
    bool _isGone = false;        // 已经消失
    Coroutine _crumbleCo;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
        _col.isTrigger = false;               // 当普通地面用
        _originalColor = _sr.color;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 只有玩家踩上来才触发
        if (!collision.collider.CompareTag("Player")) return;

        // 已经在计时或已经消失就不再重复
        if (_isCrumbling || _isGone) return;

        _crumbleCo = StartCoroutine(CrumbleRoutine());
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        // 玩家在平台还没消失之前离开了，取消这次计时，恢复正常
        if (_isCrumbling && !_isGone)
        {
            if (_crumbleCo != null)
                StopCoroutine(_crumbleCo);

            _isCrumbling = false;
            _sr.color = _originalColor;
        }
    }

    IEnumerator CrumbleRoutine()
    {
        _isCrumbling = true;

        // 在计时过程中先变颜色，告诉玩家要塌了
        _sr.color = warningColor;

        float timer = 0f;
        while (timer < timeBeforeDisappear)
        {
            timer += Time.deltaTime;
            // 这里你以后可以加抖动、闪烁等效果
            yield return null;
        }

        // 正式消失
        _isCrumbling = false;
        _isGone = true;

        _sr.enabled = false;      // 看不见
        _col.enabled = false;     // 没有碰撞

        // 等待一段时间再回来
        yield return new WaitForSeconds(respawnDelay);

        // 重新出现
        _sr.enabled = true;
        _col.enabled = true;
        _sr.color = _originalColor;
        _isGone = false;
    }
}
