using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMain : MonoBehaviour
{
    Animator anim;
    public CanonController canonController;
    public Ball ball;
    
    [Header("ボスのステータス")]
    public int HP = 1000;
    public int Power = 1;
    public float coolTime = 5f;

    public bool attack = false;     //攻撃するかどうか
    public bool gameStart = false;  //後でゲームマネージャーで設定
    public bool skill = false;      //必殺技を使うかどうか
    public bool getHit = false;     //ダメージを受けているかどうか
    private bool onece = false;     //死亡の回数制限

    void Start()
    {
        anim = GetComponent<Animator>();
        anim.Play("Idle");
        gameStart = true;
        StartCoroutine(Attack());

    }

    void Update()
    {
        if (HP <= 0 && onece == false)
        {
            anim.SetTrigger("Die");         //死亡
            onece = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (HP <= 0) return;
        if (collision.gameObject.CompareTag("Ball"))
        {
            anim.SetTrigger("GetHit");  //ダメージの演出
            HP -= ball.attackPower;
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
                    if (skill == true)
                    {
                        anim.SetTrigger("Skill");   //必殺技を使うかどうか
                        skill = false;
                        yield return new WaitForSeconds(10f);
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
                    yield break;
                }
            }
        }
    }
}
