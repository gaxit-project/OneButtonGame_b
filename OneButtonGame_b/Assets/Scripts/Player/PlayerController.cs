using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("プレイヤー設定")]
    public float moveSpeed = 5.0f;
    public float rightStanceYRotation = 0f;
    public float leftStanceYRotation = 180f;


    [Header("コンポーネント")]
    public BatController batController;
    public CameraController cameraController;

    private bool hitBat = false;
    private bool isPlayerSwinging = false;
    private Quaternion initialSwingRotation;

    void Start()
    {
        if (batController != null)
        {
            batController.OnStanceChanged += HandlePlayerStanceChange;

            HandlePlayerStanceChange(batController.isRightHanded);
        }

        if (cameraController != null)
        {
            cameraController.OnCameraReset += HandleCameraReset;
        }
    }

    void Update()
    { 
        if (!isPlayerSwinging)
        {
            // 水平方向の移動処理
            float horizontalInput = Input.GetAxis("Horizontal");
            Vector3 movement = Vector3.right * horizontalInput * moveSpeed * Time.deltaTime;
            transform.Translate(-movement);
        }

        if ((Input.GetMouseButtonDown(0) || Input.GetButtonDown("Fire1")) && !isPlayerSwinging)
        {
            StartCoroutine(SwingAction());
        }
    }

    private void LateUpdate()
    {
        Vector3 currentPosition = transform.position;

        currentPosition.z = 0f;

        transform.position = currentPosition;
    }

    private IEnumerator SwingAction()
    {
        hitBat = false;
        isPlayerSwinging = true;
        batController.SetSwingingState(true);
        //batController.PerformSwing();

        initialSwingRotation = transform.rotation;

        Quaternion initialRotation = transform.rotation;

        float rotationAmount = batController.isRightHanded ? batController.swingAngle : -batController.swingAngle;
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, rotationAmount, 0);

        // 回転
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, batController.swingSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        if (!hitBat)
        {
            Debug.Log("空振り");

            Ball currentBall = FindObjectOfType<Ball>();
            if (currentBall != null)
            {
                currentBall.HideMarker();
            }

            StartCoroutine(ResetStance());
        }

        /*
        if (!cameraController.useHomerunView)
        {

            // 戻る
            while (Quaternion.Angle(transform.rotation, initialRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, batController.swingSpeed * Time.deltaTime);
                yield return null;
            }

            transform.rotation = initialRotation;

            batController.SetSwingingState(false);
            isPlayerSwinging = false;
        }
        */
         
    }

    public void NotifyHit()
    {
        hitBat = true;
    }

    private void HandleCameraReset()
    {
        if (hitBat)
        {
            StartCoroutine(ResetStance());
        }
    }

    private IEnumerator ResetStance()
    {
        // 戻る
        while (Quaternion.Angle(transform.rotation, initialSwingRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, initialSwingRotation, batController.swingSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = initialSwingRotation;

        batController.SetSwingingState(false);
        isPlayerSwinging = false;
    }

    /// <summary>
    /// BatControllerから打席変更の通知を受け取ってプレイヤーの向きを変える
    /// </summary>
    /// <param name="isRightHanded">右打ちならtrue</param>
    private void HandlePlayerStanceChange(bool isRightHanded)
    {
        if (isPlayerSwinging) return;

        if (isRightHanded)
        {
            transform.rotation = Quaternion.Euler(0, rightStanceYRotation, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, leftStanceYRotation, 0);
        }
    }

    private void OnDestroy()
    {
        if (batController != null)
        {
            batController.OnStanceChanged -= HandlePlayerStanceChange;
        }

        if (cameraController != null)
        {
            cameraController.OnCameraReset -= HandleCameraReset;
        }
    }
}
