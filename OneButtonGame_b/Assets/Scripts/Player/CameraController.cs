using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("追跡ターゲット")]
    // 追いかける対象
    public Transform playerTarget;
    private Transform ballTarget;

    [Header("カメラオフセット")]
    // 見上げカメラのオフセット
    public Vector3 lookUpOffset = new Vector3(0, -2f, -4f);

    // 追跡カメラのオフセット
    public Vector3 followOffset = new Vector3(0, 5f, 10f);

    //
    public Vector3 homerunOffset = new Vector3(0, -1f, -10f);

    [Header("カメラの挙動")]
    // 見上げから追跡へ移行する時間
    public float transitionDuration = 1.5f;

    // カメラがターゲットを向くときの滑らかさ
    public float rotationSmoothness = 5f;

    public bool useHomerunView = false;

    // 状態管理
    private bool isTrackingBall = false;
    private float transitionTimer = 0f;

    private Vector3 cameraOffset;
    private Quaternion initialRotation;

    void Start()
    {
        // プレイヤーに対するカメラの初期位置を保存
        cameraOffset = transform.position - playerTarget.position;
        initialRotation = transform.rotation;
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

                //　カメラが常にボールの方向を向くように回転
                Quaternion ballTargetRotation = Quaternion.LookRotation(ballTarget.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, ballTargetRotation, rotationSmoothness * Time.deltaTime);
            }
        }
        else if (playerTarget != null)
        {
            // カメラをプレイヤーの背後に固定
            Vector3 desiredPosition = playerTarget.position + cameraOffset;
            transform.position = desiredPosition;
            transform.rotation = initialRotation;
        }
    }

    public void StartTracking(Transform ballTransform)
    {
        ballTarget = ballTransform;
        isTrackingBall = true;
        transitionTimer = 0f;
    }

    // カメラを初期位置に戻す
    public void ResetCamera()
    {
        isTrackingBall = false;
        ballTarget = null;
        if (playerTarget != null)
        {
            transform.position = playerTarget.position + cameraOffset;
        }
        transform.rotation = initialRotation;
    }
}
