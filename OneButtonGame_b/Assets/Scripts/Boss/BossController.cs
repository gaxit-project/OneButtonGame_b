using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BossController : MonoBehaviour
{
    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    [Header("ボスの体力")]
    public int maxHealth = 1000;
    private int currentHealth;

    [Header("関連オブジェクト")]
    public List<Transform> associatedSpawnPoint; // 敵に対応するFirePoint

    [Header("UIコンポーネント")]
    public DamageDisplay damageDisplay;
    public TextMeshProUGUI healthText;

    //private bool isDead = false;
    public bool attack = false;
    Animator anim;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        anim = GetComponent<Animator>();
        anim.Play("Idle");
    }

    void Update()
    {
        if (attack == true && currentHealth > 0)
        {
            attack = false;
            anim.Play("Attack");
        }
    }

    public void TakeBossDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth > 0)
        {
            anim.Play("GetHit");
        }

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        Debug.Log("ボスが" +  damage + "ダメージ受けた！残りのHP: " + currentHealth);

        if(damageDisplay != null)
        {
            damageDisplay.ShowDamage(damage);
        }

        UpdateHealthUI();

        if(currentHealth <= 0)
        {
            StartCoroutine(BossDie());
        }
    }

    private IEnumerator BossDie()
    {
        Debug.Log("ボスを倒した！");

        // CanonControllerを探して、自分の担当のFirePointを削除する
        CanonController canonController = FindObjectOfType<CanonController>();
        if (canonController != null)
        {
            foreach (Transform spawnPoint in associatedSpawnPoint)
            {
                canonController.RemoveSpawanPoint(spawnPoint);
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.BossDefeated();
        }

        yield return new WaitForSeconds(damageDisplay.fadeDuration/* + damageDisplay.displayDuration*/);
        yield return new WaitForSeconds(damageDisplay.fadeDuration + damageDisplay.displayDuration);

        anim.Play("Die");
        Collider[] col = gameObject.GetComponents<Collider>();
        col [0].enabled = false;
        col [1].enabled = false;
        col [2].enabled = false;

    }

    void UpdateHealthUI()
    {
        if(healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }
}
