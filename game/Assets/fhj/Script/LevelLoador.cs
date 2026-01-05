using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 1f;
    // 可选：指定只有特定标签的物体碰撞时才触发（比如主角标签是"Player"）
    public string triggerTag = "Player";
    // 碰撞后延迟切换场景的时间（秒）
    public float collisionDelayTime = 1f;

    private Coroutine loadLevelCoroutine;
    // 标记是否已经触发碰撞（防止重复触发）
    private bool isCollided = false;

    // 2D触发碰撞检测（因为你用的是Box Collider 2D且勾选了Is Trigger）
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 只让指定标签的物体触发 + 未触发过碰撞 + 无正在运行的协程
        if (other.CompareTag(triggerTag) && !isCollided && loadLevelCoroutine == null)
        {
            isCollided = true; // 标记已触发，防止重复
            // 启动延迟加载协程
            loadLevelCoroutine = StartCoroutine(DelayLoadNextLevel());
        }
    }

    // 碰撞后延迟加载下一个场景
    private IEnumerator DelayLoadNextLevel()
    {
        // 等待碰撞延迟时间（1秒）
        yield return new WaitForSeconds(collisionDelayTime);

        // 延迟结束后执行正常的场景加载逻辑
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextLevelIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("已是最后一个场景！");
            isCollided = false; // 重置标记
            loadLevelCoroutine = null;
            yield break;
        }

        // 触发过渡动画
        transition?.SetTrigger("Start");
        // 等待过渡动画完成
        yield return new WaitForSeconds(transitionTime);

        try
        {
            SceneManager.LoadScene(nextLevelIndex);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载失败: {e.Message}");
            // 加载失败时重置标记，允许再次触发
            isCollided = false;
        }

        // 重置协程和碰撞标记
        loadLevelCoroutine = null;
        isCollided = false;
    }

    // 可选：如果物体离开碰撞区域，取消延迟加载（可选功能）
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag) && isCollided)
        {
            // 停止延迟加载协程
            if (loadLevelCoroutine != null)
            {
                StopCoroutine(loadLevelCoroutine);
                loadLevelCoroutine = null;
            }
            isCollided = false; // 重置标记
        }
    }

    // 保留原有的LoadLevel方法（兼容其他调用）
    public void LoadLevel(int levelIndex)
    {
        if (loadLevelCoroutine != null) StopCoroutine(loadLevelCoroutine);
        loadLevelCoroutine = StartCoroutine(LoadLevelCoroutine(levelIndex));
    }

    // 原有的加载协程（备用）
    private IEnumerator LoadLevelCoroutine(int levelIndex)
    {
        transition?.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        try
        {
            SceneManager.LoadScene(levelIndex);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载失败: {e.Message}");
        }
        loadLevelCoroutine = null;
        isCollided = false;
    }
}