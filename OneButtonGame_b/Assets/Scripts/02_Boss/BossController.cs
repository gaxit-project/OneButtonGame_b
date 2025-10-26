using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;

public class BossController : MonoBehaviour
{

    [Header("ボスの体力")]
    public int maxHealth = 1000;
    public int attackPower = 100;
    public float attackCoolTime = 5f;
    public float skillCoolTime = 20f;


    private int currentHealth;
    public int CurrentHealth => currentHealth;

    [Header("関連オブジェクト/コンポーネント")]
    public List<Transform> associatedSpawnPoint; // 敵に対応するFirePoint
    public CanonController canonController;
    public DamageDisplay damageDisplay;
    public TextMeshProUGUI healthText;

    [Header("デバッグ用")]
    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool canUseSkill = true;
    [SerializeField] private bool isGettingHit = false;
    [SerializeField] private bool isDead = false;

    private Animator anim;
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
        anim.Play("Idle");

        if (canonController == null)
        {
            canonController = FindObjectOfType<CanonController>();
            if (canonController == null)
            {
                Debug.LogError("CanonControllerが見つかりません！", this);
                enabled = false;
                return;
            }
        }

        bossTaskCancellation = new CancellationTokenSource();
    }

    void Update()
    {

    }

    /// <summary>
    /// ダメージ処理
    /// </summary>
    public void TakeBossDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        isGettingHit = true;

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

        if(currentHealth <= 0)
        {
            GameManager.Instance.HandleBossDeath(this).Forget();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] GetHitアニメーション再生");
            anim.Play("GetHit", -1, 0f);

            WaitForGetHitEndAsync().Forget();
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

    private async UniTaskVoid StartSkillCooldownTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(skillCoolTime), cancellationToken: cancellationToken);
            canUseSkill = true;
            Debug.Log("スキルクールダウン終了");
        }
        catch (OperationCanceledException)
        {

        }
    }

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

        var cancellationToken = this.GetCancellationTokenOnDestroy();

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

        // 倒した演出
        try
        {
            //yield return new WaitForSeconds(damageDisplay.fadeDuration/* + damageDisplay.displayDuration*/);
            float waitTime = damageDisplay.fadeDuration + damageDisplay.displayDuration;
            await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: cancellationToken);

            if(cancellationToken.IsCancellationRequested) return;

            anim.Play("Die");
            Collider[] colliders = gameObject.GetComponents<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
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

    private void OnDestroy()
    {
        bossTaskCancellation?.Cancel();
        bossTaskCancellation?.Dispose();
    }

    /// <summary>
    /// GameManagerが攻撃可能か判断するためのメソッド
    /// </summary>
    /// <returns></returns>
    public bool IsAttackReady()
    {
        return canAttack &&
               !isGettingHit &&
               !isDead && 
               anim != null &&
               anim.GetCurrentAnimatorStateInfo(0).IsName("Idle");
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
}
