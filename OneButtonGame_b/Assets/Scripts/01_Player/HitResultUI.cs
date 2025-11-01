using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Reflection.Emit;

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

    [Header("表示設定")]
    public float displayDuration = 2.0f;

    private CancellationTokenSource cts;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            cts = new CancellationTokenSource();
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

    public async void ShowResult(string meet, float speedKmh, float angleDeg, string timing)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
        var token = cts.Token;
        

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
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(displayDuration), ignoreTimeScale: true, cancellationToken: token);

            HideResult();
        }
        catch (OperationCanceledException)
        {

        }
    }

    public void HideResult()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();

        if(Instance != null)
        {
            Instance = null;        }
    }
}
