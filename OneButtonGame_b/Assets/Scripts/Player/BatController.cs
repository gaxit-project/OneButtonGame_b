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

    [Header("バットの角度設定")]
    public Vector3 idleRotationEuler = new Vector3(0, 0, 0);
    public Vector3 readyRotationEuler = new Vector3(0, 0, 0);
    public float readySpeed = 15f;

    [Header("コンポーネント")]
    PlayerController playerController;

    // 打席が変更されたことを通知するイベント
    public event Action<bool> OnStanceChanged;

    private bool isSwinging = false;
    private bool canChangeStance = true;

    private Quaternion initialLocalRotation;
    private Quaternion idleRotaion;
    private Quaternion readyRotaion;

    private Vector3 initialLocalPosition;


    public bool isRightHanded { get; private set; } = true; // true：右打ち false：左打ち


    void Start()
    {
        idleRotaion = Quaternion.Euler(idleRotationEuler);
        readyRotaion = Quaternion.Euler(readyRotationEuler);
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

        Transform targetStance = isRightHanded ? rightHandedStance : leftHandedStance;

        transform.position = targetStance.position;
        transform.rotation = targetStance.rotation;

        initialLocalPosition = transform.localPosition;
        //initialLocalRotation = transform.localRotation;

        transform.localRotation = idleRotaion;
        initialLocalRotation = idleRotaion;

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

    public IEnumerator PrepareForSwing()
    {
        //while (Quaternion.Angle(transform.localRotation, readyRotaion) > 0.1f)
        //{
        //    transform.localRotation = Quaternion.Slerp(transform.localRotation, readyRotaion, readySpeed * Time.deltaTime);
        //    yield return null;
        //}

        transform.localRotation = readyRotaion;
        yield return null;
    }

    public IEnumerator ReturnToIdle()
    {
        //while (Quaternion.Angle(transform.localRotation, idleRotaion) > 0.1f)
        //{
        //    transform.localRotation = Quaternion.Slerp(transform.localRotation, idleRotaion, readySpeed * Time.deltaTime);
        //    yield return null;
        //}

        transform.localRotation = idleRotaion;
        yield return null;
    }
}
