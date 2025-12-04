using UnityEngine;
using UnityEngine.UI; 

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("按键设置")]
    public KeyCode nextKey = KeyCode.Space;   // 下一句
    public KeyCode skipKey = KeyCode.Escape;  // 跳过整个对话

    [Header("对话 UI")]
    public GameObject dialoguePanel;      // 一个 Panel，显示对话
    public Text dialogueText;            // 显示台词的 Text
    public Text nameText;                // 显示说话人名字

    string[] currentLines;
    int currentIndex;
    bool isActive;

    PlayerPlatformer currentPlayer;  // 当前对话时的玩家（用来禁用操作）
    string speakerName;              // NPC 名字

    public bool IsTalking => isActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 确保一开始对话 UI 是关掉的
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(nextKey))
        {
            ShowNextLine();
        }

        if (Input.GetKeyDown(skipKey))
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 开始一段对话（没有 NPC 名字）
    /// </summary>
    public void StartDialogue(string[] lines, PlayerPlatformer player)
    {
        StartDialogue(lines, player, null);
    }

    /// <summary>
    /// 开始一段对话（带 NPC 名字）
    /// </summary>
    public void StartDialogue(string[] lines, PlayerPlatformer player, string npcName)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("DialogueManager: 对话内容为空");
            return;
        }

        // 如果已经在说话，先结束上一段
        if (isActive)
        {
            EndDialogue();
        }

        currentLines = lines;
        currentIndex = 0;
        currentPlayer = player;
        speakerName = npcName;
        isActive = true;

        // 禁用玩家操作
        if (currentPlayer != null)
        {
            currentPlayer.SetControlEnabled(false);
        }

        // 打开对话面板
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        UpdateNameUI();
        ShowCurrentLine();
    }

    void UpdateNameUI()
    {
        if (nameText == null) return;

        if (string.IsNullOrEmpty(speakerName))
            nameText.text = "";
        else
            nameText.text = speakerName;
    }

    void ShowCurrentLine()
    {
        if (currentLines == null || currentLines.Length == 0) return;
        if (currentIndex < 0 || currentIndex >= currentLines.Length) return;

        string line = currentLines[currentIndex];

        // 更新 UI
        if (dialogueText != null)
        {
            dialogueText.text = line;
        }
        else
        {
            // 暂时用 Console 输出
            Debug.Log("[对话] " + line);
        }
    }

    void ShowNextLine()
    {
        currentIndex++;
        if (currentIndex >= currentLines.Length)
        {
            EndDialogue();
        }
        else
        {
            ShowCurrentLine();
        }
    }

    public void EndDialogue()
    {
        if (!isActive) return;

        isActive = false;

        // 关闭 UI
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // 恢复玩家操作
        if (currentPlayer != null)
        {
            currentPlayer.SetControlEnabled(true);
        }

        currentLines = null;
        currentIndex = 0;
        currentPlayer = null;
        speakerName = null;

        Debug.Log("[对话结束]");
    }
}
