using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    // �ǂ�������Ώ�
    public Transform playerTarget;
    private Transform ballTarget;

    // ���グ�J�����̃I�t�Z�b�g
    public Vector3 lookUpOffset = new Vector3(0, -2f, -4f);

    // �ǐՃJ�����̃I�t�Z�b�g
    public Vector3 followOffset = new Vector3(0, 5f, 10f);

    // ���グ����ǐՂֈڍs���鎞��
    public float transitionDuration = 1.5f;

    // �J�������^�[�Q�b�g�������Ƃ��̊��炩��
    public float rotationSmoothness = 5f;

    // ��ԊǗ�
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
