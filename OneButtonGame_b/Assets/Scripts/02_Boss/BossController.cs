using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;

public class BossController : MonoBehaviour
{
    public int CurrentHealth => currentHealth;

    [Header("ボスの体力")]
    public int maxHealth = 1000;
    public int attackPower = 100;
    public float attackCoolTime = 5f;

    private int currentHealth;

    [Header("関連オブジェクト/コンポーネント")]
    public List<Transform> associatedSpawnPoint; // 敵に対応するFirePoint
    public CanonController canonController;
    public DamageDisplay damageDisplay;
    public TextMeshProUGUI healthText;

    [Header("デバッグ用")]
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool isGettingHit = false;
                     public bool isDead = false;

    private Animator anim;
    private CameraController cameraController;
    private CancellationTokenSource bossTaskCancellation;

    //private bool isDead = false;
    public bool attack = false;

    void Awake()
    {
        // 初期化処理
        currentHealth = maxHealth;
        UpdateHealthUI();
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("Animatorが見つかりません！");
            enabled = false;
            return;
        }

        cameraController = FindObjectOfType<CameraController>();

        bossTaskCancellation = new CancellationTokenSource();
    }

    private void Start()
    {
        if (anim != null)
        {
            anim.Play("Idle");
        }
    }

    void Update()
    {

    }

    /// <summary>
    /// ダメージ処理
    /// </summary>
    public bool TakeBossDamage(int damage)
    {
        if (isDead) return false;

        bool wasAlive = currentHealth > 0;
        currentHealth -= damage;
        isGettingHit = true;

        bossTaskCancellation?.Cancel();
        bossTaskCancellation?.Dispose();
        bossTaskCancellation = new CancellationTokenSource();

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        Debug.Log($"[{gameObject.name}] {damage} ダメージ受けた！残りのHP: {currentHealth}");
        
        if (damageDisplay != null)
        {
            damageDisplay.ShowDamage(damage);
        }

        UpdateHealthUI();

        bool isFinishingBlow = wasAlive && currentHealth <= 0;

        if(currentHealth <= 0)
        {
            if (wasAlive)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.HandleBossDeath(this).Forget();
                }
                else
                {
                    DieAsync().Forget();
                }
            }
        }
        else
        {
            PlayGetHitAnimationAsync().Forget();
        }

        return isFinishingBlow;
    }

    private async UniTaskVoid PlayGetHitAnimationAsync()
    {
        if (anim == null || isDead) return;

        try
        {
            Debug.Log($"[{gameObject.name}] GetHitアニメーション再生");
            anim.Play("GetHit", -1, 0f);

            await UniTask.WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName("GetHit"), cancellationToken: bossTaskCancellation.Token);
            await UniTask.WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f, cancellationToken: bossTaskCancellation.Token);

            if (!isDead && !bossTaskCancellation.IsCancellationRequested)
            {
                anim.Play("Idle");
                isGettingHit = false;

                StartAttackCooldownTimerAsync(bossTaskCancellation.Token).Forget();
            }
        }
        catch (OperationCanceledException)
        {
            if (!isDead)
            {
                anim?.Play("Idle");
            }
        }
    }

    private async UniTaskVoid WaitForGetHitEndAsync()
    {
        try
        {
            await UniTask.WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName("GetHit"), cancellationToken: bossTaskCancellation.Token);
            await UniTask.WaitUntil(() => !anim.GetCurrentAnimatorStateInfo(0).IsName("GetHit"), cancellationToken: bossTaskCancellation.Token);

            isGettingHit = false;
            Debug.Log($"[{gameObject.name}] GetHitアニメーション終了確認");
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"[{gameObject.name}] GetHitアニメーション待機がキャンセルされました");
        }
    }

    /// <summary>
    /// 通常攻撃
    /// </summary>
    public async UniTask PerformAttackAsync(CancellationToken gameManagerToken)
    {
        if (isDead) return;

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(gameManagerToken, bossTaskCancellation.Token);
        var cancellationToken = linkedCts.Token;

        try
        {
            isGettingHit = false;
            canAttack = false;
            Debug.Log("通常攻撃開始");
            anim.Play("Attack");

            // アニメーションの特定のタイミングでボールを発射
            //await UniTask.WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName("Attack") && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f, cancellationToken: cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(1.0f), cancellationToken: cancellationToken);

            // キャンセルチェック
            if (cancellationToken.IsCancellationRequested || isDead) return;

            Debug.Log($"[{gameObject.name}] 発射指示");
            // ボール発射
            if (canonController != null)
            {
                canonController.FireCanonFromPoints(associatedSpawnPoint);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] CanonControllerが見つかりません！発射ロックを解放します。", this);
            }

            // アニメーション終了待ち
            Debug.Log("アイドル状態に戻るまで待機中");
            await UniTask.WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName("Idle"), cancellationToken: cancellationToken);
            Debug.Log($"[{gameObject.name}] アイドル状態を確認、クールダウン開始");

            // クールダウン
            //await UniTask.Delay(TimeSpan.FromSeconds(attackCoolTime), cancellationToken: cancellationToken);

            //canAttack = true;
            //isGettingHit = false;
            //Debug.Log("クールダウン開始");

            // クールダウンタイマーを開始
            StartAttackCooldownTimerAsync(bossTaskCancellation.Token).Forget();
        }
        catch(OperationCanceledException)
        {
            Debug.Log($"[{gameObject.name}] 通常攻撃キャンセル");
            if (isDead)
            {
                anim?.Play("Idle");
                StartAttackCooldownTimerAsync(bossTaskCancellation.Token).Forget();
            }
            isGettingHit = false;
        }
        catch(Exception ex)
        {
            Debug.LogError($"[{gameObject.name}] 通常攻撃中にエラー発生: {ex.Message}\n {ex.StackTrace}");
            if (!isDead)
            {
                StartAttackCooldownTimerAsync(bossTaskCancellation.Token).Forget();
            }
        }
        finally
        {
            linkedCts.Dispose();
        }
    }

    /*
    /// <summary>
    /// スキル攻撃
    /// </summary>
    private async UniTask PerformSkillAsync(CancellationToken cancellationToken)
    {
        canUseSkill = false;
        Debug.Log("スキル攻撃開始");
        anim.Play("Skill");
    }
    */

    private async UniTaskVoid StartAttackCooldownTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(attackCoolTime), cancellationToken: cancellationToken);

            if (this == null) return;
            canAttack = true;
            Debug.Log($"[{gameObject.name}] 通常攻撃クールダウン終了");
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"[{gameObject.name}] 通常攻撃クールダウンタイマーがキャンセルされた");
        }
    }

    public async UniTask DieAsync()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[{gameObject.name}] ボスを倒した！");
        bossTaskCancellation?.Cancel();

        // CanonControllerを探して、自分の担当のFirePointを削除する
        CanonController canonController = FindObjectOfType<CanonController>();
        if (canonController != null)
        {
            foreach (Transform spawnPoint in associatedSpawnPoint)
            {
                canonController.RemoveSpawanPoint(spawnPoint);
            }
        }

        /*
        // GameManagerに通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BossDefeated(this);
        }
        */

        bossTaskCancellation?.Dispose();
        bossTaskCancellation = null;

        var cancellationToken = this.GetCancellationTokenOnDestroy();

        // 倒した演出
        try
        {
            float waitTime = damageDisplay.fadeDuration + damageDisplay.displayDuration;
            await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            if (cameraController != null) cameraController.SwitchToDefeatCamera(transform);

            if (anim != null) anim.Play("Die");

            Collider[] colliders = gameObject.GetComponents<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(3.0f), cancellationToken: cancellationToken);

            Debug.Log($"[{gameObject.name}] 死亡演出終了");
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"[{gameObject.name}] 死亡演出がキャンセルされました");
        }

    }

    void UpdateHealthUI()
    {
        if(healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }

    /// <summary>
    /// GameManagerが攻撃可能か判断するためのメソッド
    /// </summary>
    /// <returns></returns>
    public bool IsAttackReady()
    {
        bool isIdle = (anim != null) ? anim.GetAnimatorTransitionInfo(0).IsName("Idle") : true;

        return canAttack && !isGettingHit && !isDead;
    }

    private async UniTaskVoid ApplyEnemyDeadBuffAsync(CancellationToken cancellationToken)
    {
        int originalDamage = this.attackPower;
        this.attackPower *= 2;
        Debug.Log($"[{gameObject.name}]別のボスが倒された！攻撃力アップ！");

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(30f), cancellationToken: cancellationToken);
        }
        finally
        {
            this.attackPower = originalDamage;
            Debug.Log($"[{gameObject.name}] ボスの攻撃力アップ終了");
        }
    }

    private void OnDestroy()
    {
        if (bossTaskCancellation != null)
        {
            if (!bossTaskCancellation.IsCancellationRequested) bossTaskCancellation?.Cancel();
            bossTaskCancellation?.Dispose();
            bossTaskCancellation = null;
        }
    }
}
