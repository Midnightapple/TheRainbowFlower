using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("对话内容")]
    [TextArea(2, 5)]
    public string[] lines;          // 在 Inspector 里写几句

    [Header("说话人名字")]
    public string npcName;

    [Header("按键设置")]
    public KeyCode interactKey = KeyCode.F;  // 按 F 开始对话

    bool playerInRange;
    PlayerPlatformer cachedPlayer;

    void Reset()
    {
        // 自动设为 trigger
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        cachedPlayer = other.GetComponent<PlayerPlatformer>();

        Debug.Log("按 F 和 NPC 说话"); // 以后可以改成 UI 提示
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        cachedPlayer = null;
    }

    void Update()
    {
        if (!playerInRange) return;
        if (DialogueManager.Instance == null) return;

        // 避免对话正在进行时重复触发
        if (DialogueManager.Instance.IsTalking) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (cachedPlayer == null)
            {
                // 尝试再找一次
                cachedPlayer = FindFirstObjectByType<PlayerPlatformer>();
            }

            DialogueManager.Instance.StartDialogue(lines, cachedPlayer, npcName);
        }
    }
}
