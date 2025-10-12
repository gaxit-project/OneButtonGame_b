using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("プレイヤー設定")]
    public float moveSpeed = 5.0f; // プレイヤーの移動速度
    public float rightStanceYRotation = 0f; // 右打席の時のY軸回転
    public float leftStanceYRotation = 180f; // 左打席の時のY軸回転 


    [Header("コンポーネント")]
    public BatController batController; // BatControllerを参照
    public CameraController cameraController; // CameraControllerを参照

    private bool hitBat = false;
    private bool isPlayerSwinging = false;
    private bool canSwing = true;

    private Quaternion initialSwingRotation; // スイング開始時の回転を保持


    void Start()
    {
        // 打席変更イベントを購読
        if (batController != null)
        {
            batController.OnStanceChanged += HandlePlayerStanceChange;

            HandlePlayerStanceChange(batController.isRightHanded);
        }

        // CameraControllerのカメラリセットイベントを購読
        if (cameraController != null)
        {
            cameraController.OnCameraReset += HandleCameraReset;
        }
    }

    void Update()
    {
        if (!canSwing) return;

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

    // プレイヤーがZ軸方向に動かないように補正
    private void LateUpdate()
    {
        Vector3 currentPosition = transform.position;

        currentPosition.z = 0f;

        transform.position = currentPosition;
    }

    /// <summary>
    /// GameManagerからプレイヤーの操作可否を設定
    /// </summary>
    /// <param name="enabled">trueなら操作可能</param>
    public void SetInputEnabled(bool enabled)
    {
        canSwing = enabled;

        if (batController != null)
        {
            batController.SetInputEnabled(enabled);
        }
    }

    /// <summary>
    /// スイングを行うコルーチン
    /// </summary>
    private IEnumerator SwingAction()
    {
        hitBat = false;
        isPlayerSwinging = true;
        batController.SetSwingingState(true);
        //batController.PerformSwing();

        initialSwingRotation = transform.rotation; // 回転前の状態を保持

       //Quaternion initialRotation = transform.rotation;

        float rotationAmount = batController.isRightHanded ? batController.swingAngle : -batController.swingAngle;
        Quaternion targetRotation = initialSwingRotation * Quaternion.Euler(0, rotationAmount, 0);

        // 目標角度まで滑らかに回転
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, batController.swingSpeed * Time.deltaTime);
            yield return null;
        }

        // スイングの頂点で少し待機
        yield return new WaitForSeconds(0.2f);

            StartCoroutine(ResetStance());

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

    /// <summary>
    /// ヒットしたことを通知するメソッド
    /// </summary>
    public void NotifyHit()
    {
        hitBat = true;
    }

    /// <summary>
    /// カメラがリセットされた時に呼ばれる
    /// </summary>
    private void HandleCameraReset()
    {
        if (hitBat)
        {
            //StartCoroutine(ResetStance());
        }
    }

    /// <summary>
    /// スイング後、プレイヤーの向きを元に戻すコルーチン
    /// </summary>
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

    // オブジェクト破棄時にイベントの購読を解除
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
