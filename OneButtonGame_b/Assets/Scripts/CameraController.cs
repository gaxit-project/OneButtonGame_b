using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    // 追いかける対象
    private Transform target;

    // 見上げカメラのオフセット
    public Vector3 lookUpOffset = new Vector3(0, -2f, -4f);

    // 追跡カメラのオフセット
    public Vector3 followOffset = new Vector3(0, 5f, 10f);

    // 見上げから追跡へ移行する時間
    public float transitionDuration = 1.5f;

    // カメラがターゲットを向くときの滑らかさ
    public float rotationSmoothness = 5f;

    // 状態管理
    private bool isTracking = false;
    private float transitionTimer = 0f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if(isTracking && target != null)
        {
            if(transitionTimer < transitionDuration)
            {
                transitionTimer += Time.deltaTime;
            }

            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            Vector3 currentOffset = Vector3.Lerp(lookUpOffset, followOffset, t);
            Vector3 desiredPosition = target.position + currentOffset;
            transform.position = desiredPosition;

            Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothness * Time.deltaTime);
        }
    }

    public void StartTracking(Transform ballTransform, bool isFry)
    {
        target = ballTransform;
        isTracking = true;

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
        isTracking = false;
        target = null;
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
}
