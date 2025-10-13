using System.Collections.Generic;
using UnityEngine;

public class CanonController : MonoBehaviour
{
    [Header("生成するボールのプレハブ")]
    public GameObject ballPrefab;

    [Header("ボールを生成する場所")]
    public List<Transform> spawnPoints;

    [Header("ターゲット設定")]
    public Transform targetPoint;
    public Vector3 targetRandomness = new Vector3(2f, 1f, 0f);
    public float timeToTarget = 2.0f;

    [Header("ボールの発射パワー")]
    public float launchPower = 50f;

    // 生成したボールの情報を保存しておくためのリスト
    private List<GameObject> spawnedBalls = new List<GameObject>();

    private bool canFire = false;

    void Start()
    {

    }

    void Update()
    {

    }

    private void OnEnable()
    {
        Ball.OnBallDestroyed += FireCanon;
    }

    private void OnDisable()
    {
        Ball.OnBallDestroyed -= FireCanon;
    }

    public void SetFiringEnabled(bool enabled)
    {
        canFire = enabled;
    }

    public void FireFirstBall()
    {
        SetFiringEnabled(true);
        FireCanon();
    }

    public void FireCanon()
    {
        if (!canFire)
        {
            return;
        }

        if (ballPrefab == null || spawnPoints == null || spawnPoints.Count == 0 || targetPoint == null)
        {
            Debug.Log("Ball PrefabまたはSpawn Pointsもしくはplayerが設定されてません");
            return;
        }

        // spawnPointでボールを生成
        int randomIndex = Random.Range(0, spawnPoints.Count);
        Transform selectedSpawnPoint = spawnPoints[randomIndex];
        GameObject newBall = Instantiate(ballPrefab, selectedSpawnPoint.position, selectedSpawnPoint.rotation);

        Rigidbody ballRigidbody = newBall.GetComponent<Rigidbody>();

        if(ballRigidbody != null )
        {
            Vector3 startPosition = selectedSpawnPoint.position;
            //Vector3 targetPosition = targetPoint.position;

            Vector3 baseTarget = targetPoint.position;

            float randomX = Random.Range(-targetRandomness.x, targetRandomness.x);
            float randomY = Random.Range(-targetRandomness.y, targetRandomness.y);
            float randomZ = Random.Range(-targetRandomness.z, targetRandomness.z);
            Vector3 randomOffset = new Vector3(randomX, randomY, randomZ);

            Vector3 finalTargetPosition = baseTarget + randomOffset;

            Vector3 initialVelocity = CalculateLaunchVelocity(startPosition, finalTargetPosition, timeToTarget);

            ballRigidbody.velocity = initialVelocity;

            /*
            // ボールに力を加える
            Vector3 launchDirection = -selectedSpawnPoint.forward;
            ballRigidbody.AddForce(launchDirection * launchPower, ForceMode.Impulse);
            */
        }

        spawnedBalls.Add(newBall);
        Debug.Log(selectedSpawnPoint.name + " から大砲を発射しました");
    }

    private Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float time)
    {
        // 目的地までの距離ベクトル
        Vector3 displacement = target - start;

        // Unityの重力ベクトル
        Vector3 gravity = Physics.gravity;

        // V = (S - 0.5 * g * t^2) / t を使って初速を計算
        Vector3 velocity = (displacement - (0.5f * gravity * (time * time))) / time;

        return velocity;
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
