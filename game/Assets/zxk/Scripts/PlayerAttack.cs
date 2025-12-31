using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float attackDamage = 20f;
    public float attackRange = 2f;
    public LayerMask enemyLayer;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Attack();
        }
    }

    void Attack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayer);
        foreach (var enemyCollider in hitEnemies)
        {
            FlyingEyeController enemy = enemyCollider.GetComponent<FlyingEyeController>();
            if (enemy != null)
            {
                Vector2 hitDirection = (enemyCollider.transform.position - transform.position).normalized;
                enemy.TakeDamage(attackDamage, hitDirection);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}