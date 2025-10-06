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
                audioSourceBGM.clip = audioClipsBGM[1];
                break;
            case "Batting":
                audioSourceBGM.clip = audioClipsBGM[2];
                break;
        }
        audioSourceBGM.Play();
    }

    //SEを再生
    public void PlaySE(int index)
    {
        audioSourceSE.PlayOneShot(audioClipsSE[index]);
    }
}
