using System.Collections;
using UnityEngine;

public class BatController : MonoBehaviour
{
    [Header("スイング設定")]
    public float swingAngle = 10f;
    public float swingSpeed = 10f;

    private bool isSwinging = false;
    private Quaternion initialRotation;

    void Start()
    {
       initialRotation = transform.rotation; 
    }

    void Update()
    {
        if(Input.GetMouseButton(0) && !isSwinging)
        {
            StartCoroutine(Swing());
        }
    }

    // スイングの動作を行うコルーチン
    IEnumerator Swing()
    {
        isSwinging = true;

        // 回転速度を計算
        Quaternion targetRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + swingAngle, transform.eulerAngles.z);

        // バットを滑らかに回転
        while(Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation,targetRotation, swingSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        // 元の位置に戻す
        while (Quaternion.Angle(transform.rotation, initialRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, initialRotation, swingSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = initialRotation;

        isSwinging =false;
    }
}
