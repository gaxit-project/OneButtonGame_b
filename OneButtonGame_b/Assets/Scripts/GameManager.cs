using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UIコンポーネント")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI countdownText;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Start is called before the first frame update
    void Start()
    {
        
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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Batting")
        {
            InitializeGame();
        }
    }

    private void InitializeGame()
    {
        isGameActive = false;
        elapsedTime = 0f;

        playerController = FindObjectOfType<PlayerController>();
        canonController = FindObjectOfType<CanonController>();

        GameObject countdownUIObject = GameObject.FindGameObjectWithTag("CountdownText");
        if(countdownUIObject != null ) countdownText = countdownUIObject.GetComponent<TextMeshProUGUI>();

        GameObject timerUIObject = GameObject.FindGameObjectWithTag("TimerText");
        if( timerUIObject != null ) timerText = timerUIObject.GetComponent<TextMeshProUGUI>();

        BossController[] allBosses = FindObjectsOfType<BossController>();
        remainingBosses = allBosses.Length;

        if (timerText != null) timerText.text = "00:00.00";

        StartCoroutine(CountdownCoroutine());
    }

    /// <summary>
    /// カウントダウン
    /// </summary>
    private IEnumerator CountdownCoroutine()
    {
        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }
        if (canonController != null)
        {
            canonController.SetFiringEnabled(false);
        }

        countdownText.gameObject.SetActive(true);

        countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownText.text = "START!";

        isGameActive = true;
        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }
        if (canonController != null)
        {
            canonController.FireFirstBall();
        }

        yield return new WaitForSeconds(0.5f);
        countdownText.gameObject.SetActive(false);
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
