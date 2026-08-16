using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Rendering.Universal.Internal;
using DG.Tweening;
public class GameOverSequence : MonoBehaviour
{
    [Header("演出に必要な参照")]
    public TextMeshProUGUI scoreText;   // 消したいスコア表示
    public TextMeshProUGUI timeText;    // 消したい時間表示
    public Transform handleTransform;   // 上に上げたい持ち手
    public SpriteRenderer fireballrenderer; // 火玉表示
    public SpriteRenderer fireballCoreRenderer; // 火玉の白い部分
    public ParticleSystem fireballParticles;
    public ParticleSystem fireballParticlesLv2;
    public ParticleSystem fireballParticlesLv3;
    public TextMeshProUGUI gameoverText; // 最終的に表示するリザルトテキスト
    public GameObject buttonGroup; 

    
    [Header("演出タイミング")]
    public float handleRiseDuration = 1.5f;  // 持ち手が上に上がる
    public float delayBeforeResult = 2f;  // 結果表示までの間
    public float fadeToBlackDuration = 3f; // 黒くなるまでの時間
    
    public void StartSequence(float finalScore)
    {
        // --- 1. スコア・時間を消す（即時処理） ---
        if (gameoverText != null) gameoverText.gameObject.SetActive(false);
        if (buttonGroup != null) buttonGroup.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (timeText != null) timeText.gameObject.SetActive(false);

        if (fireballParticles != null) fireballParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (fireballParticlesLv2 != null) fireballParticlesLv2.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (fireballParticlesLv3 != null) fireballParticlesLv3.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // --- DOTween シーケンスの構築 ---
        Sequence seq = DOTween.Sequence();

        // --- 2. 持ち手を上に動かす ---
        if (handleTransform != null)
        {
            // 上に15ユニット分、スムーズ（OutCubic）に移動
            seq.Append(handleTransform.DOMoveY(handleTransform.position.y + 15f, handleRiseDuration)
                .SetEase(Ease.OutCubic));
        }

        // --- 3. 火玉変化（色変更 & 縮小を「同時」に行う）---
        if (fireballrenderer != null)
        {
            Color targetColor = new Color(0.27f, 0.17f, 0.15f);

            // Appendで「持ち手が上がった後に実行」
            seq.Append(fireballrenderer.DOColor(targetColor, fadeToBlackDuration));
            
            // Joinを使うことで「色の変化と同時にCoreを縮小」
            if (fireballCoreRenderer != null)
            {
                seq.Join(fireballCoreRenderer.transform.DOScale(Vector3.zero, fadeToBlackDuration));
            }
        }

        // --- 4. 少し間を置く ---
        seq.AppendInterval(delayBeforeResult);

        // --- 5. アニメーションがすべて終わった時の処理 ---
        seq.OnComplete(() =>
        {
            if (gameoverText != null)
            {
                gameoverText.gameObject.SetActive(true);
                gameoverText.text = "スコア:" + Mathf.FloorToInt(finalScore);
                
                // テキスト表示時のポップアップ演出
                gameoverText.transform.localScale = Vector3.zero;
                gameoverText.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            }

            if (buttonGroup != null)
            {
                buttonGroup.SetActive(true);
            }
        });
    }
}