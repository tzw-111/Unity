using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f; // 移动速度

    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        // 获取Rigidbody2D组件
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 在Update中检测输入
        movement.x = Input.GetAxisRaw("Horizontal"); // A/D 或 左右箭头
        movement.y = Input.GetAxisRaw("Vertical");   // W/S 或 上下箭头

        // 可选：让角色朝向移动方向
        if (movement.x != 0)
        {
            FlipSprite(movement.x);
        }
    }

    void FixedUpdate()
    {
        // 在FixedUpdate中处理物理移动
        rb.velocity = movement.normalized * moveSpeed;
    }

    // 翻转精灵朝向
    private void FlipSprite(float horizontalInput)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (horizontalInput > 0 ? 1 : -1);
        transform.localScale = scale;
    }
}
