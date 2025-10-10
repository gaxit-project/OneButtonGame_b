using UnityEngine;
using UnityEngine.UI; // Sliderを使うためにこの行を追加

public class VolumeSlider : MonoBehaviour
{
    // インスペクターからBGM用とSE用のSliderをドラッグ＆ドロップで設定
    public Slider bgmSlider;
    public Slider seSlider;

    void Start()
    {
        // SoundManagerのインスタンスが存在する場合のみ処理を行う
        if (SoundManager.instance != null)
        {
            // SoundManagerから現在の音量を取得し、スライダーの初期値に設定する
            if (bgmSlider != null)
            {
                bgmSlider.value = SoundManager.instance.audioSourceBGM.volume;
            }
            if (seSlider != null)
            {
                seSlider.value = SoundManager.instance.audioSourceSE.volume;
            }
        }
        else
        {
            Debug.LogWarning("SoundManagerのインスタンスが見つかりません。");
        }
    }

    public void SetBGMVolume(float volume)
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetBGMVolume(volume);
        }
    }

    public void SetSEVolume(float volume)
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetSEVolume(volume);
        }
    }
}