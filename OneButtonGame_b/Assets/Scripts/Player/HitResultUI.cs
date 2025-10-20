using UnityEngine;
using TMPro;

public class HitResultUI : MonoBehaviour
{
    public static HitResultUI Instance { get; private set; }

    [Header("UIパネル")]
    public GameObject resultPanel;

    [Header("テキスト要素")]
    public TextMeshProUGUI meetText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI timingText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowResult(string meet, float speedKmh, float angleDeg, string timing)
    {
        if (meetText != null)
        {
            meetText.text = meet;
        }
        if (speedText != null)
        {
            speedText.text = $"{speedKmh.ToString("F1")} km/h";
        }
        if (angleText != null)
        {
            angleText.text = $"{angleDeg.ToString("F1")} °";
        }

        if (timingText != null)
        {
            timingText.text = timing;
        }

        resultPanel.SetActive(true);
    }

    public void HideResult()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
}
