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

    [Header("状態管理")]
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    private float currentHealth;
    private bool isDestroyed = false;

    private CancellationTokenSource blinkCts;

    void Start()
    {
        currentHealth = maxHealth;
        isDestroyed = false;
        UpdateStatusUI();
    }

    void Update()
    {
        
    }

    public void TakeCoreDamage(int damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;
        Debug.Log($"コアがダメージを受けた！　残りHP:{currentHealth}");

        UpdateStatusUI();

        if (currentHealth <= 0)
        {
            isDestroyed = true;
            Debug.Log("コアが破壊された！");

            blinkCts?.Cancel();
            blinkCts?.Dispose();

            SceneManager.LoadScene(gameOverSceneName);
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

    private void OnDestroy()
    {
        blinkCts?.Cancel();

    }
}
