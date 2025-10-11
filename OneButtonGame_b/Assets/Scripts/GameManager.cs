using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UIコンポーネント")]
    public TextMeshProUGUI timerText;

    [Header("リザルトシーン名")]
    public string resultSceneName = "Result";

    private int remainingBosses;
    private float elapsedTime;
    private bool isGameActive = false;

    private PlayerController playerController;
    private CanonController canonController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        BossController[] allBosses = FindObjectsOfType<BossController>();
        remainingBosses = allBosses.Length;

        if (remainingBosses > 0)
        {
            isGameActive = true;
            elapsedTime = 0f;
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (isGameActive)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void BossDefeated()
    {
        remainingBosses--;

        if (remainingBosses <= 0 && isGameActive)
        {
            isGameActive = false;

            if (DataLogger.Instance != null)
            {
                DataLogger.Instance.LogClearTime(elapsedTime);
            }

            AudioController.instance.ToResult();
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(elapsedTime);
            timerText.text = timeSpan.ToString(@"mm\:ss\.ff");
        }
    }

    public float GetClearTime()
    {
        return elapsedTime;
    }
}
