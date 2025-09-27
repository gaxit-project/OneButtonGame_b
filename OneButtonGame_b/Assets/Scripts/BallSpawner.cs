using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Header("生成するボールのプレハブ")]
    public GameObject ballPrefab;

    [Header("ボールを生成する場所")]
    public Transform spawnPoint;

    // 生成したボールの情報を保存しておくためのリスト
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
            Debug.Log("Ball PrefabまたはSpawn Positionが設定されてません");
            return;
        }

        GameObject newBall = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedBalls.Add(newBall);
        Debug.Log("ボールを生成しました");
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
