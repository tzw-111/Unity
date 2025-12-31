using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 血条控制器：整合血量控制+延迟Game Over+返回主菜单功能
/// 特性：平滑扣血完成后延迟弹出Game Over，支持重新开始/返回主菜单
/// </summary>
public class HealthBarController : MonoBehaviour
{
    [Header("血条核心配置")]
    [Tooltip("血条填充图片（你的Ima_Hpline）")]
    public Image healthBarImage;
    [Tooltip("最大血量（可在Inspector面板修改）")]
    public float maxHealth = 100f;
    [Tooltip("渐变增减血的速度（值越大越快，建议10-30）")]
    public float smoothSpeed = 20f;

    [Header("Game Over 配置")]
    [Tooltip("Game Over弹窗面板（拖入你的GameOverPanel）")]
    public GameObject gameOverPanel;
    [Tooltip("血量渐变扣完后，延迟多久弹出Game Over（秒），默认0.5秒")]
    public float gameOverDelay = 0.5f;
    [Tooltip("主菜单场景名称（必须和Build Settings里的场景名一致）")]
    public string mainMenuSceneName = "MainMenu"; // 新增：主菜单场景名

    [Header("无需手动修改")]
    [SerializeField] private float currentHealth; // 当前血量
    private float targetHealth; // 渐变目标血量
    private float originalWidth; // 血条初始宽度
    private bool isGameOverTriggered = false; // 防重复触发标记

    /// <summary>
    /// 初始化血条和Game Over面板
    /// </summary>
    private void Awake()
    {
        // 检查血条图片绑定
        if (healthBarImage == null)
        {
            Debug.LogError("请为血条控制器绑定Ima_Hpline图片！");
            return;
        }

        // 初始化血条参数
        originalWidth = healthBarImage.rectTransform.rect.width;
        currentHealth = maxHealth;
        targetHealth = currentHealth;
        UpdateHealthBar();

        // 初始化Game Over面板（隐藏）
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        // 恢复游戏时间（避免重启后暂停）
        Time.timeScale = 1;
        // 重置Game Over触发标记
        isGameOverTriggered = false;
    }

    /// <summary>
    /// 每帧更新：处理平滑渐变血条
    /// </summary>
    private void Update()
    {
        // 平滑插值血量
        if (Mathf.Abs(targetHealth - currentHealth) > 0.1f)
        {
            currentHealth = Mathf.Lerp(currentHealth, targetHealth, smoothSpeed * Time.deltaTime);
            UpdateHealthBar();
        }
        else
        {
            // 插值完成：当前血量对齐目标血量
            currentHealth = targetHealth;
            UpdateHealthBar();

            // 只在插值完成、血量为0、且未触发过Game Over时，启动延迟显示
            if ((targetHealth <= 0 || currentHealth <= 0.1f) && !isGameOverTriggered && gameOverPanel != null)
            {
                StartCoroutine(ShowGameOverWithDelay()); // 启动协程实现延迟
                isGameOverTriggered = true; // 标记已触发，避免重复
            }
        }
    }

    /// <summary>
    /// 协程方法 - 延迟指定时间后显示Game Over
    /// </summary>
    private IEnumerator ShowGameOverWithDelay()
    {
        // 等待指定延迟时间（血量渐变完成后，再等gameOverDelay秒）
        yield return new WaitForSeconds(gameOverDelay);

        // 延迟结束后，显示Game Over并暂停游戏
        Time.timeScale = 0;
        gameOverPanel.SetActive(true);
    }

    /// <summary>
    /// 更新血条显示宽度
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBarImage == null) return;

        float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);
        healthBarImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth * healthRatio);
    }

    #region 对外公开的血量操作方法
    /// <summary>
    /// 立即扣血（无渐变）
    /// </summary>
    /// <param name="damage">扣除的血量值</param>
    public void TakeDamage(float damage)
    {
        targetHealth = Mathf.Max(currentHealth - damage, 0);
        targetHealth = 0f;
        currentHealth = targetHealth;
        UpdateHealthBar();

        // 立即扣血时，也触发延迟Game Over（如果未触发过）
        if (!isGameOverTriggered && gameOverPanel != null)
        {
            StartCoroutine(ShowGameOverWithDelay());
            isGameOverTriggered = true;
        }
    }

    /// <summary>
    /// 平滑扣血（有渐变，推荐游戏内使用）
    /// </summary>
    /// <param name="damage">扣除的血量值</param>
    public void TakeDamageSmooth(float damage)
    {
        targetHealth = Mathf.Max(currentHealth - damage, 0);
        // 强制对齐到0，避免浮点数微小误差
        if (targetHealth <= 0)
        {
            targetHealth = 0f;
        }
        // 重置触发标记（如果扣血后血量又恢复，可重新触发）
        if (targetHealth > 0)
        {
            isGameOverTriggered = false;
        }
    }

    /// <summary>
    /// 立即加血（无渐变）
    /// </summary>
    /// <param name="healAmount">增加的血量值</param>
    public void Heal(float healAmount)
    {
        targetHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        currentHealth = targetHealth;
        UpdateHealthBar();
        // 加血后重置Game Over触发标记
        isGameOverTriggered = false;
    }

    /// <summary>
    /// 平滑加血（有渐变，推荐游戏内使用）
    /// </summary>
    /// <param name="healAmount">增加的血量值</param>
    public void HealSmooth(float healAmount)
    {
        targetHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        // 加血后重置Game Over触发标记
        isGameOverTriggered = false;
    }

    /// <summary>
    /// 直接设置当前血量（无渐变）
    /// </summary>
    /// <param name="newHealth">新的血量值</param>
    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        targetHealth = currentHealth;
        UpdateHealthBar();

        // 设置血量为0时，触发延迟Game Over
        if ((currentHealth <= 0.1f) && !isGameOverTriggered && gameOverPanel != null)
        {
            StartCoroutine(ShowGameOverWithDelay());
            isGameOverTriggered = true;
        }
        else if (currentHealth > 0)
        {
            isGameOverTriggered = false;
        }
    }

    /// <summary>
    /// 重置血量为满血（无渐变）
    /// </summary>
    public void ResetToFullHealth()
    {
        SetHealth(maxHealth);
    }

    /// <summary>
    /// 平滑重置为满血（有渐变）
    /// </summary>
    public void ResetToFullHealthSmooth()
    {
        targetHealth = maxHealth;
        isGameOverTriggered = false;
    }

    /// <summary>
    /// 修改最大血量（比如升级后加血上限）
    /// </summary>
    /// <param name="newMaxHealth">新的最大血量</param>
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(newMaxHealth, 1);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        targetHealth = currentHealth;
        UpdateHealthBar();
        isGameOverTriggered = false;
    }
    #endregion

    /// <summary>
    /// 重新开始游戏（给“重新开始”按钮调用）
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1; // 恢复游戏时间
        isGameOverTriggered = false; // 重置触发标记
        // 重新加载当前游戏场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 新增：返回主菜单（给“返回主菜单”按钮调用）
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1; // 必须恢复游戏时间，否则主菜单会暂停
        isGameOverTriggered = false; // 重置触发标记
        // 加载主菜单场景（场景名必须和Build Settings里一致）
        try
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("加载主菜单失败！请检查场景名是否正确，或是否加入Build Settings：" + e.Message);
        }
    }
}