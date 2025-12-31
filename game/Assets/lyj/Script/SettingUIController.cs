using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 系统设置面板控制器：控制齿轮按钮、设置面板显示隐藏、退出功能
/// </summary>
public class SettingUIController : MonoBehaviour
{
    [Header("UI引用")]
    [Tooltip("齿轮设置按钮（拖入SettingButton）")]
    public UnityEngine.UI.Button settingButton;
    [Tooltip("系统设置面板（拖入SettingPanel）")]
    public GameObject settingPanel;
    [Tooltip("主菜单场景名（可选，若退出是返回主菜单则填写）")]
    public string mainMenuSceneName = "MainMenu";

    [Header("功能选择")]
    [Tooltip("true=退出游戏，false=返回主菜单")]
    public bool isQuitGame = true;

    private void Awake()
    {
        // 初始化：隐藏设置面板
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        // 绑定齿轮按钮的点击事件
        if (settingButton != null)
        {
            settingButton.onClick.AddListener(ToggleSettingPanel);
        }
        else
        {
            Debug.LogError("请绑定齿轮设置按钮！");
        }
    }

    /// <summary>
    /// 切换设置面板的显示/隐藏
    /// </summary>
    public void ToggleSettingPanel()
    {
        if (settingPanel != null)
        {
            // 显示/隐藏面板（取反当前状态）
            settingPanel.SetActive(!settingPanel.activeSelf);
            // 可选：面板显示时暂停游戏，隐藏时恢复
            Time.timeScale = settingPanel.activeSelf ? 0 : 1;
        }
    }

    /// <summary>
    /// 退出功能（给退出按钮调用）
    /// </summary>
    public void OnQuitButtonClick()
    {
        // 恢复游戏时间（避免主菜单/退出时暂停）
        Time.timeScale = 1;

        if (isQuitGame)
        {
            // 退出游戏（打包后生效，编辑器中仅打印日志）
#if UNITY_EDITOR
            Debug.Log("编辑器中模拟退出游戏！");
            UnityEditor.EditorApplication.isPlaying = false; // 编辑器中停止运行
#else
            Application.Quit(); // 打包后真正退出游戏
#endif
        }
        else
        {
            // 返回主菜单（需确保主菜单场景已加入Build Settings）
            try
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError("加载主菜单失败：" + e.Message);
            }
        }
    }
}