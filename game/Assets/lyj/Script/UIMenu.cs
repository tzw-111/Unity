using UnityEngine;
using UnityEngine.SceneManagement; // 加载场景需要的命名空间
using TMPro; // TMP组件需要的命名空间

public class UIMenu : MonoBehaviour
{
    // 开始游戏按钮点击事件
    public void OnStartGameButtonClick()
    {
        // 加载名为"GameScene"的场景（需先在Build Settings中添加该场景）
        // 替换成你自己的游戏场景名称
        SceneManager.LoadScene("SC Demo");
    }

    // 退出游戏按钮点击事件
    public void OnQuitGameButtonClick()
    {
        // 编辑器中运行时，退出播放模式；打包后，退出应用
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}