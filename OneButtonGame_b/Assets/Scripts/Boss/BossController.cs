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

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Update()
    {
        
    }

    public void TakeDamage(int damage)
    {
        
        currentHealth -= damage;

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

        Destroy(gameObject);
    }

    void UpdateHealthUI()
    {
        if(healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }
}
