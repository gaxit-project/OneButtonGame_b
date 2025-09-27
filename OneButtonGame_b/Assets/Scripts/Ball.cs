using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour
{
    public float hitPower = 50f;
    public float upwardModifier = 0.5f;

    // この値よりY方向の力が大きいとフライ判定になる
    public float flyThreshold = 0.4f;
    // この値よりZ方向の力が小さいと真上に打ち上げたフライ判定になる
    public float verticalFlyThreshold = 0.15f;

    // 地面に触れてからカメラが戻るまでの時間
    public float resetDelay = 1.0f;

    public bool isGraunded = false;

    private Rigidbody rb;
    private Vector3 lastVelocity;
    private CameraController CameraController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        CameraController = FindObjectOfType<CameraController>();
    }

    private void FixedUpdate()
    {
        lastVelocity = rb.velocity;
    }

    // 他のオブジェクトと衝突した時に呼び出される
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bat"))
        {
            isGraunded = false;

            // 衝突した位置や相手の速度を取得
            ContactPoint contact = collision.contacts[0];
            Vector3 contactNormal = contact.normal;

            Vector3 reflectedDirection = Vector3.Reflect(lastVelocity, contactNormal);

            /*
            Vector3 upwardVector = Vector3.up * upwardModifier;

            Vector3 hitDirection = (reflectedDirection + upwardVector).normalized;
            */

            Vector3 hitDirection = new Vector3(reflectedDirection.x, upwardModifier, reflectedDirection.z).normalized;

            if(hitDirection.z < 0)
            {
                hitDirection.z = -hitDirection.z;
            }

            if (rb != null)
            {
                // 現在の速度を一度リセット
                rb.velocity = Vector3.zero;
                // 新しい方向に力を加える
                rb.AddForce(hitDirection * hitPower, ForceMode.Impulse);

                if(CameraController != null)
                {
                    bool isFry = hitDirection.y > flyThreshold;

                    if (isFry)
                    {
                        Vector2 horizontalDirection = new Vector2(hitDirection.x, hitDirection.z);

                        if(horizontalDirection.magnitude < verticalFlyThreshold)
                        {
                            isFry = false;
                        }
                    }

                    CameraController.StartTracking(transform, isFry);
                }
            }
        }

        if (collision.gameObject.CompareTag("Ground") && !isGraunded)
        {
            if(CameraController != null)
            {
                isGraunded = true;
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
    }

    IEnumerator ResetCameraAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);

        if(CameraController != null)
        {
            CameraController.ResetCamera();
        }
    }
}