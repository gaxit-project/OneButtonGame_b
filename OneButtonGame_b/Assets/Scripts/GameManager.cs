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
    public GameObject timerPanel;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI countdownText;
    public GameObject playerHealthPanel;
    public GameObject BossHpPanel;
    public GameObject coreStatusPanel;
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
    public float gameTimeLimit = 180f;
    public float allViewMovieDuration = 2.0f;
    public float introMovieDuration = 3.0f;
    public float middleViewMovieDuration = 1.0f;
    public float orbitMovieDuration = 3.0f;
    public float bossExplanationTextDuration = 4.0f;
    public float coreExplanationTextDuration = 4.0f;

    [Header("デバッグ用")]
    public bool movieSkip = false;
    public bool Copy;

    private string bossFullExplanationText = "";
    private string coreFullExplanationText = "";

    private float remainingTime;
    private float lastElapsedTime = 0f;

    private bool isPausedForDefeat = false;

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

        DOTween.KillAll();
        CancelAndDisposeToken();
    }

    // Start is called before the first frame update
    void Start()
    {
         timerPanel = GameObject.Find("TimerPanel");
         playerHealthPanel = GameObject.Find("PlayerHearts");
         BossHpPanel = GameObject.Find("BossHP");
         coreStatusPanel = GameObject.Find("CoreStatus");
         bossExplanationPanel = GameObject.Find("");
         coreExplanationPanel = GameObject.Find("");
    }


    // Update is called once per frame
    void Update()
    {
        Copy = IsGameActive;
        if (IsGameActive)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0)
            {
                remainingTime = 0;
                IsGameActive = false;
                Debug.Log("時間切れ！ゲームオーバー");
                AudioController.instance.ToGameOver();
            }

            UpdateTimerUI();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1.0f;
        isPausedForDefeat = false;

        // 既存のタスクをキャンセル
        CancelAndDisposeToken();
        gameLoopCancellationTokenSource = new CancellationTokenSource();

        if (scene.name == "Batting")
        {
            InitializeGame();
        }
        else
        {
            ResetGameState();
        }
    }

    private void InitializeGame()
    {
        IsGameActive = false;
        isPausedForDefeat = false;
        remainingTime = gameTimeLimit;
        lastElapsedTime = 0f;
        activeBosses.Clear();

        // コンポーネントの取得
        playerController = FindObjectOfType<PlayerController>();
        canonController = FindObjectOfType<CanonController>();
        cameraController = FindObjectOfType<CameraController>();

        timerPanel = FindUIElementByTag<Transform>("TimerPanel")?.gameObject;
        timerText = FindUIElementByTag<TextMeshProUGUI>("TimerText");
        countdownText = FindUIElementByTag<TextMeshProUGUI>("CountdownText");
        playerHealthPanel = FindUIElementByTag<Transform>("PlayerHealthPanel")?.gameObject;
        BossHpPanel = FindUIElementByTag<Transform>("BossHpPanel")?.gameObject;
        coreStatusPanel = FindUIElementByTag<Transform>("CoreStatusPanel")?.gameObject;
        coreStatusText = FindUIElementByTag<TextMeshProUGUI>("CoreStatusText");
        fpsText = FindUIElementByTag<TextMeshProUGUI>("FPSText");

        bossExplanationPanel = GameObject.Find("BossExplanationPanel");
        if (bossExplanationPanel != null)
        {
            bossExplanationText = bossExplanationPanel.transform.Find("ExplanationText")?.GetComponent<TextMeshProUGUI>();
            if (bossExplanationText == null) Debug.LogWarning("Child 'ExplanationText' or its TextMeshProUGUI not found in BossExplanationPanel!");
        }
        else
        {
            Debug.LogWarning("BossExplanationPanel (GameObject by name) not found!");
            bossExplanationText = null;
        }

        coreExplanationPanel = GameObject.Find("CoreExplanationPanel");
        if (coreExplanationPanel != null)
        {
            coreExplanationText = coreExplanationPanel.transform.Find("ExplanationText")?.GetComponent<TextMeshProUGUI>();
            if (coreExplanationText == null) Debug.LogWarning("Child 'ExplanationText' or its TextMeshProUGUI not found in CoreExplanationPanel!");
        }
        else
        {
            Debug.LogWarning("CoreExplanationPanel (GameObject by name) not found!");
            coreExplanationText = null;
        }

        BossController[] allBosses = FindObjectsOfType<BossController>();
        activeBosses = new List<BossController>(allBosses);

        if (timerText != null) UpdateTimerUI();

        SetGameUIActive(false);
        SetActiveIfNotNull(countdownText?.gameObject, false);

        SetActiveIfNotNull(bossExplanationPanel, false);
        SetActiveIfNotNull(bossExplanationText?.gameObject, false);
        SetActiveIfNotNull(coreExplanationPanel, false);
        SetActiveIfNotNull(coreExplanationText?.gameObject, false);

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (bossExplanationPanel != null) bossExplanationPanel.SetActive(false);
        if (bossExplanationText != null) bossExplanationText.gameObject.SetActive(false);
        if (coreExplanationPanel != null) coreExplanationPanel.SetActive(false);
        if (coreExplanationText != null) coreExplanationText.gameObject.SetActive(false);

        if (bossExplanationText != null)
        {
            bossFullExplanationText = !string.IsNullOrEmpty(bossExplanationText.text) ? bossExplanationText.text : "ボスを倒せ！！";
            bossExplanationText.text = bossFullExplanationText;
            bossExplanationText.maxVisibleCharacters = 0;
        }
        if (coreExplanationText != null)
        {
            coreFullExplanationText = !string.IsNullOrEmpty(coreExplanationText.text) ? coreExplanationText.text : "コアを守れ！！";
            coreExplanationText.text = coreFullExplanationText;
            coreExplanationText.maxVisibleCharacters = 0;
        }

        CancelAndDisposeToken();
        gameLoopCancellationTokenSource = new CancellationTokenSource();

        //StartCoroutine(CountdownCoroutine());
        PlayIntroSequenceAsync(gameLoopCancellationTokenSource.Token).Forget();
    }

    public void ResetGameState()
    {
        CancelAndDisposeToken();

        IsGameActive = false;
        //elapsedTime = 0f;
        remainingTime = 0f;

        activeBosses.Clear();

        timerPanel = null;
        timerText = null;
        countdownText = null;
        playerHealthPanel = null;
        BossHpPanel = null;
        coreStatusPanel = null;
        coreStatusText = null;
        fpsText = null;
        bossExplanationPanel = null;
        bossExplanationText = null;
        coreExplanationPanel = null;
        coreExplanationText = null;

        playerController = null;
        canonController = null;
        cameraController = null;
    }

    private void CancelAndDisposeToken()
    {
        if(gameLoopCancellationTokenSource != null)
        {
            if (!gameLoopCancellationTokenSource.IsCancellationRequested)
            {
                gameLoopCancellationTokenSource?.Cancel();
            }
            gameLoopCancellationTokenSource?.Dispose();
            gameLoopCancellationTokenSource= null;
        }
    }

    private T FindUIElementByTag<T>(string tag) where T : Component
    {
        GameObject foundObject = null;

        try
        {
            foundObject = GameObject.FindGameObjectWithTag(tag);
        }
        catch
        {
            return null;
        }

        if (foundObject == null)
        {
            return null;
        }

        T component = foundObject.GetComponent<T>();

        if (component == null)
        {
            if (typeof(T) == typeof(Transform))
            {
                return foundObject.transform as T;
            }

            return null;
        }

         return component;
    }

    private void SetGameUIActive(bool isActive)
    {
        SetActiveIfNotNull(timerPanel, isActive);
        SetActiveIfNotNull(timerText?.gameObject, isActive);
        SetActiveIfNotNull(playerHealthPanel, isActive);
        SetActiveIfNotNull(BossHpPanel, isActive);
        SetActiveIfNotNull(coreStatusPanel, isActive);
        SetActiveIfNotNull(coreStatusText?.gameObject, isActive);
        SetActiveIfNotNull(fpsText?.gameObject, isActive);;
    }

    private void SetActiveIfNotNull(GameObject obj, bool isActive)
    {
        if (obj != null)
        {
            obj.SetActive(isActive);
        }
    }

    /// <summary>
    /// カウントダウン
    /// </summary>
    private async UniTaskVoid PlayIntroSequenceAsync(CancellationToken cancellationToken)
    {
        if (playerController != null) playerController.SetInputEnabled(false);
        if (canonController != null) canonController.SetFiringEnabled(false);

        SetGameUIActive(false);

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
            else if (movieSkip)
            {
                SetActiveIfNotNull(bossExplanationPanel, false);
                SetActiveIfNotNull(bossExplanationText?.gameObject, false);
            }

            if (cameraController != null && allViewMovieDuration > 0 && !movieSkip)
            {
                Debug.Log("イントロムービー開始");
                cameraController.StartAllViewMovie();

                await UniTask.Delay(TimeSpan.FromSeconds(allViewMovieDuration));
            }

            if(cameraController != null && middleViewMovieDuration > 0 && !movieSkip)
            {
                Debug.Log("中間ムービー");
                cameraController.StartMiddleViewMovie();
                await UniTask.Delay(TimeSpan.FromSeconds(middleViewMovieDuration), cancellationToken: cancellationToken);
            }

            // オービットムービー再生
            if (cameraController != null && orbitMovieDuration > 0 && !movieSkip)
            {
                Debug.Log("オービットムービー開始");

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

                if (coreExplanationText != null) coreExplanationText.gameObject.SetActive(false);
                if (coreExplanationPanel != null) coreExplanationPanel.gameObject.SetActive(false);
                Debug.Log("テキスト非表示");
            }
            else if (movieSkip)
            {
                SetActiveIfNotNull(coreExplanationPanel, false);
                SetActiveIfNotNull(coreExplanationPanel?.gameObject, false);
            }

            //if (movieSkip) cameraController.ResetCamera();

            

            // ゲームカメラに切り替え
            if (cameraController != null)
            {
                cameraController.ResetCamera();
            }

            SetActiveIfNotNull(bossExplanationPanel, false);
            SetActiveIfNotNull(bossExplanationText?.gameObject, false);
            SetActiveIfNotNull(coreExplanationPanel, false);
            SetActiveIfNotNull(coreExplanationText?.gameObject, false);

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
                if (this == null) return;
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
        if (gameLoopCancellationTokenSource != null && !gameLoopCancellationTokenSource.IsCancellationRequested)
        {
            gameLoopCancellationTokenSource.Cancel();
            gameLoopCancellationTokenSource.Dispose();
            gameLoopCancellationTokenSource = null;
        }

        try
        {
            BossDefeated(dyingBoss);

            await dyingBoss.DieAsync();

            if(cameraController != null)
            {
                cameraController.ResetCamera();

                await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            if (!IsGameActive && activeBosses.Count <= 0)
            {
                Debug.Log("最後のボスの死亡演出完了。リザルトシーンへ遷移");
                AudioController.instance.ToResult();
            }
            else
            {
                Debug.Log("まだボスが残っているのでゲームを再開します。");

                IsGameActive = true;

                gameLoopCancellationTokenSource = new CancellationTokenSource();
                BossAttackLoopAsync(gameLoopCancellationTokenSource.Token).Forget();
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
                float elapsedTime = remainingTime;
                lastElapsedTime = elapsedTime;
                DataLogger.Instance.LogClearTime(elapsedTime);
            }
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(remainingTime);
            //timerText.text = timeSpan.ToString(@"mm\:ss\.ff");
            timerText.text = remainingTime.ToString("F0");
        }
    }

    public float GetClearTime()
    {
        return lastElapsedTime;
    }

    private void OnDestroy()
    {
        CancelAndDisposeToken();
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance != null)
        {
            Instance = null;
        }

        DOTween.KillAll();
    }
}
