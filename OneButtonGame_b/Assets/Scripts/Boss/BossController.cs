using UnityEngine;
using TMPro;

public class BossController : MonoBehaviour
{
    [Header("ボスの体力")]
    public int maxHealth = 1000;
    private int currentHealth;

    [Header("UIコンポーネント")]
    public DamageDisplay damageDisplay;
    public TextMeshProUGUI healthText;

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
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("ボスを倒した！");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.BossDefeated();
        }

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
