using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("ボスの体力")]
    public int health = 1000;

    [Header("UIコンポーネント")]
    public DamageDisplay damageDisplay;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("ボスが" +  damage + "ダメージ受けた！残りのHP: " + health);

        if(damageDisplay != null)
        {
            damageDisplay.ShowDamage(damage);
        }

        if(health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("ボスを倒した！");
        Destroy(gameObject);
    }
}
