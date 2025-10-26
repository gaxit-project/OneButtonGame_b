using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Pause : MonoBehaviour
{
    //　ポーズした時に表示するUI
    [SerializeField]
    private GameObject pauseUI;

    [SerializeField]
    private GameObject firstSelectedButton;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("q") || Input.GetButtonDown("Submit"))
        {
            //　ポーズUIのアクティブ、非アクティブを切り替え
            pauseUI.SetActive(!pauseUI.activeSelf);

            //　ポーズUIが表示されてる時は停止
            if (pauseUI.activeSelf)
            {
                Time.timeScale = 0f;
                //　ポーズUIが表示されてなければ通常通り進行

                EventSystem.current.SetSelectedGameObject(firstSelectedButton);
            }
            else
            {
                Time.timeScale = 1f;
            }
        }
    }

    public void Resume()
    {
        //　ポーズUIを非アクティブにして、通常通り進行
        pauseUI.SetActive(false);
        Time.timeScale = 1f;
    }

}
