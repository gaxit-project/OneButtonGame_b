using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Threading;

public class CoreController : MonoBehaviour
{
    [Header("コア設定")]
    public float maxHealth = 5f;
    public string gameOverSceneName = "GameOverScene";

    [Header("UIコンポーネント")]
    public TextMeshProUGUI statusText;
    public Image coreDamageOverlay;

    [Header("ダメージ演出")]
    public float flashDuration = 0.5f;
    public Color damageFlashColor = new Color(1f, 1f, 1f, 0.392f); 

    [Header("状態管理")]
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    private float currentHealth;
    private bool isDestroyed = false;

    private CancellationTokenSource blinkCts;
    private CancellationTokenSource flashCts;

    void Start()
    {
        currentHealth = maxHealth;
        isDestroyed = false;
        UpdateStatusUI();

        flashCts = new CancellationTokenSource();
        if(coreDamageOverlay != null)
        {
            coreDamageOverlay.enabled = false;
            coreDamageOverlay.color = Color.clear;
        }
    }

    void Update()
    {
        
    }

    public void TakeCoreDamage(int damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;
        Debug.Log($"コアがダメージを受けた！　残りHP:{currentHealth}");

        if(coreDamageOverlay != null)
        {
            flashCts?.Cancel();
            flashCts?.Dispose();
            flashCts = new CancellationTokenSource();
            FlashCoreDamageEffectAsync(flashCts.Token).Forget();
        }

        UpdateStatusUI();

        if (currentHealth <= 0)
        {
            isDestroyed = true;
            Debug.Log("コアが破壊された！");

            blinkCts?.Cancel();
            blinkCts?.Dispose();

            AudioController.instance.ToGameOver();
            //SceneManager.LoadScene(gameOverSceneName);
        }
    }

    private void UpdateStatusUI()
    {
        if (statusText == null) return;

        blinkCts?.Cancel();
        blinkCts?.Dispose();
        blinkCts = new CancellationTokenSource();

        statusText.enabled = true;

        float healthPercent = currentHealth / maxHealth;

        if (currentHealth <= 0)
        {
            statusText.text = "機能停止";
            statusText.color = Color.black;
        }
        else if (healthPercent <= 0.4f)
        {
            statusText.text = "危険   ";
            statusText.color = dangerColor;
            BlinkEffect(blinkCts.Token).Forget();
        }
        else if (healthPercent <= 0.8f)
        {
            statusText.text = "警告";
            statusText.color = warningColor;
        }
        else
        {
            statusText.text = "正常";
            statusText.color = normalColor;
        }
    }

    private async UniTask BlinkEffect(CancellationToken token)
    {
        float blinkSpeedSeconds = 0.3f;
        try
        {
            while (!token.IsCancellationRequested)
            {
                statusText.enabled = !statusText.enabled;

                await UniTask.Delay(
                    System.TimeSpan.FromSeconds(blinkSpeedSeconds),
                    ignoreTimeScale: false,
                    cancellationToken: token
                );
            }
        }
        catch (OperationCanceledException)
        {
            if (statusText != null)
            {
                statusText.enabled = true;
            }
        }
    }

    private async UniTaskVoid FlashCoreDamageEffectAsync(CancellationToken token)
    {
        try
        {
            coreDamageOverlay.enabled = true;
            coreDamageOverlay.color = damageFlashColor;

            float elapsedTime = 0f;
            Color startColor = damageFlashColor;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);

            while(elapsedTime < flashDuration)
            {
                token.ThrowIfCancellationRequested();

                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / flashDuration);

                coreDamageOverlay.color = Color.Lerp(startColor, endColor, progress);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            coreDamageOverlay.color = Color.clear;
            coreDamageOverlay.enabled = false;
        }
        catch (OperationCanceledException)
        {
            if(coreDamageOverlay != null)
            {
                coreDamageOverlay.color = Color.clear;
                coreDamageOverlay.enabled = false;
            }
        }
    }

    private void OnDestroy()
    {
        blinkCts?.Cancel();

        flashCts?.Cancel();
        flashCts?.Dispose();
    }
}
