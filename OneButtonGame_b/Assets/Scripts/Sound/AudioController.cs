using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading;


public class AudioController : MonoBehaviour
{
    //シーンが変更した時にBGMを変更してSEを再生する
    //シングルトンなので、instanceを使ってアクセスする

    public static AudioController instance; // シングルトンへ

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void ToTitle()
    {
        SoundManager.instance.PlaySE(0);
        Thread.Sleep(200);
        SceneManager.LoadScene("Title");
        SoundManager.instance.PlayBGM("Title");
    }

    public void ToSetting()
    {
        SoundManager.instance.PlaySE(0);
        Thread.Sleep(200);
        SceneManager.LoadScene("Setting");
        SoundManager.instance.PlayBGM("Setting");
    }

    public void ToBatting()
    {
        SoundManager.instance.PlaySE(0);
        Thread.Sleep(200);
        SceneManager.LoadScene("Batting");
        SoundManager.instance.PlayBGM("Batting");
    }
    void Start()
    {
        SoundManager.instance.PlayBGM("Title");
    }
}
