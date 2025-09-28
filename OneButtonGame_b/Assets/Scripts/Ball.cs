using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour
{
    [Header("攻撃力")]
    public int attackPower = 100;

    [Header("打撃設定")]
    public float hitPower = 50f;
    public float upwardModifier = 0.5f;
    public float horizontalControl = 2.0f;

    [Header("カメラリセット")]
    // 地面に触れてからカメラが戻るまでの時間
    public float resetDelay = 1.0f;

    private float touchGround = 0;
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
        // 衝突前の速度を保持
        lastVelocity = rb.velocity;
    }

    // 他のオブジェクトと衝突した時に呼び出される
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bat"))
        {
            isGraunded = false;

            /*
            // 衝突した位置や相手の速度を取得
            ContactPoint contact = collision.contacts[0];
            Vector3 contactNormal = contact.normal;

            // 衝突面の法線ベクトルを使って反射ベクトルを計算
            Vector3 reflectedDirection = Vector3.Reflect(lastVelocity, contactNormal);

            Vector3 hitDirection = new Vector3(reflectedDirection.x, upwardModifier, reflectedDirection.z).normalized;

            // 打球が必ず前に飛ぶように補正
            if(hitDirection.z < 0)
            {
                hitDirection.z = -hitDirection.z;
            }
            */

            float horizontalDifference = transform.position.z - collision.transform.position.z;
            float horizontalForce = -horizontalDifference * horizontalControl;

            Vector3 hitDirection = new Vector3(horizontalForce, upwardModifier, 1).normalized;

            if (rb != null)
            {
                // 現在の速度を一度リセット
                rb.velocity = Vector3.zero;
                // 新しい方向に力を加える
                rb.AddForce(hitDirection * hitPower, ForceMode.Impulse);

                
                if(CameraController != null)
                {
                    CameraController.StartTracking(transform);
                }
            }

        }
        else if (collision.gameObject.CompareTag("Ground") && !isGraunded)
        {
            touchGround++;
            if (CameraController != null && touchGround >= 3)
            {
                isGraunded = true;
                StartCoroutine(ResetCameraAfterDelay());
                Destroy(gameObject);
            }
        }
        else if (collision.gameObject.CompareTag("Wall"))
        {
            if (CameraController != null)
            {
                isGraunded = true;
                StartCoroutine(ResetCameraAfterDelay());
            }
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Boss"))
        {
            BossController boss = collision.gameObject.GetComponent<BossController>();

            if(boss != null)
            {
                boss.TakeDamage(attackPower);
            }

            if (CameraController != null)
            {
                isGraunded = true;
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Strike"))
        {
            Debug.Log("ストライク");
            if (CameraController != null && !isGraunded)
            {
                isGraunded = true;
                StartCoroutine(ResetCameraAfterDelay());
            }

            Destroy(gameObject);
        }
        else if (other.gameObject.CompareTag("Foul"))
        {
            Debug.Log("ファール");
            if (CameraController != null && !isGraunded)
            {
                isGraunded = true;
                StartCoroutine(ResetCameraAfterDelay());
            }
        }
    }

    // 指定した時間だけ待ってから処理を再開するコルーチン
    IEnumerator ResetCameraAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);

        if(CameraController != null)
        {
            CameraController.ResetCamera();
        }
        Destroy(gameObject);
    }
}