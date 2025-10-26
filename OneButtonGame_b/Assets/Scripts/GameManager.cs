using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGameActive { get; private set; } = false;

    [Header("UIコンポーネント")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI countdownText;
    public GameObject playerHealthPanel;
    public GameObject BossHpPanel;
    public TextMeshProUGUI coreStatusText;
    public TextMeshProUGUI fpsText;
    public GameObject bossExplanationPanel;
    public TextMeshProUGUI bossExplanationText;
    public GameObject coreExplanationPanel;
    public TextMeshProUGUI coreExplanationText;


    [Header("リザルトシーン名")]
    public string resultSceneName = "Result";

    [Header("ボス攻撃設定")]
    public float bossAttackInterval = 3.0f;

    [Header("ゲーム設定")]
    public float allViewMovieDuration = 2.0f;
    public float introMovieDuration = 3.0f;
    public float orbitMovieDuration = 5.0f;
    public float bossExplanationTextDuration = 4.0f;
    public float coreExplanationTextDuration = 4.0f;

    [Header("デバッグ用")]
    public bool movieSkip = false;

    private string bossFullExplanationText = "";
    private string coreFullExplanationText = "";

    private float elapsedTime;

    private PlayerController playerController;
    private CanonController canonController;
    private CameraController cameraController;

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

        // コンポーネントの取得
        playerController = FindObjectOfType<PlayerController>();
        canonController = FindObjectOfType<CanonController>();
        cameraController = FindObjectOfType<CameraController>();

        if(playerHealthPanel != null)
        {
            GameObject playerHealthPanelObject = GameObject.FindGameObjectWithTag("PlayerHealthPanel");
            if (playerHealthPanelObject != null) playerHealthPanel = playerHealthPanelObject;
        }

        if(BossHpPanel != null)
        {
            GameObject bossHpPanelObject = GameObject.FindGameObjectWithTag("BossHpPanel");
            if (bossHpPanelObject != null) BossHpPanel = bossHpPanelObject;
        }

        if(coreStatusText != null)
        {
            GameObject coreStatusTextObject = GameObject.FindGameObjectWithTag("CoreStatusText");
            if (coreStatusTextObject != null) coreStatusText = coreStatusTextObject.GetComponent<TextMeshProUGUI>();
        }

        if(fpsText != null)
        {
            GameObject fpsTextObject = GameObject.FindGameObjectWithTag("FPSText");
            if (fpsTextObject != null) fpsText = fpsTextObject.GetComponent<TextMeshProUGUI>();
        }

        GameObject countdownUIObject = GameObject.FindGameObjectWithTag("CountdownText");
        if(countdownUIObject != null ) countdownText = countdownUIObject.GetComponent<TextMeshProUGUI>();

        GameObject timerUIObject = GameObject.FindGameObjectWithTag("TimerText");
        if( timerUIObject != null ) timerText = timerUIObject.GetComponent<TextMeshProUGUI>();

        BossController[] allBosses = FindObjectsOfType<BossController>();
        activeBosses = new List<BossController>(allBosses);

        if (timerText != null) timerText.text = "00:00.00";

        SetGameUIActive(false);

        if(countdownText != null) countdownText.gameObject.SetActive(false);
        if (bossExplanationPanel != null) bossExplanationPanel.SetActive(false);
        if (coreExplanationPanel != null) coreExplanationPanel.SetActive(false);

        if (bossExplanationText != null)
        {
            bossFullExplanationText = !string.IsNullOrEmpty(bossExplanationText.text) ? bossExplanationText.text : "ボスを倒せ！！";
            bossExplanationText.gameObject.SetActive(false);
            bossExplanationText.text = bossFullExplanationText;
            bossExplanationText.maxVisibleCharacters = 0;
        }
        if (coreExplanationText != null)
        {
            coreFullExplanationText = !string.IsNullOrEmpty(coreExplanationText.text) ? coreExplanationText.text : "コアを守れ！！";
            coreExplanationText.gameObject.SetActive(false);
            coreExplanationText.text = coreFullExplanationText;
            coreExplanationText.maxVisibleCharacters = 0;
        }

        //StartCoroutine(CountdownCoroutine());
        PlayIntroSequenceAsync(gameLoopCancellationTokenSource.Token).Forget();
    }

    private void SetGameUIActive(bool isActive)
    {
        timerText.gameObject.SetActive(isActive);
        playerHealthPanel.SetActive(isActive);
        BossHpPanel.SetActive(isActive);
        coreStatusText.gameObject.SetActive(isActive);
        fpsText.gameObject.SetActive(isActive);
    }

    /// <summary>
    /// カウントダウン
    /// </summary>
    private async UniTaskVoid PlayIntroSequenceAsync(CancellationToken cancellationToken)
    {
        if (playerController != null) playerController.SetInputEnabled(false);
        if (canonController != null) canonController.SetFiringEnabled(false);

        try
        {
            // 上空から全体表示
            if (cameraController != null && allViewMovieDuration > 0 && !movieSkip)
            {
                Debug.Log("イントロムービー開始");
                cameraController.StartAllViewMovie();

                await UniTask.Delay(TimeSpan.FromSeconds(allViewMovieDuration));
            }

            // イントロムービー再生
            if (cameraController != null && introMovieDuration > 0 && !movieSkip)
            {
                Debug.Log("イントロムービー開始");
                cameraController.StartIntroMovie();

                await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: cancellationToken);
                if (bossExplanationPanel != null) bossExplanationPanel.SetActive(true);

                float textDisplayStartTime = Time.time;

                if (bossExplanationText != null && !string.IsNullOrEmpty(bossFullExplanationText))
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1.0f),cancellationToken: cancellationToken);
                    bossExplanationText.gameObject.SetActive(true);
                    int totalChars = bossFullExplanationText.Length;
                    bossExplanationText.maxVisibleCharacters = 0;

                    float charDisplayIntervalSeconds = bossExplanationTextDuration / totalChars;
                    TimeSpan interval = TimeSpan.FromSeconds(charDisplayIntervalSeconds);

                    for (int i = 0; i < totalChars; i++)
                    {
                        if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

                        bossExplanationText.maxVisibleCharacters = i + 1;
                        await UniTask.Delay(interval, cancellationToken: cancellationToken);
                    }
                    Debug.Log("説明テキスト完了");
                }

                float textDisplayElapsedTime = Time.time - textDisplayStartTime;
                float remainingOrbitTime = introMovieDuration - textDisplayElapsedTime;
                if (remainingOrbitTime > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(remainingOrbitTime), cancellationToken: cancellationToken);
                }
                else
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(introMovieDuration), cancellationToken: cancellationToken);
                }

                if (bossExplanationText != null) bossExplanationText.gameObject.SetActive(false);
                if (bossExplanationPanel != null) bossExplanationPanel.SetActive(false);
                Debug.Log("テキスト非表示");
            }

            // オービットムービー再生
            if (cameraController != null && orbitMovieDuration > 0 && !movieSkip)
            {
                Debug.Log("オービットムービー開始");
                /*
                if(coreExplanationPanel != null) coreExplanationPanel.SetActive(true);
                cameraController.StartOrbitMovie();
                await UniTask.Delay(TimeSpan.FromSeconds(orbitMovieDuration + 0.2f), cancellationToken: cancellationToken);
                if(coreExplanationPanel != null) coreExplanationPanel.SetActive(false);
                */

                cameraController.StartOrbitMovie();

                if(coreExplanationPanel != null) coreExplanationPanel.gameObject.SetActive(true);

                float textDisplayStartTime = Time.time;

                if(coreExplanationText != null && !string.IsNullOrEmpty(coreFullExplanationText))
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                    coreExplanationText.gameObject.SetActive(true);
                    int totalChars = coreFullExplanationText.Length;
                    coreExplanationText.maxVisibleCharacters = 0;

                    float charDisplayIntervalSeconds = coreExplanationTextDuration / totalChars;
                    TimeSpan interval = TimeSpan.FromSeconds(charDisplayIntervalSeconds);

                    for(int i = 0; i < totalChars; i++)
                    {
                        if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

                        coreExplanationText.maxVisibleCharacters = i + 1;
                        await UniTask.Delay(interval, cancellationToken: cancellationToken);
                    }
                    Debug.Log("説明テキスト完了");
                }

                float textDisplayElapsedTime = Time.time - textDisplayStartTime;
                float remainingOrbitTime = orbitMovieDuration - textDisplayElapsedTime;
                if(remainingOrbitTime > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(remainingOrbitTime), cancellationToken: cancellationToken);
                }

                if(coreExplanationText != null) coreExplanationText.gameObject.SetActive(false);
                if (coreExplanationPanel != null) coreExplanationPanel.gameObject.SetActive(false);
                Debug.Log("テキスト非表示");
            }

            if(movieSkip) cameraController.ResetCamera();

            // ゲームカメラに切り替え
            if (cameraController != null)
            {
                cameraController.ResetCamera();
            }

            countdownText.gameObject.SetActive(true);
            SetGameUIActive(true);

            Debug.Log("カウントダウン開始");
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

            // ゲーム開始、操作を有効化
            if (playerController != null) playerController.SetInputEnabled(true);
            if (canonController != null) canonController.SetFiringEnabled(true);

            Debug.Log("最初の発射ロックを解除しました");
            BossAttackLoopAsync(cancellationToken).Forget(); // ボスの攻撃開始

            // STARTを少しだけ表示
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
            countdownText.gameObject.SetActive(false);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("カウントダウンがキャンセルされました");
            SetGameUIActive(false);
            if (countdownText != null) countdownText.gameObject.SetActive(false);
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
