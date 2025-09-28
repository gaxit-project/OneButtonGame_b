using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("ˆÚ“®‘¬“x")]
    public float moveSpeed = 5.0f;
    void Start()
    {
        
    }

    void Update()
    {
        // …•½•ûŒü‚ÌˆÚ“®ˆ—
        float horizontalInput = Input.GetAxis("Horizontal");
        Vector3 movement = Vector3.right * horizontalInput * moveSpeed * Time.deltaTime;
        transform.Translate(movement);
    }
}
