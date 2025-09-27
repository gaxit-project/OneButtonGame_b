using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    // 追いかける対象
    public Transform playerTarget;
    private Transform ballTarget;

    // 見上げカメラのオフセット
    public Vector3 lookUpOffset = new Vector3(0, -2f, -4f);

    // 追跡カメラのオフセット
    public Vector3 followOffset = new Vector3(0, 5f, 10f);

    // 見上げから追跡へ移行する時間
    public float transitionDuration = 1.5f;

    // カメラがターゲットを向くときの滑らかさ
    public float rotationSmoothness = 5f;

    // 状態管理
    private bool isTrackingBall = false;
    private float transitionTimer = 0f;

    private Vector3 cameraOffset;
    private Quaternion initialRotation;

    void Start()
    {
        cameraOffset = transform.position - playerTarget.position;
        initialRotation = transform.rotation;
    }

    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if(isTrackingBall && ballTarget != null)
        {
            if(transitionTimer < transitionDuration)
            {
                transitionTimer += Time.deltaTime;
            }

            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            Vector3 currentOffset = Vector3.Lerp(lookUpOffset, followOffset, t);
            Vector3 desiredPosition = ballTarget.position + currentOffset;
            transform.position = desiredPosition;

            Quaternion ballTargetRotation = Quaternion.LookRotation(ballTarget.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, ballTargetRotation, rotationSmoothness * Time.deltaTime);
        }
        else if (playerTarget != null)
        {
            Vector3 desiredPosition = playerTarget.position + cameraOffset;
            transform.position = desiredPosition;
            transform.rotation = initialRotation;
        }
    }

    public void StartTracking(Transform ballTransform, bool isFry)
    {
        ballTarget = ballTransform;
        isTrackingBall = true;

        if (isFry)
        {
            transitionTimer = 0f;
        }
        else
        {
            transitionTimer = transitionDuration;
        }
    }

    public void ResetCamera()
    {
        isTrackingBall = false;
        ballTarget = null;
        transform.position = cameraOffset;
        transform.rotation = initialRotation;
    }
}
