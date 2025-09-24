using UnityEngine;

public class Ball : MonoBehaviour
{
    public float hitPower = 50f;
    public float upwardModifier = 0.5f;

    // ���̒l���Y�����̗͂��傫���ƃt���C����ɂȂ�
    public float flyThreshold = 0.4f;
    // ���̒l���Z�����̗͂��������Ɛ^��ɑł��グ���t���C����ɂȂ�
    public float verticalFlyThreshold = 0.15f;

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

    // ���̃I�u�W�F�N�g�ƏՓ˂������ɌĂяo�����
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bat"))
        {
            // �Փ˂����ʒu�⑊��̑��x���擾
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
                // ���݂̑��x����x���Z�b�g
                rb.velocity = Vector3.zero;
                // �V���������ɗ͂�������
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

        if (collision.gameObject.CompareTag("Ground"))
        {
            if(CameraController != null)
            {
                CameraController.ResetCamera();
            }
        }
    }
}