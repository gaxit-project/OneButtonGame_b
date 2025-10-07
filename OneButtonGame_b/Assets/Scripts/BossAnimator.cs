using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAnimator : MonoBehaviour
{
    Animator anim;

    public bool stAnim = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (stAnim == false)
        {
            stAnim = true;
            StartCoroutine(BossAttack());
        }
    }

    IEnumerator BossAttack()
    {
        anim.SetTrigger("Attack01");
        yield return new WaitForSeconds(1f);
        stAnim = false;
    }
}