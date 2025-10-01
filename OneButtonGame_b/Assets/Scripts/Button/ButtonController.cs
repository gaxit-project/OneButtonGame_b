using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EngGame();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwichTitleScene();
        }
    }

    public void SwichBattingScene()
    {
        SceneManager.LoadScene("Batting");
    }

    public void SwichSettingScene()
    {
        SceneManager.LoadScene("Setting");
    }

    public void SwichTitleScene()
    {
        SceneManager.LoadScene("Title");
    }

    public void EngGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


}
