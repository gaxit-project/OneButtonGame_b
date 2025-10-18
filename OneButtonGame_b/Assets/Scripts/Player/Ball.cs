using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class Ball : MonoBehaviour
{
    [Header("攻撃力")]
    public int attackPower = 100; // ボスへのダメージ
    public int playerAttackPower = 1; // プレイヤーへのダメージ

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
    public GameObject targetMarkerPrefab; //着弾点マーカー
    public GameObject timingMarkerPrefab; // タイミングマーカー

    [Header("予測マーカーの初期スケール")]
    public Vector3 initialMarkerScale = new Vector3(5f, 5f, 5f);

    [Header("予測マーカーの最終スケール")]
    public Vector3 finalMarkerScale = Vector3.one;

    [Header("画面外マーカーUI")]
    public Image arrowLeftUI;
    public Image arrowRightUI;

    [Header("当たり判定の幅")]
    public float justHitRadius = 1.0f;
    public float goodHitRadius = 2.0f;
    public float badHitRadius = 3.0f;

    [Header("ヒットパワー倍率")]
    public float justHitPowerMultiplier = 1.0f;
    public float goodHitPowerMultiplier = 0.7f;
    public float badHitPowerMultiplier = 0.5f;

    [Header("難易度")]
    [SerializeField] private bool hard = false;

    private float touchGround = 0;

    private bool hasBeenHit = false;
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

    public static event Action OnBallDestroyed;

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

        if (arrowLeftUI != null)
        {
            arrowLeftUI.gameObject.SetActive(false);
        }
        if (arrowRightUI != null)
        {
            arrowRightUI.gameObject.SetActive(false);
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

        UpdateOffScreenArrows();

        if (!hasBeenHit && playerController != null && playerController.IsPlayerSwinging && rb.velocity.z < 0)
        {
            if (playerController.batController != null && playerController.batController.sweetSpot != null)
            {
                Vector3 sweetSpotPos = playerController.batController.sweetSpot.position;
                float distance = Vector3.Distance(transform.position, sweetSpotPos);

                if (distance > justHitRadius && distance <= goodHitRadius)
                {
                    Debug.Log("Good");
                    PerformTimingHit(goodHitPowerMultiplier);
                }
                else if (distance > goodHitRadius && distance <= badHitRadius)
                {
                    Debug.Log("Bad");
                    PerformTimingHit(badHitPowerMultiplier);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // 衝突前の速度を保持
        if (!hasBeenHit)
        {
            lastVelocity = rb.velocity;
        }

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
            if (hasBeenHit) return;

            hasBeenHit = true;
            // 打撃音を再生
            SoundManager.instance.PlaySE(1);

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
                BatController batController = collision.gameObject.GetComponent<BatController>();

                if (batController != null && batController.sweetSpot != null)
                {

                    // 芯からどれだけ離れているか計算
                    Vector3 sweetSpotPosition = batController.sweetSpot.position;

                    //ボールがバットに当たった座標からバットの中心までの距離を計算
                    float hitDistance = Vector3.Distance(impactPoint, sweetSpotPosition);
                    float spatialDistance = Vector3.Distance(impactPoint, sweetSpotPosition);

                    // 判定のしきい値
                    float justHitThreshold = 2.0f; // この距離以下ならジャスト
                    float goodHitThreshold = 4.0f; // この距離以下ならグッド

                    float spatialPowerMultiplier; // 今回のヒットで適応されるパワー

                    // 距離に応じてヒットの質を判定
                    if (hitDistance <= justHitThreshold)
                    {
                        Debug.Log("ジャストヒット！");
                        spatialPowerMultiplier = 1.0f;
                    }
                    else if (hitDistance <= goodHitThreshold)
                    {
                        Debug.Log("グッドヒット");
                        spatialPowerMultiplier = 0.8f;
                    }
                    else
                    {
                        Debug.Log("バッドヒット");
                        spatialPowerMultiplier = 0.5f;
                    }

                    // タイミングの判定
                    float timingDifference = Mathf.Abs(transform.position.z);

                    // タイミングのしきい値
                    float temporalJustThreshold = 1.0f;
                    float temporalGoodThreshold = 2.0f;

                    float temporalpowerMultiplier;

                    if(timingDifference <= temporalJustThreshold)
                    {
                        temporalpowerMultiplier = 1.0f;
                    }
                    else if(timingDifference <= temporalGoodThreshold)
                    {
                        temporalpowerMultiplier = 0.7f;
                    }
                    else
                    {
                        temporalpowerMultiplier = 0.5f;
                    }

                    Debug.Log($"当たった場所: {spatialPowerMultiplier}, タイミング: {temporalpowerMultiplier}");

                    float finalHitPower = hitPower * spatialPowerMultiplier * temporalpowerMultiplier;

                    // パワーの調整
                    float horizontalDifference = transform.position.x - collision.transform.position.x;
                    float horizontalForce = horizontalDifference * horizontalControl;
                    Vector3 hitDirection = new Vector3(horizontalForce, upwardModifier, 1).normalized;

                    // 調整されたパワーを速度に適応
                    Vector3 newVelocity = hitDirection * finalHitPower;

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
                else
                {
                    // sweetSpotが無い場合
                    Debug.LogWarning("BatControllerまたはSweetSpotが設定されてません");
                    float horizontalDifference = transform.position.x - collision.transform.position.x;
                    float horizontalForce = horizontalDifference * horizontalControl;
                    Vector3 hitDirection = new Vector3(horizontalForce, upwardModifier, 1);
                    Vector3 newVelocity = hitDirection * hitPower;
                    if(rb != null)
                    {
                        rb.velocity = newVelocity;
                        LogHitData("ノーマル", newVelocity, impactPoint);
                        if(cameraController != null)
                        {
                            cameraController.StartTracking(transform);
                        }
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
                boss.TakeBossDamage(attackPower);
            }

            if (cameraController != null)
            {
                isGraunded = true;
                StopAndSaveTrajectory();
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();

            if (playerController != null)
            {
                playerController.TakePlayerDamage(playerAttackPower);
            }

            HideMarker();
            StartCoroutine(ResetCameraAfterDelay());
        }
    }

    /// <summary>
    /// 指定した時間だけ待ってからカメラをリセットし、ボールを破棄するコルーチン
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Strike"))
        {
            isFaul = true;
            Debug.Log("ストライク");

            HideMarker();

            if (cameraController != null && !isGraunded)
            {
                isGraunded = true;
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
        else if (other.gameObject.CompareTag("Foul"))
        {
            Debug.Log("ファール");

            HideMarker();

            if (cameraController != null && !isGraunded)
            {
                isGraunded = true;
                StopAndSaveTrajectory();
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void PerformTimingHit(float powerMultiplier)
    {
        if (hasBeenHit) return;
        hasBeenHit = true;

        SoundManager.instance.PlaySE(1);
        HideMarker();
        if (playerController != null)
        {
            playerController.NotifyHit();
        }
        isGraunded = false;
        isRecordingTrajectory = true;
        trajectoryPoints.Clear();

        BatController batController = playerController.batController;
        if (batController == null || batController.sweetSpot == null)
        {
            return;
        }

        float finalHitPower = hitPower * powerMultiplier;

        float horizontalDifference = transform.position.x - batController.transform.position.x;
        float horizontalForce = horizontalDifference * horizontalControl;
        Vector3 hitDirection = new Vector3(horizontalForce, upwardModifier, 1).normalized;

        Vector3 newVelocity = hitDirection * finalHitPower;
        
        if (rb != null)
        {
            rb.velocity = newVelocity;

            LogHitData("ノーマル", newVelocity, batController.sweetSpot.position);

            if (cameraController != null)
            {
                cameraController.StartTracking(transform);
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

        if (arrowLeftUI != null)
        {
            arrowLeftUI.gameObject.SetActive(false);
        }
        if (arrowRightUI != null)
        {
            arrowRightUI.gameObject.SetActive(false);
        }
    }

    private void UpdateOffScreenArrows()
    {
        if (targetMarkerInstance == null || !targetMarkerInstance.activeSelf || arrowLeftUI == null || arrowRightUI == null)
        {
            return;
        }

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(targetMarkerInstance.transform.position);

        bool isVisible = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < Screen.width;

        if (isVisible)
        {
            arrowLeftUI.gameObject.SetActive(false);
            arrowRightUI.gameObject.SetActive(false);
        }
        else
        {
            if (screenPoint.x < Screen.width / 2)
            {
                arrowLeftUI.gameObject.SetActive(true);
                arrowRightUI.gameObject.SetActive(false);
            }
            else
            {
                arrowLeftUI.gameObject.SetActive(false);
                arrowRightUI.gameObject.SetActive(true);
            }
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
            //canonController.FireCanon();
        }

        OnBallDestroyed?.Invoke();

        Destroy(gameObject);
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
            timingMarkerInstance.transform.localScale = Vector3.Lerp(initialMarkerScale, finalMarkerScale, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        timingMarkerInstance.transform.localScale = finalMarkerScale;
        markerAnimationCoroutine = null;
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
}