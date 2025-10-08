using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMain : MonoBehaviour
{

    Animator anim;
    public BossMain bossMain;

    [Header("取り巻きのステータス")]
    public int HP = 250;
    public int Power = 1;
    public float coolTime = 10f;

    public bool attack = false;     //攻撃するかどうか
    public bool gameStart = true;  //後でゲームマネージャーで設定
    public bool getHit = false;     //ダメージを受けているかどうか
    private bool dead = false;
    private bool onece = true;

    void Start()
    {
        anim = GetComponent<Animator>();
        anim.Play("Idle");

        StartCoroutine(Attack());

    }

    void Update()
    {
        if (!attack && !getHit)
        {
            attack = true;  // 次の攻撃準備
        }

        if (dead == true && onece ==true)
        {
            StartCoroutine(Dead());
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (HP <= 0) return;
        if (collision.gameObject.CompareTag("Ball"))
        {
            anim.SetTrigger("GetHit");  //ダメージの演出
            HP -= 10;
        }
    }

    IEnumerator Attack()
    {
        while (true)
        {
            if (gameStart == false)
            {
                anim.Play("Idle");
                yield return null;
            }
            else
            {
                if (HP > 0)
                {
                    if (getHit == true)
                    {
                        anim.SetTrigger("GetHit");  //ダメージ最優先
                        getHit = false;
                        yield return new WaitForSeconds(1f);
                    }
                    if (attack == true)
                    {
                        anim.SetTrigger("Attack");  //通常攻撃
                        yield return new WaitForSeconds(coolTime);
                    }
                    else
                    {
                        yield return null;
                    }
                }
                else
                {
                    anim.SetTrigger("Die");         //死亡
                    dead = true;
                    yield break;
                }
            }
        }
    }

    IEnumerator Dead()
    {
        onece  = false;
        bossMain.damage = 20;
        //エフェクト
        yield return new WaitForSeconds(30f);
        bossMain.damage = 10;

    }
}
