using UnityEngine;
using System;

public class CameraController : MonoBehaviour
{
    [Header("追跡ターゲット")]
    // 追いかける対象
    public Transform playerTarget;
    private Transform ballTarget;

    [Header("コンポーネント")]
    public BatController batController;

    [Header("カメラオフセット")]
    public Vector3 rightStanceOffset; // 右打席のカメラのオフセット
    public Vector3 leftStanceOffset; // 左打席のカメラのオフセット
    public Vector3 lookUpOffset = new Vector3(0, -2f, -4f); // 見上げカメラのオフセット
    public Vector3 followOffset = new Vector3(0, 5f, 10f); // 追跡カメラのオフセット
    public Vector3 homerunOffset = new Vector3(0, -1f, -10f); // 打球のカメラのオフセット

    [Header("カメラの挙動")]
    public float followSmoothness = 1f;
    public float transitionDuration = 1.5f; // 見上げから追跡へ移行する時間
    public float rotationSmoothness = 5f; // カメラがターゲットを向くときの滑らかさ
    public bool useHomerunView = false;

    // 状態管理
    private bool isTrackingBall = false;
    private float transitionTimer = 0f;

    public event Action OnCameraReset;

    private Vector3 currentOffset;
    private Quaternion initialRotation;

    void Start()
    {
        // プレイヤーに対するカメラの初期位置を保存
        rightStanceOffset = transform.position - playerTarget.position;
        leftStanceOffset = new Vector3(-rightStanceOffset.x, rightStanceOffset.y, rightStanceOffset.z);
        initialRotation = transform.rotation;

        if (batController != null)
        {
            batController.OnStanceChanged += HandleStanceChange;

            HandleStanceChange(batController.isRightHanded);
        }
        else
        {
            currentOffset = rightStanceOffset;
        }
    }

    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (isTrackingBall && ballTarget != null)
        {
            if (useHomerunView)
            {
                Vector3 desirdPosition = playerTarget.position + homerunOffset;
                transform.position = desirdPosition;
                transform.LookAt(ballTarget);
            }
            else
            {
                // 時間をかけてカメラの移動を滑らかにする
                if (transitionTimer < transitionDuration)
                {
                    transitionTimer += Time.deltaTime;
                }

                // ボールを追跡
                float t = Mathf.Clamp01(transitionTimer / transitionDuration);
                Vector3 currentOffset = Vector3.Lerp(lookUpOffset, followOffset, t);
                Vector3 desiredPosition = ballTarget.position + currentOffset;
                transform.position = desiredPosition;
                //transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothness * Time.deltaTime);

                //　カメラが常にボールの方向を向くように回転
                Quaternion ballTargetRotation = Quaternion.LookRotation(ballTarget.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, ballTargetRotation, rotationSmoothness * Time.deltaTime);
            }
        }
        else if (playerTarget != null)
        {
            // カメラをプレイヤーの背後に固定
            Vector3 desiredPosition = playerTarget.position + currentOffset;
            transform.position = desiredPosition;
            //transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothness * Time.deltaTime);
            transform.rotation = initialRotation;
        }
    }

    public void StartTracking(Transform ballTransform)
    {
        ballTarget = ballTransform;
        isTrackingBall = true;
        transitionTimer = 0f;
    }

    private void HandleStanceChange(bool isRightHanded)
    {
        if (isRightHanded)
        {
            currentOffset = rightStanceOffset;
        }
        else
        {
            currentOffset = leftStanceOffset;
        }
    }

    // カメラを初期位置に戻す
    public void ResetCamera()
    {

        isTrackingBall = false;
        ballTarget = null;

        if (playerTarget != null)
        {
            transform.position = playerTarget.position + currentOffset;
        }

        transform.rotation = initialRotation;
        OnCameraReset?.Invoke();
    }

    private void OnDestroy()
    {
        if (batController != null)
        {
            batController.OnStanceChanged -= HandleStanceChange;
        }
    }
}
