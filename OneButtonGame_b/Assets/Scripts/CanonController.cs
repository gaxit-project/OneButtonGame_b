using System.Collections.Generic;
using UnityEngine;

public class CanonController : MonoBehaviour
{
    [Header("生成するボールのプレハブ")]
    public GameObject ballPrefab;

    [Header("ボールを生成する場所")]
    public Transform spawnPoint;

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
        if(ballPrefab == null || spawnPoint == null)
        {
            Debug.Log("Ball PrefabまたはSpawn Positionが設定されてません");
            return;
        }

        // spawnPointでボールを生成
        GameObject newBall = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);

        Rigidbody ballRigidbody = newBall.GetComponent<Rigidbody>();

        if(ballRigidbody != null )
        {
            // ボールに力を加える
            Vector3 launchDirection = -spawnPoint.forward;
            ballRigidbody.AddForce(launchDirection * launchPower, ForceMode.Impulse);
        }

        spawnedBalls.Add(newBall);
        Debug.Log("大砲を発射しました");
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
