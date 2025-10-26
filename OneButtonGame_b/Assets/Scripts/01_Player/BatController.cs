using System;
using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime;

public class BatController : MonoBehaviour
{
    [Header("打席の構え設定")]
    [Tooltip("右打ちのバットの位置と角度")]
    public Transform rightHandedStance;
    [Tooltip("左打ちのバットの位置と角度")]
    public Transform leftHandedStance;

    [Header("スイートスポット設定")]
    public Transform sweetSpot;

    [Header("コンポーネント")]
    public PlayerController playerController;

    [Header("プレイヤースイング設定")]
    public float playerSwingRotationAngle = 180f;

    [Header("右打席スイングのキーポイント")]
    [Tooltip("通常の構え")]
    public Transform rightIdleStance;
    [Tooltip("テイクバックした状態のバットの位置・回転")]
    public Transform rightTakeBackStance;
    [Tooltip("インパクト直前の状態のバットの位置・回転")]
    public Transform rightImpactStance;
    [Tooltip("フォロースルーの状態のバットの位置・回転")]
    public Transform rightFollowThroughStance;

    [Header("左打席スイングのキーポイント")]
    [Tooltip("通常の構え")]
    public Transform leftIdleStance;
    [Tooltip("テイクバックした状態のバットの位置・回転")]
    public Transform leftTakeBackStance;
    [Tooltip("インパクト直前の状態のバットの位置・回転")]
    public Transform leftImpactStance;
    [Tooltip("フォロースルーの状態のバットの位置・回転")]
    public Transform leftFollowThroughStance;

    [Header("スイング速度設定")]
    [Tooltip("構え→テイクバックにかかる時間")]
    public float timeToTakeBack = 0.15f;
    [Tooltip("テイクバック→インパクトにかかる時間")]
    public float timeToImpact = 0.08f;
    [Tooltip("インパクト→フォロースルーにかかる時間")]
    public float timeToFollowThrough = 0.12f;
    [Tooltip("フォロースルー→構えにかかる時間")]
    public float timeToReturnIdle = 0.2f;

    private Quaternion playerInitialRotation;

    // 打席が変更されたことを通知するイベント
    public event Action<bool> OnStanceChanged;
    public bool isRightHanded { get; private set; } = true; // true：右打ち false：左打ち
    private bool isSwinging = false;
    private bool canInput = true;
    private CancellationTokenSource swingCts;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();

        OnStanceChanged += HandleStanceChange;
        if(playerController != null) playerController.SetAnimationStance(isRightHanded);
    }

    void Start()
    {
        if(rightHandedStance != null && leftHandedStance != null)
        {
            SetStance(isRightHanded);
        }
        else
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!canInput || isSwinging) return;

        // 打席の切り替え
        float horizontalInput = Input.GetAxis("Horizontal");

        if(horizontalInput > 0 && !isRightHanded)
        {
            SetStance(true);
        }
        else if(horizontalInput < 0 && isRightHanded)
        {
            SetStance(false);
        }

        if (Input.GetButtonDown("Fire1")) StartSwingSequence().Forget();
    }

    public void SetInputEnabled(bool enabled)
    {
        canInput = enabled;

        if(!enabled && isSwinging) swingCts?.Cancel();
    }

    /// <summary>
    /// 打席の構えを設定するメソッド
    /// </summary>
    void SetStance(bool isRight)
    {
        if (isSwinging) return;

        isRightHanded = isRight;

        Transform targetStance = isRight ? rightHandedStance : leftHandedStance;

        transform.localPosition = targetStance.localPosition;
        transform.localRotation = targetStance.localRotation;

        // 打席が変更されたことを通知
        OnStanceChanged?.Invoke(isRightHanded);
    }

    private void HandleStanceChange(bool isRight)
    {
        if (playerController != null) playerController.SetAnimationStance(isRight);
    }

    public async UniTaskVoid StartSwingSequence()
    {
        if (isSwinging || !canInput) return;
        if (rightIdleStance == null || rightTakeBackStance == null || rightImpactStance == null || rightFollowThroughStance == null) return;

        isSwinging = true;

        playerController?.SetInputEnabled(false);

        playerInitialRotation = playerController.transform.rotation;

        swingCts?.Cancel();
        swingCts = new CancellationTokenSource();
        CancellationToken token = swingCts.Token;

        try
        {
            Transform idle = isRightHanded ? rightIdleStance : leftIdleStance;
            Transform takeBack = isRightHanded ? rightTakeBackStance : leftHandedStance;
            Transform impact = isRightHanded ? rightImpactStance : leftImpactStance;
            Transform follow = isRightHanded ? rightFollowThroughStance : leftFollowThroughStance;

            Vector3 targetIdlePos = idle.localPosition;         Quaternion targetIdleRot = idle.localRotation;
            Vector3 targetTakeBackPos = takeBack.localPosition; Quaternion targetTakeBackRot = takeBack.localRotation;
            Vector3 targetImpactPos = impact.localPosition;     Quaternion targetImpactRot = impact.localRotation;
            Vector3 targetFollowPos = follow.localPosition;     Quaternion targetFollowRot = follow.localRotation;

            float currentSwingRotationAngle = isRightHanded ? playerSwingRotationAngle : -playerSwingRotationAngle;
            float rotationToImpact = currentSwingRotationAngle * 0.6f;
            float rotationToFollow = currentSwingRotationAngle * 0.4f;

            // 1.構え → テイクバック
            await MoveAndRotateBatAsync(targetImpactPos, targetImpactRot, timeToTakeBack, token, rotationToImpact);

            // 2.テイクバック → インパクト
            //await MoveAndRotateBatAsync(targetImpactPos, targetImpactRot, timeToImpact, token, rotationToImpact);

            // 3.インパクト → フォロースルー
            await MoveAndRotateBatAsync(targetFollowPos, targetFollowRot, timeToFollowThrough, token, rotationToFollow);

            Quaternion currentRotation = playerController.transform.rotation;
            Quaternion rotaionDifference = Quaternion.Inverse(playerController.transform.rotation) * playerInitialRotation;
            float rotaionToReturnTotal = rotaionDifference.eulerAngles.y;

            if (rotaionToReturnTotal > 180) rotaionToReturnTotal -= 360f;
            else if (rotaionToReturnTotal < -180) rotaionToReturnTotal += 360f;

            float totalReturnDuration = timeToTakeBack + timeToImpact + timeToFollowThrough;
            float returnRotationInpact = rotaionToReturnTotal * (timeToFollowThrough / totalReturnDuration);
            float returnRotationTakeBack = rotaionToReturnTotal * (timeToImpact / totalReturnDuration);
            float returnRotationIdle = rotaionToReturnTotal * (timeToTakeBack / totalReturnDuration);

            // 4.フォロースルー → 構え
            await MoveAndRotateBatAsync(targetImpactPos, targetImpactRot, timeToFollowThrough, token, returnRotationInpact);
            await MoveAndRotateBatAsync(targetIdlePos, targetIdleRot, timeToTakeBack, token, returnRotationIdle + returnRotationTakeBack);

            SetStance(isRightHanded);
            if (playerController != null) playerController.transform.rotation = playerInitialRotation;
        }
        catch (OperationCanceledException)
        {
            SetStance(isRightHanded);
        }
        finally
        {
            isSwinging = false;

            playerController?.SetInputEnabled(true);

            swingCts?.Dispose();
            swingCts = null;
        }
    }

    private async UniTask MoveAndRotateBatAsync(Vector3 targetLocalPosition, Quaternion targetLocalRotation, float duration, CancellationToken token, float playerYRotationAmount)
    {
        Vector3 startLocalPosition = transform.localPosition;
        Quaternion startLocalRotation = transform.localRotation;
        float elapsedTime = 0f;

        float playerRotationSpeed = 0f;

        if(duration > 0)
        {
            playerRotationSpeed = playerYRotationAmount / duration;
        }

        if(duration <= 0)
        {
            transform.localPosition = targetLocalPosition;
            transform.localRotation = targetLocalRotation;
            if(playerController != null && playerYRotationAmount != 0)
            {
                playerController.transform.Rotate(0, playerYRotationAmount, 0, Space.World);
            }

            return;
            
        }

        while(elapsedTime < duration)
        {
            token.ThrowIfCancellationRequested();

            if(playerController != null && playerYRotationAmount != 0)
            {
                float incrementalAngle = playerRotationSpeed * Time.deltaTime;

                playerController.transform.Rotate(0, incrementalAngle, 0, Space.World);
            }

            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);
            transform.localRotation = Quaternion.Slerp(startLocalRotation, targetLocalRotation, t);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // 最終的な位置・回転を設定
        transform.localPosition = targetLocalPosition;
        transform.localRotation = targetLocalRotation;
    }

    private void OnDestroy()
    {
        OnStanceChanged -= HandleStanceChange;
        swingCts?.Cancel();
        swingCts?.Dispose();
    }
}
