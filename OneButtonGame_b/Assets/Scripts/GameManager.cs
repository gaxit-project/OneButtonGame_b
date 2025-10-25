using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGameActive { get; private set; } = false;

    [Header("UIコンポーネント")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI countdownText;

    [Header("リザルトシーン名")]
    public string resultSceneName = "Result";

    [Header("ボス攻撃設定")]
    public float bossAttackInterval = 3.0f;

    private int remainingBosses;
    private float elapsedTime;

    private PlayerController playerController;
    private CanonController canonController;

    private List<BossController> activeBosses = new List<BossController>();
    private CancellationTokenSource gameLoopCancellationTokenSource;

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
        if (IsGameActive)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 既存のタスクをキャンセル
        gameLoopCancellationTokenSource?.Cancel();
        gameLoopCancellationTokenSource?.Dispose();
        gameLoopCancellationTokenSource = new CancellationTokenSource();

        if (scene.name == "Batting")
        {
            InitializeGame();
        }
    }

    private void InitializeGame()
    {
        IsGameActive = false;
        elapsedTime = 0f;

        playerController = FindObjectOfType<PlayerController>();
        canonController = FindObjectOfType<CanonController>();

        GameObject countdownUIObject = GameObject.FindGameObjectWithTag("CountdownText");
        if(countdownUIObject != null ) countdownText = countdownUIObject.GetComponent<TextMeshProUGUI>();

        GameObject timerUIObject = GameObject.FindGameObjectWithTag("TimerText");
        if( timerUIObject != null ) timerText = timerUIObject.GetComponent<TextMeshProUGUI>();

        BossController[] allBosses = FindObjectsOfType<BossController>();
        activeBosses = new List<BossController>(allBosses);

        if (timerText != null) timerText.text = "00:00.00";

        //StartCoroutine(CountdownCoroutine());
        CountdownCoroutineAsync(gameLoopCancellationTokenSource.Token).Forget();
    }

    /// <summary>
    /// カウントダウン
    /// </summary>
    private async UniTaskVoid CountdownCoroutineAsync(CancellationToken cancellationToken)
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

        try
        {
            countdownText.text = "3";
            //yield return new WaitForSeconds(1f);
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);

            countdownText.text = "2";
            //yield return new WaitForSeconds(1f);
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);

            countdownText.text = "1";
            //yield return new WaitForSeconds(1f);
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);

            countdownText.text = "START!";

            IsGameActive = true;

            if (playerController != null) playerController.SetInputEnabled(true);
            if (canonController != null) canonController.SetFiringEnabled(true);

            //BossController.ReleaseFireLock();
            Debug.Log("最初の発射ロックを解除しました");
            BossAttackLoopAsync(cancellationToken).Forget();

            //yield return new WaitForSeconds(0.5f);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
            countdownText.gameObject.SetActive(false);
        }
        catch(OperationCanceledException)   
        {
            Debug.Log("カウントダウンがキャンセルされました");
        }
    }

    private async UniTaskVoid BossAttackLoopAsync(CancellationToken cancellationToken)
    {
        while (IsGameActive && activeBosses.Count > 0)
        {
            try
            {
                var attackableBosses = activeBosses.Where(boss => boss != null && boss.IsAttackReady()).ToList();

                if (attackableBosses.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, attackableBosses.Count);
                    BossController selectedBoss = attackableBosses[randomIndex];

                    Debug.Log($"[{selectedBoss.gameObject.name}] を選択");

                    await selectedBoss.PerformAttackAsync(cancellationToken);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(bossAttackInterval), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("ボスの攻撃ループがキャンセルされました");
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ボスの攻撃ループ中にエラーが発生: {ex.Message}\n{ex.StackTrace}");
                await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: cancellationToken);
            }
        }
    }

    public async UniTaskVoid HandleBossDeath(BossController dyingBoss)
    {
        try
        {
            BossDefeated(dyingBoss);

            await dyingBoss.DieAsync();

            if (!IsGameActive && activeBosses.Count <= 0)
            {
                Debug.Log("最後のボスの死亡演出完了。リザルトシーンへ遷移");
                AudioController.instance.ToResult();
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"ボスの死亡処理[{dyingBoss.name}]がキャンセルされました");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ボスの死亡処理[{dyingBoss.name}]中にエラー: {ex.Message}");
        }   
    }

    public void BossDefeated(BossController defeatBoss)
    {
        if (activeBosses.Contains(defeatBoss))
        {
            activeBosses.Remove(defeatBoss);
        }

        if (activeBosses.Count <= 0 && IsGameActive)
        {
            IsGameActive = false;

            if (DataLogger.Instance != null)
            {
                DataLogger.Instance.LogClearTime(elapsedTime);
            }
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

    private void OnDestroy()
    {
        gameLoopCancellationTokenSource?.Cancel();
        gameLoopCancellationTokenSource?.Dispose();

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
