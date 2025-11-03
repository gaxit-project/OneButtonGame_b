using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

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

    [Header("チャンスボール")]
    public int chanceBallBossDamageMultiplier = 3;
    public int chanceBallCoreDamageMultiplier = 2;
    public int chanceBallPlayerDamageMultiplier = 2;
    public bool isChanceBall = false;


    [Header("難易度")]
    [SerializeField] private bool hard = false;

    private float touchGround = 0;
    private float delayTime = 0;

    private bool hasBeenHit = false;
    public bool isGraunded = false;
    private bool isFaul = false;
    private bool isWeakHit = false;
    private bool isRecordingTrajectory = false;
    private bool predictionDone = false;
    public bool justHit = false;    //以下打撃時の時間操作のため追加
    public bool lateHit = false;

    private List<Vector3> trajectoryPoints = new List<Vector3>();
    
    private Vector3 lastVelocity;
    private Vector3 initialScale;

    private Rigidbody rb;
    private PlayerController playerController;
    private BossController boss;
    private CameraController cameraController;
    private Camera mainCamera;
    private CanonController canonController;
    private GameObject targetMarkerInstance;
    private GameObject timingMarkerInstance;
    private Coroutine markerAnimationCoroutine;
    public GameObject targetObject;

    public static event Action OnBallDestroyed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boss = FindObjectOfType<BossController>();
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

        if (HitResultUI.Instance != null)
        {
            HitResultUI.Instance.HideResult();
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
                float distance = Mathf.Abs(transform.position.x - sweetSpotPos.x);

                if (distance > justHitRadius && distance <= goodHitRadius)
                {
                    PerformTimingHit(goodHitPowerMultiplier, "Good");
                }
                else if (distance > goodHitRadius && distance <= badHitRadius)
                {
                    PerformTimingHit(badHitPowerMultiplier, "Bad");
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
            justHit = true;
            if (hasBeenHit) return;

            HideMarker();
            hasBeenHit = true;
            // 打撃音を再生
            SoundManager.instance.PlaySE(1);

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
                    //LogHitData("ハード", newVelocity, impactPoint);

                    if (cameraController != null)
                    {
                        cameraController.StartTracking(transform);
                    }
                }

                hasBeenHit = true;

                // ヒットしたらマーカーを消す
                HideMarker();
            }
            else
            {
                BatController batController = collision.gameObject.GetComponent<BatController>();

                if (batController != null && batController.sweetSpot != null)
                {

                    // 芯からどれだけ離れているか計算
                    Vector3 sweetSpotPosition = batController.sweetSpot.position;

                    //ボールがバットに当たった座標からバットの中心までの距離を計算
                    float hitDistance = Mathf.Abs(impactPoint.x - sweetSpotPosition.x);
                    float spatialDistance = Vector3.Distance(impactPoint, sweetSpotPosition);

                    // 判定のしきい値
                    float justHitThreshold = 2.0f; // この距離以下ならジャスト
                    float goodHitThreshold = 3.0f; // この距離以下ならグッド

                    string spatialLabel = "";
                    float spatialPowerMultiplier; // 今回のヒットで適応されるパワー

                    // 距離に応じてヒットの質を判定
                    if (hitDistance <= justHitThreshold)
                    {
                        spatialLabel = "Just";
                        spatialPowerMultiplier = 1.0f;
                    }
                    else if (hitDistance <= goodHitThreshold)
                    {
                        spatialLabel = "Good";
                        spatialPowerMultiplier = 0.8f;
                    }
                    else
                    {
                        spatialLabel = "Bad";
                        spatialPowerMultiplier = 0.5f;
                        isWeakHit = true;
                    }

                    // タイミングの判定
                    float timingDifference = transform.position.z;

                    // タイミングのしきい値
                    float temporalJustThreshold = 0.5f;

                    string timingLabel = "";
                    float temporalpowerMultiplier;

                    if(timingDifference > temporalJustThreshold)
                    {
                        timingLabel = "Fast";
                        temporalpowerMultiplier = 0.8f;
                    }
                    else if(timingDifference < -temporalJustThreshold)
                    {
                        timingLabel = "Late";
                        temporalpowerMultiplier = 0.8f;
                    }
                    else
                    {
                        timingLabel = "Just";
                        temporalpowerMultiplier = 1.0f;
                    }

                    Debug.Log($"芯: {spatialLabel}, タイミング: {timingLabel}");

                    float finalHitPower = hitPower * spatialPowerMultiplier * temporalpowerMultiplier;

                    // パワーの調整
                    float horizontalDifference = transform.position.x - collision.transform.position.x;
                    float horizontalForce = horizontalDifference * horizontalControl;

                    float dynamicUpwardModifier = upwardModifier;

                    if (spatialLabel == "Bad")
                    {
                        dynamicUpwardModifier = 0f;
                    }

                    Vector3 hitDirection = new Vector3(horizontalForce, dynamicUpwardModifier, 1).normalized;

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

                        bool isFinishingBlow = CheckForFinishingBlow(newVelocity);

                        if (isFinishingBlow)
                        {
                            if (cameraController != null)
                            {
                                cameraController.PlayFinishingBlowEffect(transform);
                            }
                        }
                        else
                        {
                            if (cameraController != null)
                            {
                                cameraController.StartTracking(transform);
                            }
                        }

                        // ログを出力
                        LogHitData("ノーマル", newVelocity, impactPoint, spatialLabel, timingLabel);
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
                        LogHitData("ノーマル", newVelocity, impactPoint, "不明", "不明");
                        if(cameraController != null)
                        {
                            cameraController.StartTracking(transform);
                        }
                    }
                }
            }

            hasBeenHit = true;

        }
        else if (collision.gameObject.CompareTag("Ground") && !isGraunded)
        {
            touchGround++;
            if (cameraController != null && touchGround >= 3)
            {
                isGraunded = true;
                StopAndSaveTrajectory();
                ResetCameraAfterDelayAcync().Forget();
            }
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            if (cameraController != null)
            {
                isGraunded = true;
                StopAndSaveTrajectory();
                ResetCameraAfterDelayAcync().Forget();
            }
        }
        else if (collision.gameObject.CompareTag("Boss"))
        {
            BossController boss = collision.gameObject.GetComponent<BossController>();

            targetObject.SetActive(true);

            delayTime = 0.3f;

            int damageToBoss = isChanceBall ? (int)(attackPower * chanceBallBossDamageMultiplier) : attackPower;

            if (damageToBoss == 100) //チャンスボールの判定がわからなかったためダメージで代用
            {
                SoundManager.instance.PlaySE(7);
            }
            else
            {
                SoundManager.instance.PlaySE(8);
            }

            if (boss != null)
            {
                boss.TakeBossDamage(damageToBoss);
            }

            if (cameraController != null)
            {
                isGraunded = true;
                StopAndSaveTrajectory();
                ResetCameraAfterDelayAcync().Forget();
            }
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            if (!hasBeenHit)
            {
                PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();

                SoundManager.instance.PlaySE(5);
                if (playerController != null)
                {
                    int damageToPlayer = isChanceBall ? (int)(playerAttackPower * chanceBallPlayerDamageMultiplier) : playerAttackPower;
                    playerController.TakePlayerDamage(damageToPlayer);
                }
            }

            HideMarker();
            ResetCameraAfterDelayAcync().Forget();
        }
    }

    /// <summary>
    /// 指定した時間だけ待ってからカメラをリセットし、ボールを破棄するコルーチン
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Strike"))
        {
            Debug.Log("ストライク");

            HideMarker();

            CoreController coreController = FindObjectOfType<CoreController>();
            if (coreController != null)
            {
                int damageToCore = isChanceBall ? (int)(playerAttackPower * chanceBallCoreDamageMultiplier) : playerAttackPower;
                coreController.TakeCoreDamage(damageToCore);
            }
            
            SoundManager.instance.PlaySE(6);

            if (cameraController != null && !isGraunded)
            {
                isGraunded = true;
                ResetCameraAfterDelayAcync().Forget();
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
                ResetCameraAfterDelayAcync().Forget();
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void PerformTimingHit(float powerMultiplier, string spatialLabel)
    {
        if (hasBeenHit) return;
        hasBeenHit = true;

        string timingLabel = "";
        float timingThreshold = 0.1f;

        if (transform.position.z > timingThreshold)
        {
             timingLabel = "Fast";
        }
        else if (transform.position.z < -timingThreshold)
        {
            timingLabel = "Late";
        }
        else
        {
            timingLabel = "Just";
        }

        Debug.Log($"タイミング: {spatialLabel} {timingLabel}");

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

            LogHitData("ノーマル", newVelocity, batController.sweetSpot.position, spatialLabel, timingLabel);

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
        Vector3 timingMarkerPosition = new Vector3(impactX, impactY, 0.1f);

        targetMarkerInstance.transform.position = targetMarkerPosition;
        targetMarkerInstance.SetActive(true);

        timingMarkerInstance.transform.position = timingMarkerPosition;
        timingMarkerInstance.SetActive(true);

        //markerAnimationCoroutine = StartCoroutine(AnimateMarkerScale(timeToImpact));
        AnimateMarkerScaleAsync(timeToImpact, this.GetCancellationTokenOnDestroy()).Forget();
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

    private bool CheckForFinishingBlow(Vector3 initialVelocity)
    {
        if (boss != null && boss.CurrentHealth <= attackPower)
        {
            return false;
        }

        int bossLayerMask = LayerMask.GetMask("Boss");
        if (bossLayerMask == 0)
        {
            return false;
        }

        Vector3 predictedPos = transform.position;
        Vector3 predictedVel = initialVelocity;
        float timeStep = Time.fixedDeltaTime;
        float maxTime = 5.0f;
        float ballRadius = transform.localScale.x / 2.0f;

        for (float t = 0; t < maxTime; t += timeStep)
        {
            Vector3 nextPos = predictedPos + predictedVel * timeStep;

            predictedVel += Physics.gravity * timeStep;

            Vector3 moveDirection = nextPos - predictedPos;
            float moveDistance = moveDirection.magnitude;

            RaycastHit hit;
            if (Physics.SphereCast(predictedPos, ballRadius, moveDirection.normalized, out hit, moveDistance, bossLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject == boss.gameObject)
                {
                    return true;
                }
            }

            predictedPos = nextPos;
        }

        return false;
    }


    // 指定した時間だけ待ってから処理を再開するコルーチン
    async UniTaskVoid ResetCameraAfterDelayAcync()
    {
        var cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            if (!isFaul && !isWeakHit)
            {
                //yield return new WaitForSeconds(resetDelay);
                await UniTask.Delay(TimeSpan.FromSeconds(resetDelay), cancellationToken: cancellationToken);
            }
            isFaul = false;

            if (cameraController != null)
            {
                cameraController.ResetCamera();
                //canonController.FireCanon();
            }

            OnBallDestroyed?.Invoke();

            await UniTask.Delay(TimeSpan.FromSeconds(delayTime));

            delayTime = 0;

            Destroy(gameObject);
        }
        catch (OperationCanceledException)
        {

        }
    }

    private async UniTaskVoid AnimateMarkerScaleAsync(float duration, CancellationToken cancellationToken)
    {
        float elapsedTime = 0f;
        if (duration <= 0)
        {
            timingMarkerInstance.transform.localScale = finalMarkerScale;
            return;
        }

        try
        {
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                timingMarkerInstance.transform.localScale = Vector3.Lerp(initialMarkerScale, finalMarkerScale, t);

                elapsedTime += Time.deltaTime;
                //yield return null;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            timingMarkerInstance.transform.localScale = finalMarkerScale;
            //markerAnimationCoroutine = null;
        }
        catch (OperationCanceledException)
        {

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

    /// <summary>
    /// 打球のデータを整形して出力する
    /// </summary>
    /// <param name="difficulty">難易度</param>
    /// <param name="ballRb">ボールのRigitdbody</param>
    /// <param name="impactPoint">衝突した座標</param>
    void LogHitData(string difficulty, Vector3 velocity, Vector3 impactPoint, string spatial, string timing)
    {
        float speedKmh = velocity.magnitude * 3.6f;
        float horizontalMagnitude = new Vector2(velocity.x, velocity.z).magnitude;
        float verticalMagnitude = velocity.y;
        float launchAngleRed = Mathf.Atan2(verticalMagnitude, horizontalMagnitude);
        float launchAngle = launchAngleRed * Mathf.Rad2Deg;

        if (HitResultUI.Instance != null)
        {
            HitResultUI.Instance.ShowResult(spatial, speedKmh, launchAngle, timing);
        }

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