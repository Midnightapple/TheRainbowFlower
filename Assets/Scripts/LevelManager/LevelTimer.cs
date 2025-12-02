using UnityEngine;
using UnityEngine.UI;   

public class LevelTimer : MonoBehaviour
{
    [Header("计时设置")]
    [Tooltip("总时长（sec)")]
    public float levelDuration = 120f;

    [Tooltip("打开/关闭计时界面")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("时间到后传送的位置")]
    public Transform firstRoomSpawn;

    [Header("UI")]
    public GameObject timerPanel;   // 一个 Panel
    public Text timerText;          // 显示 00:00 的 Text

    float timeLeft;
    bool isRunning;

    void Start()
    {
        timeLeft = levelDuration;
        isRunning = true;

        // 进场景时关掉面板
        if (timerPanel != null)
            timerPanel.SetActive(false);
    }

    void Update()
    {
        // --- 倒计时 ---
        if (isRunning)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                isRunning = false;
                OnTimeUp();
            }

            if (timerPanel != null && timerPanel.activeSelf)
            {
                UpdateTimerUI();
            }
        }

        // --- 打开/关闭计时器面板 ---
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleTimerPanel();
        }

    }

    // 传送回第一个小关出生点
    void OnTimeUp()
    {
        PlayerRespawn respawn = FindFirstObjectByType<PlayerRespawn>();
        if (respawn == null)
        {
            Debug.LogWarning("LevelTimer: 找不到 PlayerRespawn");
            return;
        }

        // 把重生点切到第一个小关
        if (firstRoomSpawn != null)
        {
            respawn.SetSpawnPoint(firstRoomSpawn);
        }

        // 直接传送
        respawn.Respawn();

        // Cutscene
        // var cutscene = FindFirstObjectByType<CutsceneManager>();
        // if (cutscene != null) cutscene.PlayTimeoutCutscene();
    }

    // 打开/关闭计时界面
    void ToggleTimerPanel()
    {
        if (timerPanel == null) return;

        bool newState = !timerPanel.activeSelf;
        timerPanel.SetActive(newState);

        if (newState)
        {
            UpdateTimerUI(); // 打开时顺便刷新一次文本
        }
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        timerText.text = FormatTime(timeLeft);
    }

    // 把秒数格式化成 "MM:SS"
    string FormatTime(float t)
    {
        if (t < 0f) t = 0f;
        int totalSeconds = Mathf.CeilToInt(t);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // 调用
    public float GetTimeLeft()
    {
        return timeLeft;
    }
}
