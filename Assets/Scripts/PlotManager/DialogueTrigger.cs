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

    [Header("技能充能")]
    [Tooltip("增加多少层护盾")]
    public int shieldReward = 0;

    [Tooltip("增加多少点攻击技能能量")]
    public int attackChargeReward = 0;

    [Tooltip("增加多少次平台技能使用次数")]
    public int platformChargeReward = 0;

    public bool rewardOnce = true;

    bool rewardGiven = false;


    bool playerInRange;
    PlayerPlatformer cachedPlayer;
    PlayerSkills cachedSkills;

    bool startedThisDialogue = false;

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
        cachedSkills = null;

        startedThisDialogue = false;
    }

    void Update()
    {
        if (!playerInRange) return;
        if (DialogueManager.Instance == null) return;

        // 避免对话正在进行时重复触发
        if (DialogueManager.Instance.IsTalking) return;

        if (playerInRange && !DialogueManager.Instance.IsTalking)
        {
            if (Input.GetKeyDown(interactKey))
            {
                if (cachedPlayer == null)
                {
                    cachedPlayer = FindFirstObjectByType<PlayerPlatformer>();
                    cachedSkills = cachedPlayer ? cachedPlayer.GetComponent<PlayerSkills>() : null;
                }

                DialogueManager.Instance.StartDialogue(lines, cachedPlayer, npcName);
                startedThisDialogue = true;
            }
        }

        // 如果这次对话是这个 NPC 开的，并且对话刚结束
        if (startedThisDialogue && !DialogueManager.Instance.IsTalking)
        {
            if (!rewardGiven || !rewardOnce)
            {
                GiveRewardIfAny();
                if (rewardOnce)
                {
                    rewardGiven = true;
                }
            }

            // 这次对话生命周期结束
            startedThisDialogue = false;
        }
    }

    void GiveRewardIfAny()
    {
        if (cachedSkills == null)
        {
            cachedSkills = FindFirstObjectByType<PlayerSkills>();
            if (cachedSkills == null)
            {
                Debug.LogWarning("DialogueTrigger: 找不到 PlayerSkills");
                return;
            }
        }

        if (shieldReward > 0)
        {
            cachedSkills.AddShields(shieldReward);
            Debug.Log($"[NPC奖励] 护盾 +{shieldReward}，当前护盾 {cachedSkills.shieldCount}");
        }

        if (attackChargeReward > 0)
        {
            cachedSkills.AddAttackCharges(attackChargeReward);
            Debug.Log($"[NPC奖励] 攻击 +{attackChargeReward}，当前攻击 {cachedSkills.attackCharges}");
        }

        if (platformChargeReward > 0)
        {
            cachedSkills.AddPlatformCharges(platformChargeReward);
            Debug.Log($"[NPC奖励] 平台次数 +{platformChargeReward}，当前次数 {cachedSkills.platformCharges}");
        }
    }
}
