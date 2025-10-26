using UnityEngine;
using UnityEngine.EventSystems;

public class SelectedFirstButton : MonoBehaviour
{
    [SerializeField]
    private GameObject firstSelectedButton;

    // Start is called before the first frame update
    void Start()
    {
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
