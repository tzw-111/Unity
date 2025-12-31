using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 血条控制器：通过控制指定Image的宽度实现血量增减
/// 包含立即增减、平滑渐变增减、重置满血等所有常用方法
/// </summary>
public class HealthBarController : MonoBehaviour
{
    [Header("血条核心配置")]
    [Tooltip("血条填充图片（你的Ima_Hpline）")]
    public Image healthBarImage; // 绑定你的Ima_Hpline图片
    [Tooltip("最大血量（可在Inspector面板修改）")]
    public float maxHealth = 100f;
    [Tooltip("渐变增减血的速度（值越大越快，建议10-30）")]
    public float smoothSpeed = 20f;

    [Header("无需手动修改")]
    [SerializeField] private float currentHealth; // 当前血量
    private float targetHealth; // 渐变目标血量（用于平滑过渡）
    private float originalWidth; // 血条图片的初始宽度（自动记录）

    /// <summary>
    /// 初始化血条（游戏开始时调用）
    /// </summary>
    private void Awake()
    {
        // 检查是否绑定了血条图片
        if (healthBarImage == null)
        {
            Debug.LogError("请为血条控制器绑定Ima_Hpline图片！");
            return;
        }

        // 记录血条图片的初始宽度（避免缩放/锚点影响）
        originalWidth = healthBarImage.rectTransform.rect.width;
        // 初始化血量为满血
        currentHealth = maxHealth;
        targetHealth = currentHealth;
        // 更新血条显示
        UpdateHealthBar();
    }

    /// <summary>
    /// 每帧更新：处理平滑渐变的血条
    /// </summary>
    private void Update()
    {
        // 如果目标血量和当前血量不一致，就平滑插值
        if (Mathf.Abs(targetHealth - currentHealth) > 0.1f)
        {
            currentHealth = Mathf.Lerp(currentHealth, targetHealth, smoothSpeed * Time.deltaTime);
            UpdateHealthBar();
        }
        else
        {
            // 差值过小时直接对齐，避免无限插值
            currentHealth = targetHealth;
            UpdateHealthBar();
        }
    }

    /// <summary>
    /// 核心方法：更新血条图片的宽度
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBarImage == null) return;

        // 计算血量比例（0-1）
        float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);
        // 根据比例设置血条宽度
        healthBarImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalWidth * healthRatio);
    }

    #region 对外公开的血量操作方法（直接调用即可）
    /// <summary>
    /// 立即扣血（无渐变）
    /// </summary>
    /// <param name="damage">扣除的血量值</param>
    public void TakeDamage(float damage)
    {
        targetHealth = Mathf.Max(currentHealth - damage, 0); // 血量不低于0
        currentHealth = targetHealth; // 立即同步，无渐变
        UpdateHealthBar();
    }

    /// <summary>
    /// 平滑扣血（有渐变，推荐游戏内使用）
    /// </summary>
    /// <param name="damage">扣除的血量值</param>
    public void TakeDamageSmooth(float damage)
    {
        targetHealth = Mathf.Max(currentHealth - damage, 0); // 血量不低于0
    }

    /// <summary>
    /// 立即加血（无渐变）
    /// </summary>
    /// <param name="healAmount">增加的血量值</param>
    public void Heal(float healAmount)
    {
        targetHealth = Mathf.Min(currentHealth + healAmount, maxHealth); // 血量不超过最大值
        currentHealth = targetHealth; // 立即同步，无渐变
        UpdateHealthBar();
    }

    /// <summary>
    /// 平滑加血（有渐变，推荐游戏内使用）
    /// </summary>
    /// <param name="healAmount">增加的血量值</param>
    public void HealSmooth(float healAmount)
    {
        targetHealth = Mathf.Min(currentHealth + healAmount, maxHealth); // 血量不超过最大值
    }

    /// <summary>
    /// 直接设置当前血量（无渐变）
    /// </summary>
    /// <param name="newHealth">新的血量值</param>
    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth); // 限制在0-最大血量之间
        targetHealth = currentHealth;
        UpdateHealthBar();
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
    }

    /// <summary>
    /// 修改最大血量（比如升级后加血上限）
    /// </summary>
    /// <param name="newMaxHealth">新的最大血量</param>
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(newMaxHealth, 1); // 最大血量至少为1
        // 同步当前血量（如果当前血量超过新上限，自动降到新上限）
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        targetHealth = currentHealth;
        UpdateHealthBar();
    }
    #endregion
}