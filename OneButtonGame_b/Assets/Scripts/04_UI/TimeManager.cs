using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    private bool isPaused = false;
    private bool isSlowing = false;
    private bool isQuick = false;
    public Ball ball;

    void Update()
    {

        if (ball.justHit == true && !isSlowing == true)
        {
            Debug.Log("í Ç¡ÇƒÇ¢ÇÈÇÊ");
            StartCoroutine(TimeLate());
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            TimeQuick();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Pause();
        }
    }

    public void Pause()   //àÍìxñ⁄ÇÕí‚é~ìÒìxñ⁄ÇÕçƒäJ
    {
        if (isPaused == false)
        {
            Time.timeScale = 1f;
            isPaused = true;
        }
        else
        {
            Time.timeScale = 0f;
            isPaused = false;
        }
    }

    IEnumerator TimeLate()
    {
        isSlowing = true;
        Time.timeScale = 0.01f;
        Time.fixedDeltaTime = Time.timeScale;
        yield return new WaitForSeconds(1f);
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = Time.timeScale;
        ball.justHit = false;
        isSlowing = false;
    }

    public void TimeQuick()
    {
        if (isQuick == false)
        {
            Time.timeScale = 3f;
            isQuick = true;
        }
        else
        {
            Time.timeScale = 1f;
            isQuick = false;
        }
    }
}
