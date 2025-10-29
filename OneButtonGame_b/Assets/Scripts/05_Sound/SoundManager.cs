using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    //シングルトン化
    public static SoundManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            LoadVolume();
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public AudioSource audioSourceBGM; //BGMのスピーカー
    public AudioClip[] audioClipsBGM;　//BGMの音源

    public AudioSource audioSourceSE; //SEのスピーカー
    public AudioClip[] audioClipsSE; //SEの音源

    //シーンに応じてBGMを再生
    public void PlayBGM(string sceneName)
    {
        audioSourceBGM.Stop();
        switch (sceneName)
        {
            default:
            case "Title":
                audioSourceBGM.clip = audioClipsBGM[0];
                break;
            case "Setting":
                audioSourceBGM.clip = audioClipsBGM[2];
                break;
            case "Batting":
                audioSourceBGM.clip = audioClipsBGM[2];
                break;
            case "Result":
                audioSourceBGM.clip = audioClipsBGM[3];
                break;
            case "GameOver":
                audioSourceBGM.clip = audioClipsBGM[1];
                break;
        }
        audioSourceBGM.Play();
    }

    //SEを再生
    public void PlaySE(int index)
    {
        audioSourceSE.PlayOneShot(audioClipsSE[index]);
    }

    public void SetBGMVolume(float volume)
    {
        audioSourceBGM.volume = volume;
    }

    public void SetSEVolume(float volume)
    {
        audioSourceSE.volume = volume;
    }

    private void LoadVolume()
    {
        // PlayerPrefsから音量設定を読み込み、読み込めなかったらデフォルト値(1.0f)を設定
        audioSourceBGM.volume = PlayerPrefs.GetFloat("BGMVolume_Key", 0.8f);
        audioSourceSE.volume = PlayerPrefs.GetFloat("SEVolume_Key", 1.0f);
    }
}
