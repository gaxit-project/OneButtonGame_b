using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMain : MonoBehaviour
{

    Animator anim;
    public Ball ball;
    public CanonController canonController;
    public GameObject Efect;

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
        if (HP <= 0 && onece ==true)
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
                        attack = false;
                        anim.SetTrigger("Attack");  //通常攻撃
                        yield return new WaitForSeconds(2.3f);
                        canonController.FireCanon();
                        yield return new WaitForSeconds(coolTime);
                        attack = true;
                    }
                    else
                    {
                        yield return null;
                    }
                }
                else
                {
                    yield return null;
                }
            }
        }
    }

    IEnumerator Dead()
    {
        anim.SetTrigger("Die");         //死亡
        onece  = false;
        ball.attackPower = ball.attackPower * 2;
        Efect.SetActive(true);
        //エフェクト
        yield return new WaitForSeconds(30f);
        ball.attackPower = ball.attackPower / 2;
        Efect.SetActive(false);

    }
}
