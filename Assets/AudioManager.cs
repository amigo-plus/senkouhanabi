using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("再生用ソース")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource hanabiSource;
    
    [Header("BGM")]
    public AudioClip bgmbeforeIgnition; //　虫の声
    public AudioClip bgmKeeping; 
    
    [Header("SE")]
    public AudioClip lighterSE; // ライターを付ける音
    public AudioClip hanabiSE_1; // 花火（第一段階）
    public AudioClip hanabiSE_2; // 花火（第二段階）
    public AudioClip hanabiSE_3; // 花火（第三段階）
    public AudioClip Warning; // 警告音
    public AudioClip WindSE; // 風の音
    public AudioClip taikoSE; // 結果発表時の太鼓

    public void PlaySE(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void StopSE()
    {
        if (sfxSource != null) sfxSource.Stop();
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }
    public void PlayHanabiSE(AudioClip clip, bool loop = true) // 花火のSEだけbgmとは別にloop
    {
        if (hanabiSource == null || clip == null) return;
        // すでに同じクリップが再生中なら何もしない
        if (hanabiSource.clip == clip && hanabiSource.isPlaying) return;

        hanabiSource.Stop();       // 一旦止める
        hanabiSource.clip = clip;  // 音源をセット
        hanabiSource.loop = loop;  // ループを絶対にONにする
        hanabiSource.Play();       // 再生開始
    }

    public void StopHanabiSE()
    {
        if (hanabiSource != null)
        {
            hanabiSource.Stop();
            hanabiSource.clip = null; // クリップも参照解除
        }
    }

}
