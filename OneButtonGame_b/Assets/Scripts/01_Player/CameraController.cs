using UnityEngine;
using Cinemachine;
using System;
using DG.Tweening;
using System.Diagnostics.Contracts;
using Unity.PlasticSCM.Editor.WebApi;
using System.Threading;

public class CameraController : MonoBehaviour
{
    [Header("追跡ターゲット")]
    // 追いかける対象
    public Transform playerTarget;
    private Transform ballTarget;

    [Header("コンポーネント")]
    public BatController batController;

    [Header("Cinemachineカメラ")]
    public CinemachineVirtualCamera playerCamera;
    public CinemachineVirtualCamera ballCamera;
    public CinemachineVirtualCamera defeatMoveCamera;
    public CinemachineVirtualCamera allViewCamera;
    public CinemachineVirtualCamera introCamera;
    public CinemachineVirtualCamera orbitCamera;

    /*
    [Header("カメラオフセット")]
    public Vector3 rightStanceOffset; // 右打席のカメラのオフセット
    public Vector3 leftStanceOffset; // 左打席のカメラのオフセット
    public Vector3 lookUpOffset = new Vector3(0, -2f, -4f); // 見上げカメラのオフセット
    public Vector3 followOffset = new Vector3(0, 5f, 10f); // 追跡カメラのオフセット
    public Vector3 homerunOffset = new Vector3(0, -1f, -10f); // 打球のカメラのオフセット
    */

    [Header("カメラの挙動")]
    public float followSmoothness = 1f;
    public float transitionDuration = 1.5f; // 見上げから追跡へ移行する時間
    public float rotationSmoothness = 5f; // カメラがターゲットを向くときの滑らかさ

    [Header("カメラ切り替え設定")]
    public bool ballTracking = false;

    private CinemachineVirtualCamera currentActiveCamera;
    private Transform ballToTrack = null;
    private CancellationTokenSource ballTrackingCancellation;

    public event Action OnCameraReset;

    void Start()
    {
        InitializeCameras();

        if (playerCamera != null)
        {
            currentActiveCamera = allViewCamera;
            SwitchCamera(playerCamera);
        }
        else if (allViewCamera != null)
        {
            currentActiveCamera = allViewCamera;
            SwitchCamera(allViewCamera);
        }

        ballTrackingCancellation = new CancellationTokenSource();

        //  BatControllerの打席変更イベントを購読
        if (batController != null)
        {
            batController.OnStanceChanged += HandleStanceChange;

            HandleStanceChange(batController.isRightHanded);
        }
    }

    void Update()
    {
        
    }

    void InitializeCameras()
    {
        SetPriority(playerCamera, 0);
        SetPriority(ballCamera, 0);
        SetPriority(defeatMoveCamera, 0);
        SetPriority(allViewCamera, 0);
        SetPriority(introCamera, 0);
        SetPriority(orbitCamera, 0);
    }

    private void SwitchCamera(CinemachineVirtualCamera targetCamera)
    {
        if (targetCamera == null) return;

        if (currentActiveCamera != null && currentActiveCamera != targetCamera)
        {
            SetPriority(currentActiveCamera, 0);
        }

        SetPriority(targetCamera, 10);
        currentActiveCamera = targetCamera;
    }

    private void SetPriority(CinemachineVirtualCamera vcam, int priority)
    {
        if (vcam != null)
        {
            vcam.Priority = priority;
        }
    }

    public void StartAllViewMovie()
    {
        SwitchCamera(allViewCamera);
    }

    public void StartIntroMovie()
    {
        SwitchCamera(introCamera);
    }

    public void StartOrbitMovie()
    {
        SwitchCamera(orbitCamera);
    }

    /// <summary>
    /// ボールの追跡を開始
    /// </summary>
    /// <param name="ballTransform">追跡対象のボールのTransform</param>
    public void StartTracking(Transform ballTransform)
    {
        if (!ballTracking)
        {
            return;
        }

        ballTarget = ballTransform;

        if (ballCamera != null)
        {
            ballCamera.Follow = ballTarget;
            ballCamera.LookAt = ballTarget;
            ballCamera.Priority = 20;
        }

        if (playerCamera != null)
        {
            playerCamera.Priority = 0;
        }
        if (defeatMoveCamera != null)
        {
            defeatMoveCamera.Priority = 0;
        }
    }

    /// <summary>
    /// BatControllerからの通知で、打席に合わせたカメラオフセットに切り替える
    /// </summary>
    private void HandleStanceChange(bool isRightHanded)
    {
        /*
        if (isRightHanded)
        {
            currentOffset = rightStanceOffset;
        }
        else
        {
            currentOffset = leftStanceOffset;
        }
        */
    }

    /// <summary>
    /// プレイヤー追跡状態に戻す
    /// </summary>
    public void ResetCamera()
    {
        ballToTrack = null;

        if (playerCamera != null)
        {
            SwitchCamera(playerCamera);
        }

        if (ballTrackingCancellation != null && !ballTrackingCancellation.IsCancellationRequested)
        {
            ballTrackingCancellation.Cancel();
            ballTrackingCancellation.Dispose();
            ballTrackingCancellation = new CancellationTokenSource();
        }

        // カメラがリセットされたことをPlayerCOntrollerに通知
        OnCameraReset?.Invoke();
    }

    public void SwitchToDefeatCamera(Transform defeatedBoss)
    {
        if (defeatedBoss != null)
        {
            defeatMoveCamera.LookAt = defeatedBoss;

            SwitchCamera(defeatMoveCamera);
        }
    }

    public void PlayFinishingBlowEffect(Transform ball)
    {
        StartTracking(ball);
    }

    // オブジェクト破棄時にイベントの購読を解除
    private void OnDestroy()
    {
        if (batController != null)
        {
            batController.OnStanceChanged -= HandleStanceChange;
        }
    }
}
