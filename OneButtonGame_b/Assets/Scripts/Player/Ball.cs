using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Ball : MonoBehaviour
{
    [Header("攻撃力")]
    public int attackPower = 100;

    [Header("打撃設定")]
    public float hitPower = 50f;
    public float upwardModifier = 0.5f;
    public float horizontalControl = 2.0f;
    public float pushForce = 10f;

    [Header("カメラリセット")]
    // 地面に触れてからカメラが戻るまでの時間
    public float resetDelay = 1.0f;

    [Header("スケール調整")]
    public float distanceScalingFactor = 0.05f;

    [Header("予測マーカー")]
    public GameObject targetMarkerPrefab;
    public GameObject timingMarkerPrefab;

    [Header("予測マーカーの初期スケール")]
    public Vector3 initialMarkerScale = new Vector3(5f, 5f, 5f);

    [Header("予測マーカーの最終スケール")]
    public Vector3 finalMarkerScale = Vector3.one;

    [Header("難易度")]
    [SerializeField] private bool hard = false;

    private float touchGround = 0;

    public bool isGraunded = false;
    private bool isFaul = false;
    private bool isRecordingTrajectory = false;
    private bool predictionDone = false;

    private List<Vector3> trajectoryPoints = new List<Vector3>();
    
    private Vector3 lastVelocity;
    private Vector3 initialScale;

    private Rigidbody rb;
    private PlayerController playerController;
    private CameraController cameraController;
    private Camera mainCamera;
    private CanonController canonController;
    private GameObject targetMarkerInstance;
    private GameObject timingMarkerInstance;
    private Coroutine markerAnimationCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cameraController = FindObjectOfType<CameraController>();
        canonController = FindObjectOfType<CanonController>();
        playerController = FindObjectOfType<PlayerController>();

        initialScale = transform.localScale;
        mainCamera = Camera.main;

        if (targetMarkerPrefab != null)
        {
            targetMarkerInstance = Instantiate(targetMarkerPrefab);
            targetMarkerInstance.SetActive(false);
        }

        if (timingMarkerPrefab != null)
        {
            timingMarkerInstance = Instantiate(timingMarkerPrefab);
            timingMarkerInstance.SetActive(false);
        }
    }

    private void Update()
    {
        if (isRecordingTrajectory && mainCamera != null)
        {
            float distance = Vector3.Distance(transform.position, mainCamera.transform.position);

            float scaleFactor = 1.0f + distance * distanceScalingFactor;

            transform.localScale = initialScale * scaleFactor;
        }
    }

    private void FixedUpdate()
    {
        // 衝突前の速度を保持
        lastVelocity = rb.velocity;

        if (!predictionDone && rb.velocity.magnitude > 0.1f)
        {
            PredictAndPlaceMarker();
            predictionDone = true;
        }

        if (isRecordingTrajectory)
        {
            trajectoryPoints.Add(transform.position);
        }
    }

    // 他のオブジェクトと衝突した時に呼び出される
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bat"))
        {
            // ヒットしたらマーカーを消す
            HideMarker();

            if (playerController != null)
            {
                playerController.NotifyHit();
            }

            isGraunded = false;
            isRecordingTrajectory = true;
            trajectoryPoints.Clear();

            Vector3 impactPoint = collision.contacts[0].point;

            if (hard)
            {
                ContactPoint contact = collision.contacts[0];
                Vector3 contactNormal = contact.normal;

                Vector3 reflectedDirection = Vector3.Reflect(lastVelocity.normalized, contactNormal);

                reflectedDirection.y = Mathf.Abs(reflectedDirection.y) * upwardModifier;

                if(reflectedDirection.z > 0)
                {
                    reflectedDirection.z *= -1;
                }

                Vector3 newVelocity = reflectedDirection.normalized * hitPower;

                if (rb != null)
                {
                    rb.velocity = newVelocity;
                    rb.AddForce(rb.velocity.normalized * pushForce);

                    // ログを出力
                    LogHitData("ハード", newVelocity, impactPoint);

                    if (cameraController != null)
                    {
                        cameraController.StartTracking(transform);
                    }
                }
            }
            else
            {
                float horizontalDifference = transform.position.x - collision.transform.position.x;

                float horizontalForce = horizontalDifference * horizontalControl;

                Vector3 hitDirection = new Vector3(horizontalForce, upwardModifier, 1).normalized;

                Vector3 newVelocity = hitDirection * hitPower;

                if (rb != null)
                {
                    /*
                    // 現在の速度を一度リセット
                    rb.velocity = Vector3.zero;
                    // 新しい方向に力を加える
                    rb.AddForce(hitDirection * hitPower, ForceMode.Impulse);
                    */
                    rb.velocity = newVelocity;

                    // ログを出力
                    LogHitData("ノーマル", newVelocity, impactPoint);

                    if (cameraController != null)
                    {
                        cameraController.StartTracking(transform);
                    }
                }
            }

        }
        else if (collision.gameObject.CompareTag("Ground") && !isGraunded)
        {
            touchGround++;
            if (cameraController != null && touchGround >= 3)
            {
                isGraunded = true;
                StopAndSaveTrajectory();
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            if (cameraController != null)
            {
                isGraunded = true;
                StopAndSaveTrajectory();
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
        else if (collision.gameObject.CompareTag("Boss"))
        {
            BossController boss = collision.gameObject.GetComponent<BossController>();

            if(boss != null)
            {
                boss.TakeDamage(attackPower);
            }

            if (cameraController != null)
            {
                isGraunded = true;
                StopAndSaveTrajectory();
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Strike"))
        {
            isFaul = true;
            Debug.Log("ストライク");
            if (cameraController != null && !isGraunded)
            {
                isGraunded = true;
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
        else if (other.gameObject.CompareTag("Foul"))
        {
            Debug.Log("ファール");
            if (cameraController != null && !isGraunded)
            {
                isGraunded = true;
                StopAndSaveTrajectory();
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
    }

    private void PredictAndPlaceMarker()
    {
        if (targetMarkerInstance == null || timingMarkerInstance == null) return;

        Vector3 initialVelocity = rb.velocity;
        Vector3 initialPosition = transform.position;

        float timeToImpact = -initialPosition.z / initialVelocity.z;

        if (timeToImpact < 0 || Mathf.Abs(initialVelocity.z) < 0.1f) return;

        float impactX = initialVelocity.x * timeToImpact + initialPosition.x;
        float impactY = (0.5f * Physics.gravity.y * timeToImpact * timeToImpact) + (initialVelocity.y * timeToImpact) + initialPosition.y;

        Vector3 targetMarkerPosition = new Vector3(impactX, impactY, 0);
        Vector3 timingMarkerPosition = new Vector3(impactX, impactY, 0.01f);

        targetMarkerInstance.transform.position = targetMarkerPosition;
        targetMarkerInstance.SetActive(true);

        timingMarkerInstance.transform.position = timingMarkerPosition;
        timingMarkerInstance.SetActive(true);

        markerAnimationCoroutine = StartCoroutine(AnimateMarkerScale(timeToImpact));
    } 

    public void HideMarker()
    {
        if (targetMarkerInstance != null)
        {
            targetMarkerInstance.SetActive(false);
        }

        if (timingMarkerInstance != null)
        {
            if (markerAnimationCoroutine != null)
            {
                StopCoroutine(markerAnimationCoroutine);
                markerAnimationCoroutine = null;
            }
            timingMarkerInstance.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (targetMarkerInstance != null)
        {
            Destroy(targetMarkerInstance);
        }
        if (timingMarkerInstance != null)
        {
            Destroy(timingMarkerInstance);
        }
    }

    // 指定した時間だけ待ってから処理を再開するコルーチン
    IEnumerator ResetCameraAfterDelay()
    {
        if (!isFaul)
        {
            yield return new WaitForSeconds(resetDelay);
        }
        isFaul = false;

        if(cameraController != null)
        {
            cameraController.ResetCamera();
            canonController.FireCanon();
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 打球のデータを整形して出力する
    /// </summary>
    /// <param name="difficulty">難易度</param>
    /// <param name="ballRb">ボールのRigitdbody</param>
    /// <param name="impactPoint">衝突した座標</param>
    void LogHitData(string difficulty, Vector3 velocity, Vector3 impactPoint)
    {
        float horizontalMagnitude = new Vector2(velocity.x, velocity.z).magnitude;
        float verticalMagnitude = velocity.y;
        float launchAngleRed = Mathf.Atan2(verticalMagnitude, horizontalMagnitude);
        float launchAngle = launchAngleRed * Mathf.Rad2Deg;
        /*
        Vector3 flatVelocity = new Vector3(velocity.x, 0, velocity.z);
        float launchAngle = Vector3.Angle(velocity, flatVelocity);
        */

        if (DataLogger.Instance != null)
        {
            DataLogger.Instance.LogHit(
                difficulty,
                velocity.magnitude,
                launchAngle,
                impactPoint
            );
        }
    }

    void StopAndSaveTrajectory()
    {
        if (isRecordingTrajectory)
        {
            isRecordingTrajectory = false;

            if (DataLogger.Instance != null)
            {
                string difficulty = hard ? "ハード" : "ノーマル";
                /*
                Vector3 initialVelocity = rb.velocity;
                Vector3 flatVelocity = new Vector3(initialVelocity.x, 0, initialVelocity.z);
                float launchAngle = Vector3.Angle(initialVelocity, flatVelocity);

                DataLogger.Instance.SaveTrajectory(difficulty, initialVelocity.magnitude, launchAngle, trajectoryPoints);
                */

                Vector3 finalVelocity = rb.velocity;
                float horizontalMagnitude = new Vector2(finalVelocity.x, finalVelocity.z).magnitude;
                float verticalMagnitude = finalVelocity.y;
                float launchAngleRed = Mathf.Atan2(verticalMagnitude, horizontalMagnitude);
                float launchAngle = launchAngleRed * Mathf.Rad2Deg;

                DataLogger.Instance.SaveTrajectory(difficulty, finalVelocity.magnitude, launchAngle, trajectoryPoints);
            }
        }
    }

    private IEnumerator AnimateMarkerScale(float duration)
    {
        float elapsedTime = 0f;
        if (duration <= 0)
        {
            timingMarkerInstance.transform.localScale = finalMarkerScale;
            yield break;
        }

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            timingMarkerInstance.transform.localScale = Vector3.Lerp(initialScale, finalMarkerScale, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        timingMarkerInstance.transform.localScale = finalMarkerScale;
        markerAnimationCoroutine = null;
    }
}