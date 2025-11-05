using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    //public GameManager gameManager;

    private bool isPaused = false;
    private bool isSlowing = false;
    private bool onece = true;

    void Update()
    {
        if (!GameManager.Instance.IsGameActive && !isPaused && !isSlowing && Input.GetButtonDown("Fire1"))
        {
            if (Time.timeScale <= 2f)
            {
                Time.timeScale = Time.timeScale * 2;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        if (GameManager.Instance.IsGameActive && Time.timeScale != 1f && onece)
        {
            onece = false;
            Time.timeScale = 1f;
        }
    }

    public IEnumerator TimeLate()
    {
        isSlowing = true;

        float originalFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        isSlowing = false;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

}
