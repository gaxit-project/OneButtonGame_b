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

    // ���̃I�u�W�F�N�g�ƏՓ˂������ɌĂяo�����
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bat"))
        {
            // �Փ˂����ʒu�⑊��̑��x���擾
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
                // ���݂̑��x����x���Z�b�g
                rb.velocity = Vector3.zero;
                // �V���������ɗ͂�������
                rb.AddForce(hitDirection * hitPower, ForceMode.Impulse);
            }
        }
    }
}