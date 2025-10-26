using UnityEngine;

public class FixedRotation : MonoBehaviour
{
    [Header("コンポーネント")]
    public BatController batController;
    public Transform translucentBat;
    public Vector3 rightStanceObjectYRotation = new Vector3(0, 90, 0);
    public Vector3 leftStanceObjectYRotation = new Vector3(0, -90, 0);

    private Quaternion rightRotation;
    private Quaternion leftRotation;

    private Quaternion fixedRotation;

    private void Awake()
    {
        rightRotation = Quaternion.Euler(rightStanceObjectYRotation);
        leftRotation = Quaternion.Euler(leftStanceObjectYRotation);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (batController.isRightHanded)
        {
            translucentBat.transform.rotation = rightRotation;
        }
        else
        {
            translucentBat.transform.rotation = leftRotation;
        }
    }
}
