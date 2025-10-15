using UnityEngine;
using TMPro;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("ボスの体力")]
    public int maxHealth = 1000;
    private int currentHealth;

    [Header("UIコンポーネント")]
    public DamageDisplay damageDisplay;
    public TextMeshProUGUI healthText;

    private bool isDead = false;
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
        if(attack == true)
        {
            attack = false;
            anim.SetTrigger("Attack");
        }
    }

    public void TakeDamage(int damage)
    {
        
        currentHealth -= damage;
        anim.SetTrigger("GetHit");

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
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        Debug.Log("ボスを倒した！");

        GetComponent<Collider>().enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.BossDefeated();
        }

        yield return new WaitForSeconds(damageDisplay.fadeDuration + damageDisplay.displayDuration);

        anim.SetTrigger("Die");
    }

    void UpdateHealthUI()
    {
        if(healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }
}
