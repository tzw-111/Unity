using System.Diagnostics;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    // 受击扣血方法
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        //Debug.Log(gameObject.name + " 受到 " + damage + " 点伤害，剩余生命值：" + currentHealth);

        // 可以在这里添加受击特效、音效等
        if (currentHealth <= 0)
        {
            // 调用死亡逻辑（如果是玩家可以在这里处理游戏结束）
            OnDeath();
        }
    }

    // 死亡逻辑
    protected virtual void OnDeath()
    {
        //Debug.Log(gameObject.name + " 死亡");
        // 玩家死亡可以在这里加载游戏失败场景
        // 敌人死亡逻辑已经在Enemy2DController中实现
    }

    // 给玩家调用的攻击方法（示例）
    public void AttackEnemy(Enemy2DController enemy, Vector2 attackDirection)
    {
        enemy.TakeDamage(20f, attackDirection); // 攻击伤害20，击退方向为玩家朝向
    }
}