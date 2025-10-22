using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayerX : MonoBehaviour
{
    public Transform playerTarget;
    public float fixedY = 5f;
    public float fixedZ = -10f;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = new Vector3(playerTarget.position.x, fixedY, fixedZ);
    }

    // Update is called once per frame
    void Update()
    {
        if (playerTarget != null)
        {
            transform.position = new Vector3(playerTarget.position.x, fixedY, fixedZ);
        }
    }
}
