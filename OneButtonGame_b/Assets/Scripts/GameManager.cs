using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("リザルトシーン名")]
    public string resultSceneName = "Result";

    private int remainingBosses;
    private float elapsedTime;
    private bool isGameActive = false;

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

            SceneManager.LoadScene(resultSceneName);
        }
    }
}
