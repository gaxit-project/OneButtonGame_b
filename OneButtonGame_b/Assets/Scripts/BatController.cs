using System.Collections;
using UnityEngine;

public class BatController : MonoBehaviour
{
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

    IEnumerator Swing()
    {
        isSwinging = true;

        Quaternion targetRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + swingAngle, transform.eulerAngles.z);

        while(Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation,targetRotation, swingSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        while (Quaternion.Angle(transform.rotation, initialRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, initialRotation, swingSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = initialRotation;

        isSwinging =false;
    }
}
