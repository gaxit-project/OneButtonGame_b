using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMain : MonoBehaviour
{
    Animator anim;

    [Header("ボスのステータス")]
    public int HP = 1000;
    public int Power = 1;
    public float coolTime = 5f;

    public bool attack = false;
    public bool gameStart = false;  //後でゲームマネージャーで設定
    public bool skill = false;
    public bool getHit = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        anim.Play("Idle");

        StartCoroutine(Attack());

    }

    void Update()
    {
        
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
                    else if (skill == true)
                    {
                        anim.SetTrigger("Skill");   //必殺技を使うかどうか
                        skill = false;
                        yield return new WaitForSeconds(coolTime);
                    }
                    else if (attack == true)
                    {
                        anim.SetTrigger("Attack");  //通常攻撃
                        attack = false;
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
                    yield break;
                }
            }
        }
    }
}
