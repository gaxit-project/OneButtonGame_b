using System.Collections.Generic;
using UnityEngine;

public class CanonController : MonoBehaviour
{
    [Header("生成するボールのプレハブ")]
    public GameObject ballPrefab;

    [Header("ボールを生成する場所")]
    public List<Transform> spawnPoints;

    [Header("ボールの発射パワー")]
    public float launchPower = 50f;

    // 生成したボールの情報を保存しておくためのリスト
    private List<GameObject> spawnedBalls = new List<GameObject>();

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            FireCanon();
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            CleanerAllBalls();
        }
    }

    void FireCanon()
    {
        if(ballPrefab == null || spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.Log("Ball PrefabまたはSpawn Pointsが設定されてません");
            return;
        }

        // spawnPointでボールを生成
        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedSpawnPoint = spawnPoints[randomIndex];
        GameObject newBall = Instantiate(ballPrefab, selectedSpawnPoint.position, selectedSpawnPoint.rotation);

        Rigidbody ballRigidbody = newBall.GetComponent<Rigidbody>();

        if(ballRigidbody != null )
        {
            // ボールに力を加える
            Vector3 launchDirection = -selectedSpawnPoint.forward;
            ballRigidbody.AddForce(launchDirection * launchPower, ForceMode.Impulse);
        }

        spawnedBalls.Add(newBall);
        Debug.Log(selectedSpawnPoint.name + " から大砲を発射しました");
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
        spawnedBalls.Clear();
    }
}
