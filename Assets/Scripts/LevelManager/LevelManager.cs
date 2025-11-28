using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    [Tooltip("需要一起加载的关卡场景名")]
    public string[] roomSceneNames;

    private IEnumerator Start()
    {
        // 启动游戏时，把所有小关卡 Additive 加载进来
        foreach (var sceneName in roomSceneNames)
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                while (!op.isDone)
                {
                    yield return null;
                }
            }
        }

        // 全部加载完之后，游戏正式开始
        Debug.Log("All room scenes loaded.");
    }
}
