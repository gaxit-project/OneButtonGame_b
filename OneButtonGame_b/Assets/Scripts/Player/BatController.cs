using System;
using System.Collections;
using UnityEngine;

public class BatController : MonoBehaviour
{
    [Header("スイング設定")]
    [Tooltip("右打ちを基準としたスイングの角度")]
    public float swingAngle = 10f;
    public float swingSpeed = 10f;

    [Header("打席の構え設定")]
    [Tooltip("右打ちのバットの位置と角度")]
    public Transform rightHandedStance;
    [Tooltip("左打ちのバットの位置と角度")]
    public Transform leftHandedStance;

    [Header("コンポーネント")]
    PlayerController playerController;

    // 打席が変更されたことを通知するイベント
    public event Action<bool> OnStanceChanged;

    private bool isSwinging = false;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalPosition;
    public bool isRightHanded { get; private set; } = true; // true：右打ち false：左打ち
    private bool canChangeStance = true;


    void Start()
    {
        if(rightHandedStance != null)
        {
            // ゲーム開始時は右打ち
            SetStance(true);
        }
        else
        {
            Debug.Log("打席の構え(Stance)が設定されていません");
        }
    }

    void Update()
    {
        if (!canChangeStance)
        {
            return;
        }

        // 打席の切り替え
        float horizontalInput = Input.GetAxis("Horizontal");

        if(horizontalInput > 0 && !isRightHanded)
        {
            SetStance(true);
        }
        else if(horizontalInput < 0 && isRightHanded)
        {
            SetStance(false);
        }

        /*
        if (Input.GetMouseButtonDown(0))
        {
            PerformSwing();
        }
        */
    }

    public void SetInputEnabled(bool enabled)
    {
        canChangeStance = enabled;
    }

    /// <summary>
    /// 打席の構えを設定するメソッド
    /// </summary>
    void SetStance(bool isRight)
    {
        if (isSwinging) return;

        isRightHanded = isRight;

        /*
        if (isRightHanded)
        {
            transform.position = rightHandedStance.position;
            transform.rotation = rightHandedStance.rotation;
        }
        else
        {
            transform.position = leftHandedStance.position;
            transform.rotation = leftHandedStance.rotation;
        }
        */

        Transform targetStance = isRightHanded ? rightHandedStance : leftHandedStance;

        transform.position = targetStance.position;
        transform.rotation = targetStance.rotation;

        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        // 打席が変更されたことを通知
        OnStanceChanged?.Invoke(isRightHanded);
    }

    /// <summary>
    /// PlayerControllerからスイング状態の変更を受け取る
    /// </summary>
    public void SetSwingingState(bool swinging)
    {
        isSwinging = swinging;

        if (!swinging)
        {
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
        }
    }

    /*
    public void PerformSwing()
    {
        if (!isSwinging)
        {
            //StartCoroutine(Swing());
        }
    }

    /// <summary>
    /// スイングの動作を行うコルーチン
    /// </summary>
    IEnumerator Swing()
    {
        isSwinging = true;

        float currentSwingAngle = isRightHanded ? swingAngle : -swingAngle;

        // 目標の回転速度を計算
        //Quaternion targetLocalRotation = initialLocalRotation * Quaternion.Euler(0, currentSwingAngle, 0);
        Quaternion targetLocalRotation = Quaternion.Euler(transform.eulerAngles.x,transform.eulerAngles.y + currentSwingAngle, transform.eulerAngles.z);

        // バットを滑らかに回転
        while (Quaternion.Angle(transform.localRotation, targetLocalRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.localRotation,targetLocalRotation, swingSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // 元の位置に戻す
        while (Quaternion.Angle(transform.localRotation, initialLocalRotation) > 0.1f)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, initialLocalRotation, swingSpeed * Time.deltaTime);
            yield return null;
        }

        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;

        isSwinging =false;
    }
    */


}
