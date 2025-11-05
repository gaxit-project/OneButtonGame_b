using UnityEngine;
using Cinemachine;
using System;
using DG.Tweening;
using System.Threading;
using Cysharp.Threading.Tasks;

public class CameraController : MonoBehaviour
{
    [Header("追跡ターゲット")]
    // 追いかける対象
    public Transform playerTarget;
    private Transform ballTarget;

    [Header("コンポーネント")]
    public BatController batController;

    [Header("カメラシェイク")]
    public CinemachineImpulseSource impulseSource;

    [Header("Cinemachineカメラ")]
    public CinemachineVirtualCamera playerCamera;
    public CinemachineVirtualCamera ballCamera;
    public CinemachineVirtualCamera defeatMoveCamera;
    public CinemachineVirtualCamera allViewCamera;
    public CinemachineVirtualCamera introCamera;
    public CinemachineVirtualCamera middleViewCamera;
    public CinemachineVirtualCamera orbitCamera;

    [Header("カメラの挙動")]
    public float followSmoothness = 1f;
    public float transitionDuration = 1.5f; // 見上げから追跡へ移行する時間
    public float rotationSmoothness = 5f; // カメラがターゲットを向くときの滑らかさ

    [Header("カメラ切り替え設定")]
    public bool ballTracking = false;

    private CinemachineBrain brain;
    private CinemachineVirtualCamera currentActiveCamera;
    private Transform ballToTrack = null;
    private CancellationTokenSource ballTrackingCancellation;

    public event Action OnCameraReset;

    void Start()
    {
        brain = GetComponent<CinemachineBrain>();
        if(brain == null)
        {
            Debug.LogError("Main CameraにCinemaChineBrainが見つかりません。");
        }

        InitializeCameras();

        if (playerCamera != null)
        {

            SwitchCamera(playerCamera);
        }
        else if (allViewCamera != null)
        {
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
        SetPriority(middleViewCamera, 0);
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

    public void StartMiddleViewMovie()
    {
        SwitchCamera(middleViewCamera);
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

    public async UniTask SwitchToDefeatCamera(Transform defeatedBoss)
    {
        if (defeatedBoss != null)
        {
            defeatMoveCamera.LookAt = defeatedBoss;

            defeatMoveCamera.Follow = defeatedBoss;

            var transposer = defeatMoveCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (transposer != null)
            {
                transposer.m_TrackedObjectOffset = new Vector3(0, 300, -10);
                transposer.m_CameraDistance = 15;
            }
            else
            {
                Debug.Log("defeatMoveCameraのBodyがTransposerに設定されていません。");
            }

            if (ballCamera != null)
            {
                SetPriority(ballCamera, 0);
            }

            SwitchCamera(defeatMoveCamera);

            if(brain != null && brain.m_DefaultBlend.m_Time > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(brain.m_DefaultBlend.m_Time), cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse();
            }
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
