using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Header("��������{�[���̃v���n�u")]
    public GameObject ballPrefab;

    [Header("�{�[���𐶐�����ꏊ")]
    public Transform spawnPoint;

    // ���������{�[���̏���ۑ����Ă������߂̃��X�g
    private List<GameObject> spawnedBalls = new List<GameObject>();
    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            SpawnBall();
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            CleanerAllBalls();
        }
    }

    void SpawnBall()
    {
        if(ballPrefab == null || spawnPoint == null)
        {
            Debug.Log("Ball Prefab�܂���Spawn Position���ݒ肳��Ă܂���");
            return;
        }

        GameObject newBall = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedBalls.Add(newBall);
        Debug.Log("�{�[���𐶐����܂���");
    }

    void CleanerAllBalls()
    {
        foreach(GameObject ball in spawnedBalls)
        {
            if(ball != null)
            {
                Destroy(ball);
            }
        }
    }
}
