using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

public class PlayerController : MonoBehaviour
{
    [Header("プレイヤー設定")]
    public float playerMaxHealth = 3f;
    private float currentHealth;
    public float moveSpeed = 5.0f; // プレイヤーの移動速度
    public float rightStanceYRotation = 0f; // 右打席の時のY軸回転
    public float leftStanceYRotation = 180f; // 左打席の時のY軸回転 
    private bool isInputEnabled = true;

    [Header("ゲームオーバー設定")]
    public string gameOverSceneName = "GameOver";

    [Header("UIコンポーネント")]
    public List<Image> healthHearts;

    [Header("コンポーネント")]
    public BatController batController; // BatControllerを参照
    public CameraController cameraController; // CameraControllerを参照
    public Transform translucentBatTransform;
    public Vector3 rightStanceTranslucentBatPosition = new Vector3(0.5f, 0, 0);
    public Vector3 leftStanceTranslucentBatPosition = new Vector3(-0.5f, 0, 0);

    public bool IsPlayerSwinging { get; private set; } = false;

    private bool hitBat = false;
    private bool canSwing = true;
    private bool isDead = false;

    private Animator animator;
    private CharacterController controller;
    private Quaternion initialSwingRotation; // スイング開始時の回転を保持

    private CancellationTokenSource swingCancellation;


    void Start()
    {
        currentHealth = playerMaxHealth;
        UpdateHealthUI();

        animator = GetComponentInChildren<Animator>();
        if(animator == null ) animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

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
        if (isInputEnabled)
        {
            float x = Input.GetAxis("Horizontal");

            Vector3 move = Vector3.right * x;
            if (move.magnitude > 1f)
            {
                move.Normalize();
            }

            controller.Move(move * moveSpeed * Time.deltaTime);

            float animationSpeed = Mathf.Abs(x);
            if (animator != null) animator.SetFloat("moveSpeed", animationSpeed);

            if (Input.GetButtonDown("Fire1")) TriggerHit();
        }
        else
        {
            if (animator != null) animator.SetFloat("moveSpeed", 0f);
        }
    }

    // プレイヤーがX軸方向に動かないように補正
    private void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        currentPosition.y = 0f;
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
        isInputEnabled = enabled;
    }

    public bool IsInputEnabled => isInputEnabled;

    public void SetAnimationStance(bool isRight)
    {
        if (animator != null) animator.SetBool("isRightStance", isRight);
    }

    public void TriggerHit()
    {
        if (animator != null && isInputEnabled) animator.SetTrigger("Hit"); 
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
    /// BatControllerから打席変更の通知を受け取ってプレイヤーの向きを変える
    /// </summary>
    /// <param name="isRightHanded">右打ちならtrue</param>
    private void HandlePlayerStanceChange(bool isRightHanded)
    {
        if (IsPlayerSwinging) return;

        if (isRightHanded)
        {
            transform.rotation = Quaternion.Euler(0, rightStanceYRotation, 0);

            if (translucentBatTransform != null)
            {
                translucentBatTransform.localPosition = rightStanceTranslucentBatPosition;
            }
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, leftStanceYRotation, 0);

            if (translucentBatTransform != null)
            {
                translucentBatTransform.localPosition = leftStanceTranslucentBatPosition;
            }
        }
    }

    private void UpdateHealthUI()
    {
        for (int i = 0; i < healthHearts.Count; i++)
        {
            if (i < currentHealth)
            {
                healthHearts[i].enabled = true;
            }
            else
            {
                healthHearts[i].enabled = false;
            }
        }
    }

    public void TakePlayerDamage(int damage)
    {
        if (isDead)
        {
            SetInputEnabled(false);
            return;
        }

        currentHealth -= damage;
        Debug.Log("プレイヤーがダメージを受けた！　残りHP：" + currentHealth);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            isDead = true;
            Debug.Log("ゲームオーバー");

            SceneManager.LoadScene(gameOverSceneName);
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

        swingCancellation?.Cancel();
        swingCancellation?.Dispose();
    }
}
