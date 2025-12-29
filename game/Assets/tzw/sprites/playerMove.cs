using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public class playerMove : MonoBehaviour
{
    float horizontal = 0;//Ë®Æ½
    Vector2 position ;
    float vertical;//´¹Ö±

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        position = transform.position;
        position.x = position.x + horizontal * 0.1f;
        transform.position = position;

        vertical = Input.GetAxis("Vertical");
        position = transform.position;
        position.y = position.y + horizontal * 0.1f;
        transform.position = position;
    }
}
