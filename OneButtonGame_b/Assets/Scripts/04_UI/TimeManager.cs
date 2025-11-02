using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public GameManager gameManager;

    private bool isPaused = false;
    private bool isSlowing = false;
    public Ball ball;

    void Update()
    {

        if (ball.justHit && !isPaused && !isSlowing)
        {
            StartCoroutine(TimeLate());
        }

        if (!gameManager.IsGameActive && !isPaused && !isSlowing && Input.GetKeyDown(KeyCode.O))
        {
            Time.timeScale = 3f;
        }
        else if (gameManager.IsGameActive && !isPaused && !isSlowing && Input.GetKey(KeyCode.O))
        {
            Time.timeScale = 1f;
        }

        if (!isPaused && !isSlowing && Input.GetKeyUp(KeyCode.O))
        {
            Time.timeScale = 1f;
        }

        /*if (Input.GetKeyDown(KeyCode.P))
        {
            Pause();
        }*/
    }
    /*
    public void Pause()   //àÍìxñ⁄ÇÕí‚é~ìÒìxñ⁄ÇÕçƒäJ
    {
        if (isPaused == false)
        {
            Time.timeScale = 0f;
            isPaused = true;
        }
        else
        {
            Time.timeScale = 1f;
            isPaused = false;
        }
    }*/

    IEnumerator TimeLate()
    {
        isSlowing = true;

        float originalFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        ball.justHit = false;
        isSlowing = false;
    }

}
