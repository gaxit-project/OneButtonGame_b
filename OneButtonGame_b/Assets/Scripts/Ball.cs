using UnityEngine;

public class Ball : MonoBehaviour
{
    public float hitPower = 50f;
    public float upwardModifier = 0.5f;

    private Rigidbody rb;
    private Vector3 lastVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
            // 衝突した位置や相手の速度を取得
            ContactPoint contact = collision.contacts[0];
            Vector3 contactNormal = contact.normal;

            Vector3 reflectedDirection = Vector3.Reflect(lastVelocity, contactNormal);

            Vector3 upwardVector = Vector3.up * upwardModifier;

            Vector3 hitDirection = (reflectedDirection + upwardVector).normalized;

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
            }
        }
    }
}